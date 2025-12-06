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

        var server = new McpServer("razor-markup-utility", "2.0.0");
        server.RegisterToolsFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
        await server.RunAsync(args);
    }

    // =========================================================================================
    // Core Tools (Consolidated)
    // =========================================================================================

    [McpTool("analyze_razor", "分析 Razor 專案結構、依賴關係與代碼使用狀況。")]
    public static string AnalyzeRazor(
        [McpParameter("分析目標路徑 (檔案或目錄，視類型而定)")] string path,
        [McpParameter("分析類型 (TagHelpers, Dependencies, ImplicitDeps, UsedClasses, Orphans)")] string analysisType,
        [McpParameter("選用參數 (JSON): { recursive: bool }", false)] string optionsJson = "{}")
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
             // 判斷 path 是檔案還是目錄
             if (Directory.Exists(path))
             {
                 return JsonSerializer.Serialize(RazorAnalyzer.GetUsedClassesFromDirectory(path, recursive), _jsonPrettyOptions);
             }
             if (File.Exists(path))
             {
                 var classes = RazorAnalyzer.GetUsedClasses(File.ReadAllText(path));
                 return JsonSerializer.Serialize(classes, _jsonPrettyOptions);
             }
             throw new FileNotFoundException("Path not found", path);
        }
        else if (analysisType.Equals("Orphans", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(RazorOrphanScanner.ScanOrphans(path), _jsonPrettyOptions);
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
        {
            return JsonSerializer.Serialize(RazorDomParser.GetStructure(content), _jsonPrettyOptions);
        }
        else
        {
            return JsonSerializer.Serialize(RazorDomParser.QueryElements(content, xpath), _jsonPrettyOptions);
        }
    }

    [McpTool("edit_razor_dom", "修改 Razor DOM 結構 (Update, Wrap, Append)。")]
    public static string EditRazorDom(
        [McpParameter("Razor 檔案路徑")] string path,
        [McpParameter("目標節點 XPath")] string xpath,
        [McpParameter("操作類型 (Update, Wrap, Append)")] string operation,
        [McpParameter("內容 (HTML 或 Wrapper Tag)")] string content,
        [McpParameter("屬性 (JSON Dictionary)", false)] string attributesJson = "{}")
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);
        
        string fileContent = File.ReadAllText(path);
        var attributes = JsonSerializer.Deserialize<Dictionary<string, string>>(attributesJson, _jsonOptions);
        string newFileContent = fileContent;

        if (operation.Equals("Update", StringComparison.OrdinalIgnoreCase))
        {
            // content 視為 InnerHtml
            newFileContent = RazorDomModifier.UpdateElement(fileContent, xpath, content, attributes);
        }
        else if (operation.Equals("Wrap", StringComparison.OrdinalIgnoreCase))
        {
            // content 視為 Wrapper Tag (e.g., "div")
            newFileContent = RazorDomModifier.WrapElement(fileContent, xpath, content, attributes);
        }
        else if (operation.Equals("Append", StringComparison.OrdinalIgnoreCase))
        {
            // content 視為 New HTML
            newFileContent = RazorDomModifier.AppendElement(fileContent, xpath, content);
        }
        else
        {
            throw new ArgumentException($"未知的操作類型: {operation}");
        }

        File.WriteAllText(path, newFileContent);
        return $"操作 {operation} 成功執行於 {path}";
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
            // 判斷是單檔還是目錄
            if (File.Exists(path))
            {
                 return RazorSplitter.SplitFile(path);
            }
            if (Directory.Exists(path))
            {
                 // 批次分割
                 var files = Directory.GetFiles(path, "*.razor", SearchOption.AllDirectories); // 預設遞迴?
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
