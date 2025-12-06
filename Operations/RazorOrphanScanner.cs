using System.Text.RegularExpressions;

namespace RazorMarkupUtility.Operations;

public static class RazorOrphanScanner
{
    private static readonly Regex ClassAttrRegex = new(@"\b(class|CssClass)\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CssClassDefRegex = new(@"\.([a-zA-Z0-9_-]+)(?=[^{}]*\{)", RegexOptions.Compiled);

    /// <summary>
    /// Scans a Razor file for CSS classes that are NOT defined in its corresponding scoped CSS file.
    /// </summary>
    /// <param name="razorPath">Path to the .razor file.</param>
    /// <returns>A list of locally undefined CSS classes.</returns>
    public static List<string> ScanOrphans(string razorPath)
    {
        if (!File.Exists(razorPath))
        {
            throw new FileNotFoundException($"File not found: {razorPath}");
        }

        // 1. Extract used classes from Razor file
        var razorContent = File.ReadAllText(razorPath);
        var usedClasses = new HashSet<string>();

        foreach (Match match in ClassAttrRegex.Matches(razorContent))
        {
            var classValue = match.Groups[2].Value;
            var classes = classValue.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var cls in classes)
            {
                // Filter out Razor expressions and invalid class names
                if (!string.IsNullOrWhiteSpace(cls) && IsValidClassName(cls))
                {
                    usedClasses.Add(cls);
                }
            }
        }

        // 2. Extract defined classes from Scoped CSS (if exists)
        var cssPath = razorPath + ".css"; // Standard Blazor scoped CSS convention
        
        var definedClasses = new HashSet<string>();

        if (File.Exists(cssPath))
        {
            var cssContent = File.ReadAllText(cssPath);
            foreach (Match match in CssClassDefRegex.Matches(cssContent))
            {
                definedClasses.Add(match.Groups[1].Value);
            }
        }

        // 3. Find candidates (Used - Defined)
        usedClasses.ExceptWith(definedClasses);

        return usedClasses.OrderBy(c => c).ToList();
    }

    private static bool IsValidClassName(string cls)
    {
        // 1. Must not start with @ (Razor explicit expression)
        if (cls.StartsWith("@")) return false;

        // 2. Must not contain non-CSS characters often found in leaked code
        // (e.g. operators ?, |, =, !, (, ), ,, +, *, ;)
        // Valid Tailwind chars include: a-z, A-Z, 0-9, -, _, :, /, [, ], ., %
        if (cls.Any(c => c == '?' || c == '|' || c == '=' || c == '!' || c == '(' || c == ')' || 
                         c == ',' || c == '+' || c == '*' || c == ';' || c == '<' || c == '>' ||
                         c == '\'' || c == '"' || c == '&' || c == '{' || c == '}'))
        {
            return false;
        }

        // 3. Filter out pure numbers (e.g. arguments "1", "0") often leaked from function calls
        if (int.TryParse(cls, out _)) return false;

        return true;
    }
}
