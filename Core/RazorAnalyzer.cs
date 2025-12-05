using HtmlAgilityPack;

namespace RazorMarkupUtility.Core;

public static class RazorAnalyzer
{
    public static List<string> GetUsedClasses(string content)
    {
        var doc = RazorDomParser.Load(content);
        var nodes = doc.DocumentNode.SelectNodes("//*[@class]");
        
        var usedClasses = new HashSet<string>();

        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                var classAttr = node.Attributes["class"];
                if (classAttr != null)
                {
                    var classes = classAttr.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var cls in classes)
                    {
                        usedClasses.Add(cls.Trim());
                    }
                }
            }
        }

        return usedClasses.OrderBy(c => c).ToList();
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
