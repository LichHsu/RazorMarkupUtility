using Lichs.MCP.Core;
using Lichs.MCP.Core.Attributes;
using RazorMarkupUtility.Core;
using RazorMarkupUtility.Models;
using RazorMarkupUtility.Operations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RazorMarkupUtility;

internal class Program
{
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

        var server = new McpServer("razor-markup-utility", "2.2.0");
        server.RegisterToolsFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
        await server.RunAsync(args);
    }

    [McpTool("analyze_razor", "分析 Razor 專案結構、依賴關係、與代碼使用狀況。")]
    public static string AnalyzeRazor(
        [McpParameter("分析目標路徑")] string path,
        [McpParameter("分析類型 (TagHelpers, Dependencies, ImplicitDeps, UsedClasses, Orphans, Validation, Patterns)")] string analysisType,
        [McpParameter("選用參數", false)] RazorAnalysisOptions? options = null)
    {
        options ??= new RazorAnalysisOptions();
        bool recursive = options.Recursive;

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
        if (Directory.Exists(path))
            {
                var orphans = new List<string>();
                var files = Directory.GetFiles(path, "*.razor", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    try
                    {
                        var fileOrphans = RazorOrphanScanner.ScanOrphans(file);
                        orphans.AddRange(fileOrphans);
                    }
                    catch { /* Ignore */ }
                }
                return JsonSerializer.Serialize(orphans.Distinct().OrderBy(x => x), _jsonPrettyOptions);
            }
            return JsonSerializer.Serialize(RazorOrphanScanner.ScanOrphans(path), _jsonPrettyOptions);
        }
        else if (analysisType.Equals("Validation", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(RazorValidator.ValidateFile(path), _jsonPrettyOptions);
        }
        else if (analysisType.Equals("Patterns", StringComparison.OrdinalIgnoreCase))
        {
            // Patterns might also need Recursive option if path is directory, but logic is naive for now
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
        [McpParameter("操作列表")] List<RazorEditOperation> operations)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);
        if (operations == null || operations.Count == 0) return "No operations provided.";

        string fileContent = File.ReadAllText(path);

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

    [McpTool("refactor_razor", "執行進階重構。")]
    public static string RefactorRazor(
        [McpParameter("目標路徑 (檔案或目錄)")] string path,
        [McpParameter("重構類型 (Split, BatchRenameClass)")] string refactoringType,
        [McpParameter("選項參數", false)] RazorRefactorOptions? options = null)
    {
        options ??= new RazorRefactorOptions();

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
            if (string.IsNullOrEmpty(options.OldClass) || string.IsNullOrEmpty(options.NewClass))
                throw new ArgumentException("BatchRenameClass 需要 OldClass 與 NewClass 參數");

            var result = RazorRefactorer.BatchRenameClassUsage(path, options.OldClass, options.NewClass, options.Recursive);
            return JsonSerializer.Serialize(result, _jsonPrettyOptions);
        }

        throw new ArgumentException($"未知的重構類型: {refactoringType}");
    }

    [McpTool("merge_razor", "將設計檔 (Design) 的樣式合併回邏輯檔 (Logic)")]
    public static string MergeRazor(
        [McpParameter("邏輯 Razor 檔案路徑")] string logicPath,
        [McpParameter("設計 HTML 檔案路徑")] string designPath,
        [McpParameter("合併選項", false)] RazorMergeOptions? options = null)
    {
        options ??= new RazorMergeOptions();
        string newContent = RazorMerger.MergeStyles(logicPath, designPath, options);

        // Write back to Logic Path? Or create a new file?
        // Usually merging implies updating the logic file.
        // Let's backup first just in case.
        string backupPath = logicPath + ".bak";
        File.Copy(logicPath, backupPath, true);

        File.WriteAllText(logicPath, newContent);

        return $"合併成功！已將樣式從 {Path.GetFileName(designPath)} 套用到 {Path.GetFileName(logicPath)}。備份已建立於 {Path.GetFileName(backupPath)}。";
    }
}
