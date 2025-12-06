using Lichs.MCP.Core.Attributes;

namespace RazorMarkupUtility.Models;

public class RazorMergeOptions
{
    [McpParameter("對接 ID 屬性名稱 (預設: data-mcp-id)", false)]
    public string IdAttribute { get; set; } = "data-mcp-id";

    [McpParameter("要合併的屬性列表 (預設: class, style)", false)]
    public List<string> AttributesToMerge { get; set; } = new() { "class", "style" };
}
