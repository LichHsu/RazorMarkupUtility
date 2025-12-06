using System.Text.Json;
using RazorMarkupUtility.Core;
using RazorMarkupUtility.Models;
using RazorMarkupUtility.Operations;
using Lichs.MCP.Core.Attributes;

namespace RazorMarkupUtility.MCP;

public static class ToolHandlers
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [McpTool("get_razor_dom", "Parses Razor/HTML content and returns a simplified DOM tree.")]
    public static string HandleGetRazorDom([McpParameter("Path to the file")] string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        string content = File.ReadAllText(path);
        var structure = RazorDomParser.GetStructure(content);
        return JsonSerializer.Serialize(structure, _jsonOptions);
    }

    [McpTool("query_razor_elements", "Queries elements using XPath.")]
    public static string HandleQueryRazorElements(
        [McpParameter("Path to the file")] string path,
        [McpParameter("XPath query")] string xpath)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        string content = File.ReadAllText(path);
        var elements = RazorDomParser.QueryElements(content, xpath);
        return JsonSerializer.Serialize(elements, _jsonOptions);
    }

    [McpTool("update_razor_element", "Updates an element's inner HTML or attributes.")]
    public static string HandleUpdateRazorElement(
        [McpParameter("Path to the file")] string path,
        [McpParameter("XPath to the target element")] string xpath,
        [McpParameter("Optional new inner HTML", false)] string? newInnerHtml = null,
        [McpParameter("Optional dictionary of attributes to update", false)] Dictionary<string, string>? attributes = null)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        string content = File.ReadAllText(path);
        string newContent = RazorDomModifier.UpdateElement(content, xpath, newInnerHtml, attributes);
        File.WriteAllText(path, newContent);

        return "Element updated successfully.";
    }

    [McpTool("wrap_razor_element", "Wraps an element with a new parent tag.")]
    public static string HandleWrapRazorElement(
        [McpParameter("Path to the file")] string path,
        [McpParameter("XPath to the target element")] string xpath,
        [McpParameter("The wrapper tag name (e.g., div)")] string wrapperTag,
        [McpParameter("Optional attributes for the wrapper", false)] Dictionary<string, string>? attributes = null)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        string content = File.ReadAllText(path);
        string newContent = RazorDomModifier.WrapElement(content, xpath, wrapperTag, attributes);
        File.WriteAllText(path, newContent);

        return "Element wrapped successfully.";
    }

    [McpTool("append_razor_element", "Appends a new element as a child of the target element.")]
    public static string HandleAppendRazorElement(
        [McpParameter("Path to the file")] string path,
        [McpParameter("XPath to the target element")] string xpath,
        [McpParameter("The HTML content to append")] string newHtml)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        string content = File.ReadAllText(path);
        string newContent = RazorDomModifier.AppendElement(content, xpath, newHtml);
        File.WriteAllText(path, newContent);

        return "Element appended successfully.";
    }

    [McpTool("split_razor_file", "Splits a .razor file into .razor, .razor.cs, and .razor.css.")]
    public static string HandleSplitRazorFile([McpParameter("Path to the .razor file")] string path)
    {
        return RazorSplitter.SplitFile(path);
    }

    [McpTool("split_razor_batch", "Splits multiple .razor files in batch.")]
    public static string HandleSplitRazorBatch(
        [McpParameter("Directory to search for .razor files", false)] string? directory = null,
        [McpParameter("List of file paths", false)] List<string>? paths = null,
        [McpParameter("Path to a file containing a list of paths", false)] string? pathsFilePath = null,
        [McpParameter("Whether to search recursively (default false)", false)] bool recursive = false)
    {
        // 1. Check for file-based input first (Standardization)
        if (!string.IsNullOrEmpty(pathsFilePath))
        {
            return RazorSplitter.BatchSplit(pathsFilePath);
        }

        var pathsList = new List<string>();

        if (paths != null)
        {
            pathsList.AddRange(paths);
        }

        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            pathsList.AddRange(Directory.GetFiles(directory, "*.razor", searchOption));
        }

        if (pathsList.Count == 0)
        {
            throw new ArgumentException("Must provide 'paths', 'pathsFilePath' or 'directory' containing .razor files.");
        }

        return RazorSplitter.BatchSplit(pathsList.Distinct());
    }

    [McpTool("scan_razor_orphans", "Scans a Razor file for used CSS classes that are NOT defined in its scoped CSS file.")]
    public static string HandleScanRazorOrphans([McpParameter("Path to the .razor file")] string path)
    {
        var orphans = RazorOrphanScanner.ScanOrphans(path);
        return JsonSerializer.Serialize(orphans, _jsonOptions);
    }

    [McpTool("batch_rename_class_usage", "Batch renames CSS class usage across multiple Razor files.")]
    public static string HandleBatchRenameClassUsage(
        [McpParameter("Directory to search")] string directory,
        [McpParameter("Class name to replace")] string oldClass,
        [McpParameter("New class name")] string newClass,
        [McpParameter("Recursive search (default true)", false)] bool recursive = true)
    {
        var result = RazorRefactorer.BatchRenameClassUsage(directory, oldClass, newClass, recursive);
        return JsonSerializer.Serialize(result, _jsonOptions);
    }

    [McpTool("get_used_css_classes", "Scans Razor/HTML files and returns a list of used CSS classes.")]
    public static string HandleGetUsedCssClasses(
        [McpParameter("Directory to scan", false)] string? directory = null,
        [McpParameter("Path to a single file", false)] string? path = null,
        [McpParameter("Whether to scan recursively (default true)", false)] bool recursive = true)
    {
        if (!string.IsNullOrEmpty(directory))
        {
             var classes = RazorAnalyzer.GetUsedClassesFromDirectory(directory, recursive);
             return JsonSerializer.Serialize(classes, _jsonOptions);
        }
        else if (!string.IsNullOrEmpty(path))
        {
             if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);
            string content = File.ReadAllText(path);
            var classes = RazorAnalyzer.GetUsedClasses(content);
            return JsonSerializer.Serialize(classes, _jsonOptions);
        }
        else
        {
            throw new ArgumentException("Must provide 'directory' or 'path'.");
        }
    }
}
