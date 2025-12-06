using HtmlAgilityPack;
using RazorMarkupUtility.Core;

namespace RazorMarkupUtility.Operations;

public static class RazorValidator
{
    public static List<string> ValidateFile(string path)
    {
        if (!File.Exists(path)) return new List<string> { "File not found." };
        
        string content = File.ReadAllText(path);
        // RazorDomParser.Load uses HtmlAgilityPack internally
        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        if (doc.ParseErrors != null && doc.ParseErrors.Any())
        {
            return doc.ParseErrors
                .Select(e => $"Line {e.Line}: {e.Reason}")
                .ToList();
        }

        return new List<string>(); // Empty means valid
    }
}
