using System.Text.Json.Serialization;

namespace RazorMarkupUtility.Models;


public class RazorDomItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = ""; // Renamed/Aliased from TagName for consistency with user request (e.g. "div")

    [JsonIgnore]
    public string TagName => Type; // Backward compatibility alias if needed, or just helpers

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("class")]
    public string? Class { get; set; }

    [JsonPropertyName("xpath")]
    public string XPath { get; set; } = "";

    [JsonPropertyName("attributes")]
    public Dictionary<string, string> Attributes { get; set; } = new();

    [JsonPropertyName("children")]
    public List<RazorDomItem> Children { get; set; } = new();

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
