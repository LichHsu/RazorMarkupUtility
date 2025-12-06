using HtmlAgilityPack;
using RazorMarkupUtility.Core;

namespace RazorMarkupUtility.Operations;

public static class RazorPatternAnalyzer
{
    public static List<DuplicatePattern> AnalyzePatterns(string path)
    {
        // Simplified implementation:
        // 1. Get all elements
        // 2. Group by OuterHtml (ignoring whitespace differences potentially)
        // 3. Filter groups with count > 1

        if (!File.Exists(path)) return new List<DuplicatePattern>();

        string content = File.ReadAllText(path);
        var doc = RazorDomParser.Load(content);

        var elements = doc.DocumentNode.Descendants()
            .Where(n => n.NodeType == HtmlNodeType.Element)
            .Where(n => n.InnerHtml.Length > 20) // Filter out trivial elements like <br> or short spans
            .ToList();

        var duplicates = elements
            .GroupBy(n => SimplifyHtml(n.OuterHtml))
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicatePattern
            {
                PatternPreview = g.Key.Substring(0, Math.Min(g.Key.Length, 100)) + "...",
                OccurrenceCount = g.Count(),
                Locations = g.Select(n => n.Line).ToList()
            })
            .OrderByDescending(d => d.PatternPreview.Length) // Prioritize larger blocks
            .ToList();

        return duplicates;
    }

    private static string SimplifyHtml(string html)
    {
        // Remove whitespace between tags to normalize
        // This is a naive implementation
        return System.Text.RegularExpressions.Regex.Replace(html, @"\s+", " ").Trim();
    }
}

public class DuplicatePattern
{
    public string PatternPreview { get; set; } = "";
    public int OccurrenceCount { get; set; }
    public List<int> Locations { get; set; } = new();
}
