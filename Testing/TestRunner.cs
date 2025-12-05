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

        try { TestRazorSplitting(); passed++; Console.WriteLine("✓ TestRazorSplitting Passed"); }
        catch (Exception ex) { failed++; Console.WriteLine($"✗ TestRazorSplitting Failed: {ex.Message}"); }

        try { TestAppendElement(); passed++; Console.WriteLine("✓ TestAppendElement Passed"); }
        catch (Exception ex) { failed++; Console.WriteLine($"✗ TestAppendElement Failed: {ex.Message}"); }

        Console.WriteLine($"=== Tests Completed: {passed} Passed, {failed} Failed ===");
    }

    private static void TestAppendElement()
    {
        string html = "<div id='parent'><span>Existing</span></div>";
        string newHtml = RazorDomModifier.AppendElement(html, "//div[@id='parent']", "<button>New Button</button>");

        Assert(newHtml.Contains("<button>New Button</button>"), "Should contain new element");
        Assert(newHtml.Contains("<span>Existing</span>"), "Should preserve existing content");
        // HtmlAgilityPack formatting might vary, but generally it appends to end
    }

    private static void TestRazorSplitting()
    {
        string razorContent = @"
@page ""/""
@namespace MyApp.Pages

<h1>Hello</h1>

@code {
    private int count = 0;
    private void Increment() { count++; }
}

<style>
    h1 { color: red; }
</style>
";
        // Test RazorParser directly
        string? codeBlock = RazorParser.ExtractCodeBlock(razorContent);
        Assert(codeBlock != null && codeBlock.Contains("private int count = 0;"), "Should extract code block");

        string? styleBlock = RazorParser.ExtractStyleBlock(razorContent);
        Assert(styleBlock != null && styleBlock.Contains("color: red;"), "Should extract style block");

        string cleanContent = RazorParser.RemoveBlocks(razorContent);
        Assert(!cleanContent.Contains("@code"), "Should remove @code");
        Assert(!cleanContent.Contains("<style>"), "Should remove <style>");
        Assert(cleanContent.Contains("<h1>Hello</h1>"), "Should keep HTML");

        // Test RazorSplitter (Integration Test)
        string testDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestRazor");
        Directory.CreateDirectory(testDir);
        string testFile = Path.Combine(testDir, "TestComp.razor");
        File.WriteAllText(testFile, razorContent);

        RazorMarkupUtility.Operations.RazorSplitter.SplitFile(testFile);

        Assert(File.Exists(Path.Combine(testDir, "TestComp.razor.cs")), "Should create .cs file");
        Assert(File.Exists(Path.Combine(testDir, "TestComp.razor.css")), "Should create .css file");
        
        string csContent = File.ReadAllText(Path.Combine(testDir, "TestComp.razor.cs"));
        Assert(csContent.Contains("partial class TestComp"), "CS file should contain partial class");
        Assert(csContent.Contains("namespace MyApp.Pages"), "CS file should use correct namespace");

        // Cleanup
        Directory.Delete(testDir, true);
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
