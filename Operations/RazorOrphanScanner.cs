using System.Text.RegularExpressions;
using RazorMarkupUtility.Core;
using RazorMarkupUtility.Models;

namespace RazorMarkupUtility.Operations;

public static class RazorOrphanScanner
{
    private static readonly Regex SafeCssClassDefRegex = new(@"\.(-?[_a-zA-Z]+[_a-zA-Z0-9-]*)", RegexOptions.Compiled);
    private static readonly Regex StringLiteralRegex = new(@"\""([a-zA-Z0-9\-_ ]+)\""", RegexOptions.Compiled);

    /// <summary>
    /// Scans a Razor file for CSS classes that are NOT defined in its corresponding scoped CSS file.
    /// Uses DOM parsing to avoid false positives from @code blocks.
    /// </summary>
    /// <param name="razorPath">Path to the .razor file.</param>
    /// <returns>A list of locally undefined CSS classes.</returns>
    public static List<string> ScanOrphans(string razorPath)
    {
        if (!File.Exists(razorPath))
        {
            throw new FileNotFoundException($"File not found: {razorPath}");
        }

        // 1. Extract used classes from Razor DOM
        var razorContent = File.ReadAllText(razorPath);
        var dom = RazorDomParser.GetStructure(razorContent);
        
        var usedClasses = new HashSet<string>();
        CollectClasses(dom, usedClasses);

        // 2. Extract defined classes from Scoped CSS (if exists)
        var cssPath = razorPath + ".css"; // Standard Blazor scoped CSS convention
        var definedClasses = new HashSet<string>();

        if (File.Exists(cssPath))
        {
            var cssContent = File.ReadAllText(cssPath);
            // Use safer regex for CSS parsing
            foreach (Match match in SafeCssClassDefRegex.Matches(cssContent))
            {
                definedClasses.Add(match.Groups[1].Value);
            }
        }

        // 3. Find candidates (Used - Defined)
        usedClasses.ExceptWith(definedClasses);

        return usedClasses.OrderBy(c => c).ToList();
    }

    private static void CollectClasses(List<RazorDomItem> nodes, HashSet<string> usedClasses)
    {
        foreach (var node in nodes)
        {
            if (!string.IsNullOrWhiteSpace(node.Class))
            {
                ExtractClasses(node.Class, usedClasses);
            }

            // Also check for dynamic class attributes if not covered by .Class
            if (node.Attributes.TryGetValue("class", out var classAttr) && classAttr != node.Class)
            {
                 ExtractClasses(classAttr, usedClasses);
            }

            if (node.Children.Count > 0)
            {
                CollectClasses(node.Children, usedClasses);
            }
        }
    }

    private static void ExtractClasses(string classAttributeValue, HashSet<string> usedClasses)
    {
        // 1. Standard split
        var classes = classAttributeValue.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var cls in classes)
        {
            if (IsValidClassName(cls))
            {
                usedClasses.Add(cls);
            }
        }

        // 2. If it makes use of C# logic (contains @), try to extract string literals
        if (classAttributeValue.Contains("@"))
        {
             foreach (Match match in StringLiteralRegex.Matches(classAttributeValue))
             {
                 var literals = match.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                 foreach (var lit in literals)
                 {
                     if (IsValidClassName(lit)) usedClasses.Add(lit);
                 }
             }
        }
    }

    private static bool IsValidClassName(string cls)
    {
        cls = cls.Trim();
        if (string.IsNullOrEmpty(cls)) return false;

        // 1. Must not start with @ (Razor explicit expression)
        if (cls.StartsWith("@")) return false;
        
        // Exclude Private fields
        if (cls.StartsWith("_")) return false; 
        
        // CSS classes cannot start with digit
        if (char.IsDigit(cls[0])) return false;
        
        if (cls.StartsWith("(") || cls.EndsWith(")")) return false;

        // C# property access detection: reject dot between letters (e.g. Model.Property)
        if (cls.Contains('.') && Regex.IsMatch(cls, @"[a-zA-Z]\.[a-zA-Z]")) return false;

        // 2. Must not contain non-CSS characters often found in leaked code
        if (cls.Any(c => c == '?' || c == '|' || c == '=' || c == '!' || c == '(' || c == ')' ||
                         c == ',' || c == '+' || c == '*' || c == ';' || c == '<' || c == '>' ||
                         c == '\'' || c == '"' || c == '&' || c == '{' || c == '}'))
        {
            return false;
        }

        // 3. Filter out pure numbers
        if (int.TryParse(cls, out _)) return false;
        
        // Exclude common keywords
        if (cls == "true" || cls == "false" || cls == "null" || cls == "string" || cls == "int" || cls == "bool" || cls == "var") return false;

        return true;
    }
}
