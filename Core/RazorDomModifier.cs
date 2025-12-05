using HtmlAgilityPack;
using RazorMarkupUtility.Models;

namespace RazorMarkupUtility.Core;

public static class RazorDomModifier
{
    public static string UpdateElement(string content, string xpath, string? newInnerHtml = null, Dictionary<string, string>? attributes = null)
    {
        var doc = RazorDomParser.Load(content);
        var node = doc.DocumentNode.SelectSingleNode(xpath);

        if (node == null) throw new Exception($"Element not found at XPath: {xpath}");

        if (newInnerHtml != null)
        {
            node.InnerHtml = newInnerHtml;
        }

        if (attributes != null)
        {
            foreach (var kvp in attributes)
            {
                node.SetAttributeValue(kvp.Key, kvp.Value);
            }
        }

        return doc.DocumentNode.OuterHtml;
    }

    public static string WrapElement(string content, string xpath, string wrapperTag, Dictionary<string, string>? attributes = null)
    {
        var doc = RazorDomParser.Load(content);
        var node = doc.DocumentNode.SelectSingleNode(xpath);

        if (node == null) throw new Exception($"Element not found at XPath: {xpath}");

        // Create wrapper
        var wrapper = doc.CreateElement(wrapperTag);
        if (attributes != null)
        {
            foreach (var kvp in attributes)
            {
                wrapper.SetAttributeValue(kvp.Key, kvp.Value);
            }
        }

        // Clone node to preserve it
        var clonedNode = node.Clone();
        
        // Replace original node with wrapper
        node.ParentNode.ReplaceChild(wrapper, node);
        
        // Add cloned node to wrapper
        wrapper.AppendChild(clonedNode);

        return doc.DocumentNode.OuterHtml;
    }

    public static string AppendElement(string content, string parentXpath, string newHtml)
    {
        var doc = RazorDomParser.Load(content);
        var parent = doc.DocumentNode.SelectSingleNode(parentXpath);

        if (parent == null) throw new Exception($"Parent element not found at XPath: {parentXpath}");

        var newNode = HtmlNode.CreateNode(newHtml);
        parent.AppendChild(newNode);

        return doc.DocumentNode.OuterHtml;
    }
}
