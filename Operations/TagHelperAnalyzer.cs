using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace RazorMarkupUtility.Operations;

public static class TagHelperAnalyzer
{
    public class TagHelperUsage
    {
        public string TagName { get; set; } = "";
        public List<string> Attributes { get; set; } = new();
        public int LineNumber { get; set; }
        public string File { get; set; } = "";
    }

    /// <summary>
    /// 分析 Razor 檔案中的 TagHelper 使用 (包含 Component)
    /// </summary>
    public static List<TagHelperUsage> AnalyzeTagHelpers(string filePath)
    {
        var usages = new List<TagHelperUsage>();
        if (!File.Exists(filePath)) return usages;

        string content = File.ReadAllText(filePath);
        var doc = new HtmlDocument();
        doc.LoadHtml(content); // Note: HtmlAgilityPack is lenient with Razor syntax

        // 簡單策略：視為所有非標準 HTML Tag (包含大寫開頭) 為 TagHelper/Component
        // 同時包含標準 Tag 但有 asp-* 屬性的元素

        var allNodes = doc.DocumentNode.Descendants();
        foreach (var node in allNodes)
        {
            if (node.NodeType != HtmlNodeType.Element) continue;

            bool isComponent = IsPotentialComponent(node.Name);
            bool hasTagHelperAttributes = node.Attributes.Any(a => a.Name.StartsWith("asp-") || a.Name.Contains(".")); 
            // blazor attributes often contain dots or are distinct

            if (isComponent || hasTagHelperAttributes)
            {
                usages.Add(new TagHelperUsage
                {
                    TagName = node.Name,
                    Attributes = node.Attributes.Select(a => a.Name).ToList(),
                    LineNumber = node.Line,
                    File = filePath
                });
            }
        }

        return usages;
    }

    private static bool IsPotentialComponent(string tagName)
    {
        // 1. 包含點號 (Namespace.Component)
        if (tagName.Contains('.')) return true;

        // 2. 開頭大寫 (PascalCase, e.g. <MyComponent>) - HTML 是 case-insensitive 但 Razor Component 通常寫成大寫
        // HtmlAgilityPack converts standard tags to lowercase. If it preserves case, we can check.
        // Actually HAP lowercases everything by default unless configured otherwise.
        // However, Blazor components are often used in PascalCase in source.
        
        // 為了更準確，我們假設非標準 HTML 標籤即為 Component
        return !_standardHtmlTags.Contains(tagName.ToLower());
    }

    private static readonly HashSet<string> _standardHtmlTags = new()
    {
        "a", "abbr", "address", "area", "article", "aside", "audio", "b", "base", "bdi", "bdo", "blockquote", "body", "br", "button", "canvas", "caption", "cite", "code", "col", "colgroup", "data", "datalist", "dd", "del", "details", "dfn", "dialog", "div", "dl", "dt", "em", "embed", "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6", "head", "header", "hgroup", "hr", "html", "i", "iframe", "img", "input", "ins", "kbd", "label", "legend", "li", "link", "main", "map", "mark", "meta", "meter", "nav", "noscript", "object", "ol", "optgroup", "option", "output", "p", "param", "picture", "pre", "progress", "q", "rp", "rt", "ruby", "s", "samp", "script", "section", "select", "small", "source", "span", "strong", "style", "sub", "summary", "sup", "svg", "table", "tbody", "td", "template", "textarea", "tfoot", "th", "thead", "time", "title", "tr", "track", "u", "ul", "var", "video", "wbr"
    };
}
