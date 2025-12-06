using Lichs.MCP.Core.Attributes;

namespace RazorMarkupUtility.Models;

public class RazorAnalysisOptions
{
    [McpParameter("是否遞迴掃描子目錄", false)]
    public bool Recursive { get; set; } = true;
}

public class RazorEditOperation
{
    public string Type { get; set; } = ""; // Update, Wrap, Append
    public string Xpath { get; set; } = "";
    public string Content { get; set; } = "";
    public Dictionary<string, string>? Attributes { get; set; }
}

public class RazorRefactorOptions
{
    // For BatchRenameClass
    public string? OldClass { get; set; }
    public string? NewClass { get; set; }
    [McpParameter("是否遞迴掃描", false)]
    public bool Recursive { get; set; } = true;
}
