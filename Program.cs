using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RazorMarkupUtility.MCP;
using RazorMarkupUtility.Operations;
using Lichs.MCP.Core;
using Lichs.MCP.Core.Attributes;

namespace RazorMarkupUtility;

internal class Program
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    private static readonly JsonSerializerOptions _jsonPrettyOptions = new()
    {
        WriteIndented = true,
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
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: split-batch <directory> [recursive]");
                    return;
                }
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
                Console.WriteLine(JsonSerializer.Serialize(result, _jsonPrettyOptions));
                return;
            }

            if (args[0] == "get-deps")
            {
                string root = args[1];
                Console.WriteLine(GetRazorDependencies(root));
                return;
            }

            if (args[0] == "get-implicit")
            {
                string root = args[1];
                Console.WriteLine(GetImplicitDependencies(root));
                return;
            }
        }

        var server = new McpServer("razor-markup-utility", "1.0.0");
        
        // 自動掃描 [McpTool]
        server.RegisterToolsFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
        
        await server.RunAsync(args);
    }

    // --- New Tools Wrappers ---

    [McpTool("analyze_tag_helpers", "分析 Razor 檔案中的 TagHelper 與元件使用情況 (識別 <MyComponent> 或 asp-* 屬性)。")]
    public static string AnalyzeTagHelpers([McpParameter("Razor 檔案路徑")] string path)
    {
        var usages = TagHelperAnalyzer.AnalyzeTagHelpers(path);
        return JsonSerializer.Serialize(usages, _jsonPrettyOptions);
    }

    [McpTool("get_razor_dependencies", "建立 Razor 專案的依賴關係圖 (Views, Layouts, Partials)。")]
    public static string GetRazorDependencies([McpParameter("專案根目錄")] string rootPath)
    {
        var graph = RazorDependencyAnalyzer.BuildDependencyGraph(rootPath);
        return JsonSerializer.Serialize(graph, _jsonPrettyOptions);
    }

    [McpTool("get_implicit_dependencies", "分析 Razor 專案的隱式依賴 (ViewImports, App.razor)。")]
    public static string GetImplicitDependencies([McpParameter("專案根目錄")] string rootPath)
    {
        var analyzer = new RazorImplicitDependencyAnalyzer(rootPath);
        var deps = analyzer.AnalyzeImplicitDependencies();
        return JsonSerializer.Serialize(deps, _jsonPrettyOptions);
    }
}
