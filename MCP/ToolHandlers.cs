using System.Text.Json;
using RazorMarkupUtility.Core;
using RazorMarkupUtility.Models;
using RazorMarkupUtility.Operations;

namespace RazorMarkupUtility.MCP;

public static class ToolHandlers
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string HandleGetRazorDom(JsonElement args)
    {
        string path = args.GetProperty("path").GetString()!;
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        string content = File.ReadAllText(path);
        var structure = RazorDomParser.GetStructure(content);
        return JsonSerializer.Serialize(structure, _jsonOptions);
    }

    public static string HandleQueryRazorElements(JsonElement args)
    {
        string path = args.GetProperty("path").GetString()!;
        string xpath = args.GetProperty("xpath").GetString()!;
        
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        string content = File.ReadAllText(path);
        var elements = RazorDomParser.QueryElements(content, xpath);
        return JsonSerializer.Serialize(elements, _jsonOptions);
    }

    public static string HandleUpdateRazorElement(JsonElement args)
    {
        string path = args.GetProperty("path").GetString()!;
        string xpath = args.GetProperty("xpath").GetString()!;
        
        string? newInnerHtml = args.TryGetProperty("newInnerHtml", out var html) ? html.GetString() : null;
        
        Dictionary<string, string>? attributes = null;
        if (args.TryGetProperty("attributes", out var attrs))
        {
            attributes = JsonSerializer.Deserialize<Dictionary<string, string>>(attrs.GetRawText());
        }

        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        string content = File.ReadAllText(path);
        string newContent = RazorDomModifier.UpdateElement(content, xpath, newInnerHtml, attributes);
        File.WriteAllText(path, newContent);

        return "Element updated successfully.";
    }

    public static string HandleWrapRazorElement(JsonElement args)
    {
        string path = args.GetProperty("path").GetString()!;
        string xpath = args.GetProperty("xpath").GetString()!;
        string wrapperTag = args.GetProperty("wrapperTag").GetString()!;
        
        Dictionary<string, string>? attributes = null;
        if (args.TryGetProperty("attributes", out var attrs))
        {
            attributes = JsonSerializer.Deserialize<Dictionary<string, string>>(attrs.GetRawText());
        }

        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        string content = File.ReadAllText(path);
        string newContent = RazorDomModifier.WrapElement(content, xpath, wrapperTag, attributes);
        File.WriteAllText(path, newContent);

        return "Element wrapped successfully.";
    }

    public static string HandleSplitRazorFile(JsonElement args)
    {
        string path = args.GetProperty("path").GetString()!;
        return RazorSplitter.SplitFile(path);
    }

    public static string HandleSplitRazorBatch(JsonElement args)
    {
        var paths = new List<string>();

        if (args.TryGetProperty("paths", out var p))
        {
            foreach (var item in p.EnumerateArray())
            {
                var val = item.GetString();
                if (val != null) paths.Add(val);
            }
        }

        if (args.TryGetProperty("directory", out var d))
        {
            string? dir = d.GetString();
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                bool recursive = args.TryGetProperty("recursive", out var r) && r.GetBoolean();
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                paths.AddRange(Directory.GetFiles(dir, "*.razor", searchOption));
            }
        }

        if (paths.Count == 0)
        {
            throw new ArgumentException("Must provide 'paths' or 'directory' containing .razor files.");
        }

        return RazorSplitter.BatchSplit(paths.Distinct());
    }
}
