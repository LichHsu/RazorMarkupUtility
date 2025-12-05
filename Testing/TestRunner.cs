using RazorMarkupUtility.Core;
using RazorMarkupUtility.Models;

namespace RazorMarkupUtility.Testing;

public static class TestRunner
{
    public static void RunAllTests()
    {
        Console.WriteLine("=== Running Tests ===");
        int passed = 0;
        int failed = 0;

        try { TestBasicParsing(); passed++; Console.WriteLine("✓ TestBasicParsing Passed"); }
        catch (Exception ex) { failed++; Console.WriteLine($"✗ TestBasicParsing Failed: {ex.Message}"); }

        try { TestRazorSyntaxHandling(); passed++; Console.WriteLine("✓ TestRazorSyntaxHandling Passed"); }
        catch (Exception ex) { failed++; Console.WriteLine($"✗ TestRazorSyntaxHandling Failed: {ex.Message}"); }

        try { TestUpdateElement(); passed++; Console.WriteLine("✓ TestUpdateElement Passed"); }
        catch (Exception ex) { failed++; Console.WriteLine($"✗ TestUpdateElement Failed: {ex.Message}"); }

        try { TestWrapElement(); passed++; Console.WriteLine("✓ TestWrapElement Passed"); }
        catch (Exception ex) { failed++; Console.WriteLine($"✗ TestWrapElement Failed: {ex.Message}"); }

        Console.WriteLine($"=== Tests Completed: {passed} Passed, {failed} Failed ===");
    }

    private static void TestBasicParsing()
    {
        string html = "<div id='test' class='container'><span>Hello</span></div>";
        var structure = RazorDomParser.GetStructure(html);

        Assert(structure.Count == 1, "Should have 1 root element");
        Assert(structure[0].TagName == "div", "Root tag should be div");
        Assert(structure[0].Id == "test", "Id should be test");
        Assert(structure[0].Children.Count == 1, "Should have 1 child");
        Assert(structure[0].Children[0].TagName == "span", "Child tag should be span");
    }

    private static void TestRazorSyntaxHandling()
    {
        string razor = @"
<div>
    @if (true)
    {
        <span>Visible</span>
    }
    <button @onclick=""ClickMe"">Click</button>
</div>";

        var structure = RazorDomParser.GetStructure(razor);
        
        // HtmlAgilityPack usually treats @if as text nodes or ignores them if they are not tags.
        // The span and button should still be parsed correctly if they are recognized as tags.
        
        // Let's check if we can find the button
        var elements = RazorDomParser.QueryElements(razor, "//button");
        Assert(elements.Count == 1, "Should find button");
        Assert(elements[0].Attributes.ContainsKey("@onclick"), "Should preserve @onclick attribute");
    }

    private static void TestUpdateElement()
    {
        string html = "<div id='target'>Old Content</div>";
        string newHtml = RazorDomModifier.UpdateElement(html, "//div[@id='target']", "New Content", new Dictionary<string, string> { { "class", "updated" } });

        Assert(newHtml.Contains("New Content"), "Should update inner HTML");
        Assert(newHtml.Contains("class=\"updated\""), "Should add class attribute");
    }

    private static void TestWrapElement()
    {
        string html = "<button>Click</button>";
        string newHtml = RazorDomModifier.WrapElement(html, "//button", "div", new Dictionary<string, string> { { "class", "wrapper" } });

        Assert(newHtml.Contains("<div class=\"wrapper\"><button>Click</button></div>"), "Should wrap element");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
