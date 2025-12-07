using System.Text.RegularExpressions;
using RazorMarkupUtility.Models;

namespace RazorMarkupUtility.Core;

public static class RazorAnalyzer
{

    private static readonly Regex StringLiteralRegex = new(@"\""([a-zA-Z0-9\-_ ]+)\""", RegexOptions.Compiled);

    public static List<string> GetUsedClasses(string content)
    {
        var dom = RazorDomParser.GetStructure(content);
        var usedClasses = new HashSet<string>();

        // 1. Analyze DOM
        CollectClasses(dom, usedClasses);

        // 2. Analyze @code and @functions blocks for dynamic class strings
        // We do this separately because DOM parser treats @code as text/comment which we need to parse manually
        // or extract using Regex since RazorParser.RemoveBlocks removes them.
        // We will re-extract them here from raw content.
        var codeBlock = RazorParser.ExtractCodeBlock(content);
        if (codeBlock != null) AnalyzeCodeStringLiterals(codeBlock, usedClasses);
        
        // Note: ExtractCodeBlock only finds the first one. Razor might have multiple or @functions.
        // For robustness, let's use a regex that matches ALL code blocks contents
        var scriptBlocks = Regex.Matches(content, @"@code\s*\{((?>[^{}]+|\{(?<DEPTH>)|\}(?<-DEPTH>))*(?(DEPTH)(?!)))\}", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        foreach (Match block in scriptBlocks)
        {
             AnalyzeCodeStringLiterals(block.Groups[1].Value, usedClasses);
        }

        var funcBlocks = Regex.Matches(content, @"@functions\s*\{((?>[^{}]+|\{(?<DEPTH>)|\}(?<-DEPTH>))*(?(DEPTH)(?!)))\}", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        foreach (Match block in funcBlocks)
        {
             AnalyzeCodeStringLiterals(block.Groups[1].Value, usedClasses);
        }

        return usedClasses.OrderBy(c => c).ToList();
    }

    private static void AnalyzeCodeStringLiterals(string codeContent, HashSet<string> usedClasses)
    {
         foreach (Match match in StringLiteralRegex.Matches(codeContent))
         {
             var literals = match.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
             foreach (var lit in literals)
             {
                 // Stricter check for C# string literals to avoid false positives (variable names, log messages)
                 if (IsHighConfidenceCssClass(lit)) usedClasses.Add(lit);
             }
         }
    }

    private static bool IsHighConfidenceCssClass(string cls)
    {
        if (!IsValidCssClass(cls)) return false;

        // Rule 1: Must contain a hyphen OR be a known utility pattern
        // Hyphens are very strong indicators of CSS/Tailwind (btn-primary, text-center)
        if (cls.Contains('-')) return true;

        // Rule 2: White-list common single-word Tailwind classes
        // (Avoiding common words like 'block', 'hidden', 'fixed' that might be variable names is hard, 
        // but 'absolute', 'relative', 'container', 'flex' are safer)
        var safeSingleWords = new HashSet<string> { 
            "absolute", "relative", "fixed", "static", "sticky",
            "block", "inline", "flex", "grid", "hidden",
            "border", "outline", "shadow", "rounded",
            "italic", "underline", "uppercase", "lowercase", "capitalize",
            "truncate", "transition", "transform", "container"
        };
        
        if (safeSingleWords.Contains(cls)) return true;

        // Rule 3: Allow if it looks like a BEM modifier or standard class pattern used in the project?
        // For now, if no hyphen and not whitelist, we reject to be safe.
        // E.g. "active", "disabled" are often used as boolean states in C# too ("if (active)"), so "active" class is risky.
        // But if the user does 'return "active";', it is a class.
        // Heuristic: If it has at least 2 hyphens it's almost certainly a class? No, 1 is enough.
        
        return false;
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
            if (IsValidCssClass(cls))
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
                     if (IsValidCssClass(lit)) usedClasses.Add(lit);
                 }
             }
        }
    }

    private static bool IsValidCssClass(string cls)
    {
        cls = cls.Trim();
        if (string.IsNullOrEmpty(cls)) return false;

        // Exclude Razor syntax and Logic
        if (cls.StartsWith("@")) return false;
        if (cls.StartsWith("_")) return false; // Private fields
        if (char.IsDigit(cls[0])) return false; // CSS classes cannot start with digit
        if (cls.StartsWith("(") || cls.EndsWith(")")) return false;
        
        // C# property access detection: reject dot between letters (e.g. Model.Property)
        // Tailwind uses dots for decimals (w-1.5), so we allow digit-dot-digit
        if (cls.Contains('.') && Regex.IsMatch(cls, @"[a-zA-Z]\.[a-zA-Z]")) return false;

        // Check for obviously invalid chars
        if (cls.Any(c => c == '?' || c == '|' || c == '=' || c == '!' || c == '(' || c == ')' ||
                         c == ',' || c == '+' || c == '*' || c == ';' || c == '<' || c == '>' ||
                         c == '\'' || c == '"' || c == '&' || c == '{' || c == '}'))
        {
            return false;
        }

        // Exclude pure numbers
        if (int.TryParse(cls, out _)) return false;
        
        // Exclude common keywords
        if (cls == "true" || cls == "false" || cls == "null" || cls == "string" || cls == "int" || cls == "bool" || cls == "var") return false;

        return true;
    }

    public static List<string> GetUsedClassesFromDirectory(string directory, bool recursive = true)
    {
        var allUsedClasses = new HashSet<string>();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException($"Directory not found: {directory}");

        var files = Directory.GetFiles(directory, "*.razor", searchOption)
           .Concat(Directory.GetFiles(directory, "*.html", searchOption))
           .Concat(Directory.GetFiles(directory, "*.cshtml", searchOption));

        foreach (var file in files)
        {
            try
            {
                string content = File.ReadAllText(file);
                var classes = GetUsedClasses(content);
                foreach (var cls in classes)
                {
                    allUsedClasses.Add(cls);
                }
            }
            catch
            {
                // Ignore parse errors in individual files
            }
        }

        return allUsedClasses.OrderBy(c => c).ToList();
    }
}
