using System.Text.Json.Serialization;

namespace RazorMarkupUtility.Models;

public class RazorElement
{
    [JsonPropertyName("tagName")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("class")]
    public string? Class { get; set; }

    [JsonPropertyName("xpath")]
    public string XPath { get; set; } = "";

    [JsonPropertyName("attributes")]
    public Dictionary<string, string> Attributes { get; set; } = new();

    [JsonPropertyName("children")]
    public List<RazorElement> Children { get; set; } = new();

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
