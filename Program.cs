using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RazorMarkupUtility.Core;
using RazorMarkupUtility.Models;
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

        if (args.Length > 0 && args[0] == "--test")
        {
            RazorMarkupUtility.Testing.TestRunner.RunAllTests();
            return;
        }

        var server = new McpServer("razor-markup-utility", "2.1.0");
        server.RegisterToolsFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
        await server.RunAsync(args);
    }

    // =========================================================================================
    // Core Tools (Consolidated)
    // =========================================================================================

    [McpTool("analyze_razor", "分析 Razor 專案結構、依賴關係、與代碼使用狀況。")]
    public static string AnalyzeRazor(
        [McpParameter("分析目標路徑")] string path,
        [McpParameter("分析類型 (TagHelpers, Dependencies, ImplicitDeps, UsedClasses, Orphans, Validation, Patterns)")] string analysisType,
        [McpParameter("選用參數 (JSON)", false)] string optionsJson = "{}")
    {
        var options = JsonSerializer.Deserialize<JsonElement>(optionsJson);
        bool recursive = true;
        if (options.TryGetProperty("recursive", out var r)) recursive = r.GetBoolean();

        if (analysisType.Equals("TagHelpers", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(TagHelperAnalyzer.AnalyzeTagHelpers(path), _jsonPrettyOptions);
        }
        else if (analysisType.Equals("Dependencies", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(RazorDependencyAnalyzer.BuildDependencyGraph(path), _jsonPrettyOptions);
        }
        else if (analysisType.Equals("ImplicitDeps", StringComparison.OrdinalIgnoreCase))
        {
            var analyzer = new RazorImplicitDependencyAnalyzer(path);
            return JsonSerializer.Serialize(analyzer.AnalyzeImplicitDependencies(), _jsonPrettyOptions);
        }
        else if (analysisType.Equals("UsedClasses", StringComparison.OrdinalIgnoreCase))
        {
             if (Directory.Exists(path))
                 return JsonSerializer.Serialize(RazorAnalyzer.GetUsedClassesFromDirectory(path, recursive), _jsonPrettyOptions);
             if (File.Exists(path))
                 return JsonSerializer.Serialize(RazorAnalyzer.GetUsedClasses(File.ReadAllText(path)), _jsonPrettyOptions);
             throw new FileNotFoundException("Path not found", path);
        }
        else if (analysisType.Equals("Orphans", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(RazorOrphanScanner.ScanOrphans(path), _jsonPrettyOptions);
        }
        else if (analysisType.Equals("Validation", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(RazorValidator.ValidateFile(path), _jsonPrettyOptions);
        }
        else if (analysisType.Equals("Patterns", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(RazorPatternAnalyzer.AnalyzePatterns(path), _jsonPrettyOptions);
        }

        throw new ArgumentException($"未知的分析類型: {analysisType}");
    }

    [McpTool("inspect_razor_dom", "檢視 Razor DOM 結構或查詢特定節點。")]
    public static string InspectRazorDom(
        [McpParameter("Razor 檔案路徑")] string path,
        [McpParameter("XPath 查詢字串 (若為 null 則回傳完整 DOM 結構)", false)] string? xpath = null)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);
        string content = File.ReadAllText(path);

        if (string.IsNullOrEmpty(xpath))
            return JsonSerializer.Serialize(RazorDomParser.GetStructure(content), _jsonPrettyOptions);
        else
            return JsonSerializer.Serialize(RazorDomParser.QueryElements(content, xpath), _jsonPrettyOptions);
    }

    [McpTool("edit_razor_dom", "批次修改 Razor DOM 結構。")]
    public static string EditRazorDom(
        [McpParameter("Razor 檔案路徑")] string path,
        [McpParameter("操作列表 (JSON Array of ops)")] string operationsJson)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);
        string fileContent = File.ReadAllText(path);
        
        var operations = JsonSerializer.Deserialize<List<RazorOperation>>(operationsJson, _jsonOptions);
        if (operations == null) return "No operations provided.";

        // In-memory sequential processing
        foreach (var op in operations)
        {
            if (op.Type.Equals("Update", StringComparison.OrdinalIgnoreCase))
            {
                fileContent = RazorDomModifier.UpdateElement(fileContent, op.Xpath, op.Content, op.Attributes);
            }
            else if (op.Type.Equals("Wrap", StringComparison.OrdinalIgnoreCase))
            {
                fileContent = RazorDomModifier.WrapElement(fileContent, op.Xpath, op.Content, op.Attributes);
            }
            else if (op.Type.Equals("Append", StringComparison.OrdinalIgnoreCase))
            {
                fileContent = RazorDomModifier.AppendElement(fileContent, op.Xpath, op.Content);
            }
        }

        File.WriteAllText(path, fileContent);
        return $"成功執行 {operations.Count} 個 DOM 操作於 {path}";
    }

    [McpTool("refactor_razor", "執行進階重構 (Split, BatchRenameClass)。")]
    public static string RefactorRazor(
        [McpParameter("目標路徑 (檔案或目錄)")] string path,
        [McpParameter("重構類型 (Split, BatchRenameClass)")] string refactoringType,
        [McpParameter("選項參數 (JSON)", false)] string optionsJson = "{}")
    {
        var options = JsonSerializer.Deserialize<JsonElement>(optionsJson);

        if (refactoringType.Equals("Split", StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(path)) return RazorSplitter.SplitFile(path);
            if (Directory.Exists(path))
            {
                 var files = Directory.GetFiles(path, "*.razor", SearchOption.AllDirectories);
                 return RazorSplitter.BatchSplit(files);
            }
             throw new FileNotFoundException("Path not found", path);
        }
        else if (refactoringType.Equals("BatchRenameClass", StringComparison.OrdinalIgnoreCase))
        {
            string oldClass = "";
            string newClass = "";
            bool recursive = true;

            if (options.TryGetProperty("oldClass", out var oc)) oldClass = oc.GetString() ?? "";
            if (options.TryGetProperty("newClass", out var nc)) newClass = nc.GetString() ?? "";
            if (options.TryGetProperty("recursive", out var r)) recursive = r.GetBoolean();

            if (string.IsNullOrEmpty(oldClass) || string.IsNullOrEmpty(newClass))
                throw new ArgumentException("BatchRenameClass 需要 oldClass 與 newClass 參數");

            var result = RazorRefactorer.BatchRenameClassUsage(path, oldClass, newClass, recursive);
            return JsonSerializer.Serialize(result, _jsonPrettyOptions);
        }

        throw new ArgumentException($"未知的重構類型: {refactoringType}");
    }
}

public class RazorOperation
{
    public string Type { get; set; } = ""; // Update, Wrap, Append
    public string Xpath { get; set; } = "";
    public string Content { get; set; } = "";
    public Dictionary<string, string>? Attributes { get; set; }
}
