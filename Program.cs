using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RazorMarkupUtility.MCP;
using Lichs.MCP.Core;

namespace RazorMarkupUtility;

internal class Program
{
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

        var server = new McpServer("razor-markup-utility", "1.0.0");
        RegisterTools(server);
        await server.RunAsync(args);
    }

    private static void RegisterTools(McpServer server)
    {
        server.RegisterTool("get_razor_dom",
            "Parses Razor/HTML content and returns a simplified DOM tree.",
            new { type = "object", properties = new { path = new { type = "string" } }, required = new[] { "path" } },
            args => ToolHandlers.HandleGetRazorDom(args));

        server.RegisterTool("query_razor_elements",
            "Queries elements using XPath.",
            new { type = "object", properties = new { path = new { type = "string" }, xpath = new { type = "string" } }, required = new[] { "path", "xpath" } },
            args => ToolHandlers.HandleQueryRazorElements(args));

        server.RegisterTool("update_razor_element",
            "Updates an element's inner HTML or attributes.",
            new { type = "object", properties = new { path = new { type = "string" }, xpath = new { type = "string" }, newInnerHtml = new { type = "string", description = "Optional new inner HTML" }, attributes = new { type = "object", description = "Optional dictionary of attributes to update" } }, required = new[] { "path", "xpath" } },
            args => ToolHandlers.HandleUpdateRazorElement(args));

        server.RegisterTool("wrap_razor_element",
            "Wraps an element with a new parent tag.",
            new { type = "object", properties = new { path = new { type = "string" }, xpath = new { type = "string" }, wrapperTag = new { type = "string" }, attributes = new { type = "object" } }, required = new[] { "path", "xpath", "wrapperTag" } },
            args => ToolHandlers.HandleWrapRazorElement(args));

        server.RegisterTool("append_razor_element",
            "Appends a new element as a child of the target element.",
            new { type = "object", properties = new { path = new { type = "string" }, xpath = new { type = "string" }, newHtml = new { type = "string", description = "The HTML content to append" } }, required = new[] { "path", "xpath", "newHtml" } },
            args => ToolHandlers.HandleAppendRazorElement(args));

        server.RegisterTool("split_razor_file",
            "Splits a .razor file into .razor, .razor.cs, and .razor.css.",
            new { type = "object", properties = new { path = new { type = "string", description = "Path to the .razor file" } }, required = new[] { "path" } },
            args => ToolHandlers.HandleSplitRazorFile(args));

        server.RegisterTool("split_razor_batch",
            "Splits multiple .razor files in batch.",
            new { type = "object", properties = new { paths = new { type = "array", items = new { type = "string" }, description = "List of file paths" }, pathsFilePath = new { type = "string", description = "Path to a file containing a list of paths (JSON array or line-separated). Prioritized over 'paths'." }, directory = new { type = "string", description = "Directory to search for .razor files" }, recursive = new { type = "boolean", description = "Whether to search recursively (default false)" } } },
            args => ToolHandlers.HandleSplitRazorBatch(args));

        server.RegisterTool("batch_rename_class_usage",
            "Batch renames CSS class usage across multiple Razor files.",
            new { type = "object", properties = new { directory = new { type = "string", description = "Directory to search" }, oldClass = new { type = "string", description = "Class name to replace" }, newClass = new { type = "string", description = "New class name" }, recursive = new { type = "boolean", description = "Recursive search (default true)" } }, required = new[] { "directory", "oldClass", "newClass" } },
            args => ToolHandlers.HandleBatchRenameClassUsage(args));

        server.RegisterTool("get_used_css_classes",
            "Scans Razor/HTML files and returns a list of used CSS classes.",
            new { type = "object", properties = new { path = new { type = "string", description = "Path to a single file" }, directory = new { type = "string", description = "Directory to scan" }, recursive = new { type = "boolean", description = "Whether to scan recursively (default true)" } } },
            args => ToolHandlers.HandleGetUsedCssClasses(args));

        server.RegisterTool("scan_razor_orphans",
            "Scans a Razor file for used CSS classes that are NOT defined in its scoped CSS file.",
            new { type = "object", properties = new { path = new { type = "string", description = "Path to the .razor file" } }, required = new[] { "path" } },
            args => ToolHandlers.HandleScanRazorOrphans(args));
    }
}
