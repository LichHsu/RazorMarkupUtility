using System.Text.RegularExpressions;

namespace RazorMarkupUtility.Core;

public static class RazorParser
{
    /// <summary>
    /// Extracts the content inside @code { ... } block.
    /// Returns null if not found.
    /// </summary>
    public static string? ExtractCodeBlock(string content)
    {
        // Simple regex might fail with nested braces, but for @code it usually starts at top level.
        // A robust parser would count braces. Let's try a brace counting approach.

        int codeIndex = content.IndexOf("@code");
        if (codeIndex == -1) return null;

        int openBraceIndex = content.IndexOf('{', codeIndex);
        if (openBraceIndex == -1) return null;

        int balance = 1;
        int i = openBraceIndex + 1;
        while (i < content.Length && balance > 0)
        {
            if (content[i] == '{') balance++;
            else if (content[i] == '}') balance--;
            i++;
        }

        if (balance == 0)
        {
            // Return content inside braces
            return content.Substring(openBraceIndex + 1, i - openBraceIndex - 2).Trim();
        }

        return null;
    }

    /// <summary>
    /// Extracts the content inside <style> ... </style> block.
    /// Returns null if not found.
    /// </summary>
    public static string? ExtractStyleBlock(string content)
    {
        var match = Regex.Match(content, @"<style[^>]*>(.*?)</style>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        return null;
    }

    /// <summary>
    /// Removes @code and <style> blocks from the content.
    /// </summary>
    public static string RemoveBlocks(string content)
    {
        string newContent = content;

        // Remove Razor comments @* ... *@
        newContent = Regex.Replace(newContent, @"@\*.*?\*@", "", RegexOptions.Singleline);

        // Remove @code block
        // Remove @code block using Regex with balancing groups to handle nested braces
        // Matches @code { ... }
        newContent = Regex.Replace(newContent, @"@code\s*\{((?>[^{}]+|\{(?<DEPTH>)|\}(?<-DEPTH>))*(?(DEPTH)(?!)))\}", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Remove @functions block (similar to @code)
        newContent = Regex.Replace(newContent, @"@functions\s*\{((?>[^{}]+|\{(?<DEPTH>)|\}(?<-DEPTH>))*(?(DEPTH)(?!)))\}", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Remove <style> block
        newContent = Regex.Replace(newContent, @"<style[^>]*>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        return newContent.Trim();
    }
}
