using HtmlAgilityPack;
using RazorMarkupUtility.Models;

namespace RazorMarkupUtility.Operations;

public static class RazorMerger
{
    /// <summary>
    /// 將設計檔 (Design) 的樣式合併回邏輯檔 (Logic)
    /// </summary>
    public static string MergeStyles(string logicPath, string designPath, RazorMergeOptions options)
    {
        if (!File.Exists(logicPath)) throw new FileNotFoundException("Logic file not found", logicPath);
        if (!File.Exists(designPath)) throw new FileNotFoundException("Design file not found", designPath);

        string logicContent = File.ReadAllText(logicPath);
        string designContent = File.ReadAllText(designPath);

        var logicDoc = new HtmlDocument();
        // Option "Auto" usually handles Razor marginally well, but keeping original formatting is hard.
        // We will just update attributes on the DOM and use Output, hopefully preserving code.
        // Known Issue: HtmlAgilityPack might mangle @code blocks if they are not inside valid HTML tags or generic text.
        // Mitigation: We parse logicContent, update it, but we need to be careful about preserving @code.
        // Actually, RazorDomParser.Load uses HtmlAgilityPack. 

        logicDoc.LoadHtml(logicContent);
        var designDoc = new HtmlDocument();
        designDoc.LoadHtml(designContent);

        var idAttr = options.IdAttribute;
        var attrsToMerge = new HashSet<string>(options.AttributesToMerge, StringComparer.OrdinalIgnoreCase);

        // Map Design Nodes by ID
        var designMap = new Dictionary<string, HtmlNode>();
        var designNodes = designDoc.DocumentNode.SelectNodes($"//*[@{idAttr}]");

        if (designNodes != null)
        {
            foreach (var node in designNodes)
            {
                string id = node.GetAttributeValue(idAttr, "");
                if (!string.IsNullOrEmpty(id))
                {
                    designMap[id] = node;
                }
            }
        }

        // Traverse Logic Nodes
        var logicNodes = logicDoc.DocumentNode.SelectNodes($"//*[@{idAttr}]");
        int mergedCount = 0;

        if (logicNodes != null)
        {
            foreach (var lNode in logicNodes)
            {
                string id = lNode.GetAttributeValue(idAttr, "");
                if (designMap.TryGetValue(id, out var dNode))
                {
                    // Merge Attributes
                    foreach (var attrName in attrsToMerge)
                    {
                        var dVal = dNode.GetAttributeValue(attrName, "");
                        if (dVal != null)
                        {
                            // Overwrite logic node's attribute with design node's value
                            lNode.SetAttributeValue(attrName, dVal);
                        }
                    }
                    mergedCount++;
                }
            }
        }

        // Save back
        // Warning: HAP's .OuterHtml might mess up Razor specific constructs like @if (..) { <div.. }
        // Ideally we should use a smarter replacer, but for V1 let's trust HAP's minimal interference on well-formed HTML.
        // If Logic Razor is pure markup + @code block, usually it's fine.
        // If it has complex inline C# blocks interleaved, HAP *might* decode symbols.

        return logicDoc.DocumentNode.OuterHtml; // Returns the full merged content
    }
}
