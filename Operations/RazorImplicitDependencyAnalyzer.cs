using System.Text.RegularExpressions;

namespace RazorMarkupUtility.Operations;

public class RazorImplicitDependencyAnalyzer
{
    private readonly string _rootPath;
    private readonly Dictionary<string, List<string>> _implicitDependencies = new(); // Directory -> [LayoutPaths, ViewImportPaths]

    public RazorImplicitDependencyAnalyzer(string rootPath)
    {
        _rootPath = rootPath;
    }

    /// <summary>
    /// 分析隱式依賴 (ViewImports, App.razor RouteView)
    /// </summary>
    /// <returns>每個目錄的隱式依賴列表</returns>
    public Dictionary<string, List<string>> AnalyzeImplicitDependencies()
    {
        // 1. Scan for _ViewImports.cshtml and _ViewStart.cshtml
        var configFiles = Directory.GetFiles(_rootPath, "_View*.cshtml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(_rootPath, "_Imports.razor", SearchOption.AllDirectories));

        foreach (var file in configFiles)
        {
            var dir = Path.GetDirectoryName(file);
            if (dir == null) continue;

            if (!_implicitDependencies.ContainsKey(dir))
            {
                _implicitDependencies[dir] = new List<string>();
            }

            _implicitDependencies[dir].Add(file);

            // Check for explicit layout definition in _ViewStart
            if (Path.GetFileName(file).Equals("_ViewStart.cshtml", StringComparison.OrdinalIgnoreCase))
            {
                var content = File.ReadAllText(file);
                var layoutMatch = Regex.Match(content, @"Layout\s*=\s*""([^""]+)""");
                if (layoutMatch.Success)
                {
                    _implicitDependencies[dir].Add($"LAYOUT:{layoutMatch.Groups[1].Value}");
                }
            }
        }

        // 2. Scan App.razor for Blazor default layout
        var appRazor = Path.Combine(_rootPath, "App.razor");
        if (File.Exists(appRazor))
        {
            var content = File.ReadAllText(appRazor);
            var routeViewMatch = Regex.Match(content, @"<RouteView\s+.*DefaultLayout=""@typeof\(([^)]+)\)""");
            if (routeViewMatch.Success)
            {
                // Root implicit dependency
                if (!_implicitDependencies.ContainsKey(_rootPath))
                {
                    _implicitDependencies[_rootPath] = new List<string>();
                }
                _implicitDependencies[_rootPath].Add($"LAYOUT:{routeViewMatch.Groups[1].Value}");
            }
        }

        return _implicitDependencies;
    }

    /// <summary>
    /// 獲取指定檔案的隱式依賴列表 (包含上層目錄的繼承)
    /// </summary>
    public List<string> GetImplicitDependenciesForFile(string filePath)
    {
        var result = new List<string>();
        var dir = Path.GetDirectoryName(filePath);

        while (dir != null && dir.StartsWith(_rootPath))
        {
            if (_implicitDependencies.TryGetValue(dir, out var deps))
            {
                result.AddRange(deps);
            }
            dir = Path.GetDirectoryName(dir);
        }

        return result.Distinct().ToList();
    }
}
