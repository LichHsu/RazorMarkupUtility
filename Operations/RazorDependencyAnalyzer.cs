using System.Text.RegularExpressions;

namespace RazorMarkupUtility.Operations;

public static class RazorDependencyAnalyzer
{
    public class RazorDependencyNode
    {
        public string FilePath { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public List<string> DependsOn { get; set; } = new(); // Layouts, Partials this file uses
        public List<string> UsedBy { get; set; } = new();    // Files that use this file
    }

    /// <summary>
    /// 分析 Razor 檔案的依賴關係 (_Layout, Partial, ViewImports)
    /// </summary>
    public static List<RazorDependencyNode> BuildDependencyGraph(string rootPath)
    {
        var result = new Dictionary<string, RazorDependencyNode>();
        var files = Directory.GetFiles(rootPath, "*.cshtml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(rootPath, "*.razor", SearchOption.AllDirectories));

        foreach (var file in files)
        {
            var node = new RazorDependencyNode
            {
                FilePath = file,
                RelativePath = Path.GetRelativePath(rootPath, file)
            };
            result[file] = node;
        }

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var node = result[file];

            // 1. Layout Dependency: @{ Layout = "_Layout"; } or @layout MainLayout
            var layoutMatch = Regex.Match(content, @"Layout\s*=\s*""([^""]+)""");
            if (layoutMatch.Success)
            {
                AddDependency(result, rootPath, node, layoutMatch.Groups[1].Value);
            }
            
            var blazorLayoutMatch = Regex.Match(content, @"@layout\s+([a-zA-Z0-9_]+)");
            if (blazorLayoutMatch.Success)
            {
                AddDependency(result, rootPath, node, blazorLayoutMatch.Groups[1].Value);
            }

            // 2. Partial Tag Helper: <partial name="_LoginPartial" />
            var partialTagMatch = Regex.Matches(content, @"<partial\s+name=""([^""]+)""");
            foreach (Match match in partialTagMatch)
            {
                AddDependency(result, rootPath, node, match.Groups[1].Value);
            }

            // 3. Html.Partial / RenderPartial: @await Html.PartialAsync("_Val")
            var htmlPartialMatch = Regex.Matches(content, @"PartialAsync\(""([^""]+)""");
            foreach (Match match in htmlPartialMatch)
            {
                AddDependency(result, rootPath, node, match.Groups[1].Value);
            }
            
            // 4. Component Usage (Optional - requires TagHelperAnalyzer, simplified here)
            // For now, we focus on explicit file-based dependencies usually found in MVC/Razor Pages
        }

        return result.Values.ToList();
    }

    private static void AddDependency(Dictionary<string, RazorDependencyNode> graph, string rootPath, RazorDependencyNode current, string targetName)
    {
        // Try to resolve targetName to a file path
        // Common pattern: _Layout -> Shared/_Layout.cshtml
        // Or simple name -> search in Shared or current dir
        
        // Simplified resolution: Find any file ending with targetName.cshtml or .razor
        // This is a heuristic and might have collisions, but good enough for 90%
        
        string targetKey = null;

        foreach (var key in graph.Keys)
        {
            string fileName = Path.GetFileNameWithoutExtension(key);
            if (fileName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                targetKey = key;
                break;
            }
        }

        if (targetKey != null)
        {
            if (!current.DependsOn.Contains(targetKey))
                current.DependsOn.Add(targetKey);

            var targetNode = graph[targetKey];
            if (!targetNode.UsedBy.Contains(current.FilePath))
                targetNode.UsedBy.Add(current.FilePath);
        }
    }
}
