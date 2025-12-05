using System.Text.Json;
using RazorMarkupUtility.Core;
using RazorMarkupUtility.Models;

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
}
