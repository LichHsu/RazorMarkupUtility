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
                // Filter out Razor expressions (starting with @) and typically dynamic bindings
                if (!cls.StartsWith("@") && !string.IsNullOrWhiteSpace(cls))
                {
                    usedClasses.Add(cls);
                }
            }
        }

        // 2. Extract defined classes from Scoped CSS (if exists)
        var cssPath = razorPath + ".css"; // Standard Blazor scoped CSS convention
        // Also check for {File}.razor.css convention if {File}.css isn't found, though usually .razor.css IS the convention for .razor files.
        // Usually: MyComponent.razor -> MyComponent.razor.css

        // NOTE: The user prompt context implies checking ".razor.css".
        // If razorPath is "Component.razor", cssPath currently is "Component.razor.css", which is correct.
        
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
}
