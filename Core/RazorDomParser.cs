using HtmlAgilityPack;
using RazorMarkupUtility.Models;

namespace RazorMarkupUtility.Core;

public static class RazorDomParser
{
    public static HtmlDocument Load(string content)
    {
        var doc = new HtmlDocument();
        doc.OptionOutputOriginalCase = true;
        doc.OptionWriteEmptyNodes = true;
        doc.OptionAutoCloseOnEnd = false; // Important for Razor mixed content
        doc.OptionCheckSyntax = false;

        doc.LoadHtml(content);
        return doc;
    }

    public static List<RazorDomItem> GetStructure(string content)
    {
        var doc = Load(content);
        var rootNodes = doc.DocumentNode.ChildNodes;
        var result = new List<RazorDomItem>();

        foreach (var node in rootNodes)
        {
            if (node.NodeType == HtmlNodeType.Element)
            {
                result.Add(ConvertNode(node));
            }
        }

        return result;
    }

    private static RazorDomItem ConvertNode(HtmlNode node)
    {
        var element = new RazorDomItem
        {
            Type = node.OriginalName, // Map to Type (Preserve Case)
            Id = node.GetAttributeValue("id", null),
            Class = node.GetAttributeValue("class", null),
            XPath = node.XPath,
            Text = node.InnerText.Trim()
        };

        foreach (var attr in node.Attributes)
        {
            element.Attributes[attr.Name] = attr.Value;
        }

        foreach (var child in node.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Element)
            {
                element.Children.Add(ConvertNode(child));
            }
        }

        return element;
    }

    public static List<RazorDomItem> QueryElements(string content, string xpath)
    {
        var doc = Load(content);
        var nodes = doc.DocumentNode.SelectNodes(xpath);

        // Fallback: If no nodes found, try lowercase query because HAP normalizes tag names
        if (nodes == null || nodes.Count == 0)
        {
            var xpathLower = xpath.ToLowerInvariant();
            if (xpath != xpathLower)
            {
                nodes = doc.DocumentNode.SelectNodes(xpathLower);
            }
        }

        if (nodes == null) return new List<RazorDomItem>();

        return nodes.Select(ConvertNode).ToList();
    }
}
