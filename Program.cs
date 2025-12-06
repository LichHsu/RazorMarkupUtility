using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RazorMarkupUtility.MCP;

namespace RazorMarkupUtility;

internal class Program
{
    private static readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mcp_debug_log.txt");
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        Console.InputEncoding = new UTF8Encoding(false);

        if (args.Length > 0)
        {
            if (args[0] == "--test")
            {
                RazorMarkupUtility.Testing.TestRunner.RunAllTests();
                return;
            }
            
            if (args[0] == "split-batch")
            {
                // Usage: split-batch <directory> [recursive]
                string directory = args[1];
                bool recursive = args.Length > 2 && bool.Parse(args[2]);
                
                var paths = Directory.GetFiles(directory, "*.razor", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                Console.WriteLine(RazorMarkupUtility.Operations.RazorSplitter.BatchSplit(paths));
                return;
            }

            if (args[0] == "rename-class")
            {
                // Usage: rename-class <directory> <oldClass> <newClass> [recursive]
                string directory = args[1];
                string oldClass = args[2];
                string newClass = args[3];
                bool recursive = args.Length <= 4 || bool.Parse(args[4]);

                var result = RazorMarkupUtility.Operations.RazorRefactorer.BatchRenameClassUsage(directory, oldClass, newClass, recursive);
                Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
                return;
            }
        }

        Log("=== Razor Markup Server Started ===");

        try
        {
            while (true)
            {
                string? line = await Console.In.ReadLineAsync();
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                Log($"[RECV]: {line}");

                var request = JsonSerializer.Deserialize<JsonRpcRequest>(line, _jsonOptions);
                if (request == null) continue;

                object? result = null;

                switch (request.Method)
                {
                    case "initialize":
                        result = new
                        {
                            protocolVersion = "2024-11-05",
                            capabilities = new { tools = new { } },
                            serverInfo = new { name = "razor-markup-utility", version = "1.0.0" }
                        };
                        break;

                    case "notifications/initialized":
                        continue;

                    case "tools/list":
                        result = new { tools = GetToolDefinitions() };
                        break;

                    case "tools/call":
                        try
                        {
                            result = HandleToolCall(request.Params);
                        }
                        catch (Exception ex)
                        {
                            Log($"[ERROR]: {ex.Message}");
                            SendResponse(new JsonRpcResponse
                            {
                                Id = request.Id,
                                Error = new { code = -32602, message = ex.Message }
                            });
                            continue;
                        }
                        break;
                }

                if (result != null)
                {
                    SendResponse(new JsonRpcResponse { Id = request.Id, Result = result });
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[FATAL]: {ex}");
        }
    }

    private static object[] GetToolDefinitions()
    {
        return
        [
            new
            {
                name = "get_razor_dom",
                description = "Parses Razor/HTML content and returns a simplified DOM tree.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string" }
                    },
                    required = new[] { "path" }
                }
            },
            new
            {
                name = "query_razor_elements",
                description = "Queries elements using XPath.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string" },
                        xpath = new { type = "string" }
                    },
                    required = new[] { "path", "xpath" }
                }
            },
            new
            {
                name = "update_razor_element",
                description = "Updates an element's inner HTML or attributes.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string" },
                        xpath = new { type = "string" },
                        newInnerHtml = new { type = "string", description = "Optional new inner HTML" },
                        attributes = new { type = "object", description = "Optional dictionary of attributes to update" }
                    },
                    required = new[] { "path", "xpath" }
                }
            },
            new
            {
                name = "wrap_razor_element",
                description = "Wraps an element with a new parent tag.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string" },
                        xpath = new { type = "string" },
                        wrapperTag = new { type = "string" },
                        attributes = new { type = "object" }
                    },
                    required = new[] { "path", "xpath", "wrapperTag" }
                }
            },
            new
            {
                name = "append_razor_element",
                description = "Appends a new element as a child of the target element.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string" },
                        xpath = new { type = "string" },
                        newHtml = new { type = "string", description = "The HTML content to append" }
                    },
                    required = new[] { "path", "xpath", "newHtml" }
                }
            },
            new
            {
                name = "split_razor_file",
                description = "Splits a .razor file into .razor, .razor.cs, and .razor.css.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "Path to the .razor file" }
                    },
                    required = new[] { "path" }
                }
            },
            new
            {
                name = "split_razor_batch",
                description = "Splits multiple .razor files in batch.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        paths = new { type = "array", items = new { type = "string" }, description = "List of file paths" },
                        pathsFilePath = new { type = "string", description = "Path to a file containing a list of paths (JSON array or line-separated). Prioritized over 'paths'." },
                        directory = new { type = "string", description = "Directory to search for .razor files" },
                        recursive = new { type = "boolean", description = "Whether to search recursively (default false)" }
                    }
                }
            },
            new
            {
                name = "batch_rename_class_usage",
                description = "Batch renames CSS class usage across multiple Razor files.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        directory = new { type = "string", description = "Directory to search" },
                        oldClass = new { type = "string", description = "Class name to replace" },
                        newClass = new { type = "string", description = "New class name" },
                        recursive = new { type = "boolean", description = "Recursive search (default true)" }
                    },
                    required = new[] { "directory", "oldClass", "newClass" }
                }
            },
            new
            {
                name = "get_used_css_classes",
                description = "Scans Razor/HTML files and returns a list of used CSS classes.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "Path to a single file" },
                        directory = new { type = "string", description = "Directory to scan" },
                        recursive = new { type = "boolean", description = "Whether to scan recursively (default true)" }
                    }
                }
            },
            new
            {
                name = "scan_razor_orphans",
                description = "Scans a Razor file for used CSS classes that are NOT defined in its scoped CSS file.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "Path to the .razor file" }
                    },
                    required = new[] { "path" }
                }
            }
        ];
    }

    private static object HandleToolCall(JsonElement paramsEl)
    {
        string name = paramsEl.GetProperty("name").GetString() ?? "";
        JsonElement args = paramsEl.GetProperty("arguments");

        string resultText = name switch
        {
            "get_razor_dom" => ToolHandlers.HandleGetRazorDom(args),
            "query_razor_elements" => ToolHandlers.HandleQueryRazorElements(args),
            "update_razor_element" => ToolHandlers.HandleUpdateRazorElement(args),
            "wrap_razor_element" => ToolHandlers.HandleWrapRazorElement(args),
            "append_razor_element" => ToolHandlers.HandleAppendRazorElement(args),
            "split_razor_file" => ToolHandlers.HandleSplitRazorFile(args),
            "split_razor_batch" => ToolHandlers.HandleSplitRazorBatch(args),
            "batch_rename_class_usage" => ToolHandlers.HandleBatchRenameClassUsage(args),
            "get_used_css_classes" => ToolHandlers.HandleGetUsedCssClasses(args),
            "scan_razor_orphans" => ToolHandlers.HandleScanRazorOrphans(args),
            _ => throw new Exception($"Unknown tool: {name}")
        };

        return new { content = new[] { new { type = "text", text = resultText } } };
    }

    private static void SendResponse(JsonRpcResponse response)
    {
        string json = JsonSerializer.Serialize(response, _jsonOptions);
        Log($"[SEND]: {json}");
        Console.Write(json + "\n");
        Console.Out.Flush();
    }

    private static void Log(string message)
    {
        try { File.AppendAllText(_logPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}"); }
        catch { }
    }
}
