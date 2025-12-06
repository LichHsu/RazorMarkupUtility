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
        
        // 自動掃描 [McpTool]
        server.RegisterToolsFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
        
        await server.RunAsync(args);
    }
}
