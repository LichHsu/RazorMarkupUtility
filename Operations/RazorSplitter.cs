using RazorMarkupUtility.Core;
using System.Text;

namespace RazorMarkupUtility.Operations;

public static class RazorSplitter
{
    public static string SplitFile(string razorPath)
    {
        if (!File.Exists(razorPath))
        {
            throw new FileNotFoundException("Razor file not found.", razorPath);
        }

        string content = File.ReadAllText(razorPath);
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(razorPath);
        string directory = Path.GetDirectoryName(razorPath) ?? "";

        // 1. Extract Code
        string? codeBlock = RazorParser.ExtractCodeBlock(content);
        if (!string.IsNullOrWhiteSpace(codeBlock))
        {
            string csPath = Path.Combine(directory, $"{fileNameWithoutExt}.razor.cs");

            // Try to find namespace
            string namespaceName = "MyApp.Components"; // Fallback Default

            // 1. Try explicit @namespace directive
            var namespaceMatch = System.Text.RegularExpressions.Regex.Match(content, @"@namespace\s+([\w\.]+)");
            if (namespaceMatch.Success)
            {
                namespaceName = namespaceMatch.Groups[1].Value;
            }
            else
            {
                // 2. Try infer from directory structure (Project Root)
                string? projectFile = FindProjectFile(directory);
                if (projectFile != null)
                {
                    string projectName = Path.GetFileNameWithoutExtension(projectFile);
                    string projectDir = Path.GetDirectoryName(projectFile) ?? "";
                    
                    if (directory.StartsWith(projectDir))
                    {
                        string relativePath = directory.Substring(projectDir.Length).TrimStart(Path.DirectorySeparatorChar);
                        if (!string.IsNullOrEmpty(relativePath))
                        {
                            namespaceName = $"{projectName}.{relativePath.Replace(Path.DirectorySeparatorChar, '.')}";
                        }
                        else
                        {
                            namespaceName = projectName;
                        }
                    }
                }
            }

            string classContent = $@"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;

namespace {namespaceName}
{{
    public partial class {fileNameWithoutExt}
    {{
{codeBlock}
    }}
}}";
            File.WriteAllText(csPath, classContent);
        }

        // 2. Extract Style
        string? styleBlock = RazorParser.ExtractStyleBlock(content);
        if (!string.IsNullOrWhiteSpace(styleBlock))
        {
            string cssPath = Path.Combine(directory, $"{fileNameWithoutExt}.razor.css");
            File.WriteAllText(cssPath, styleBlock);
        }

        // 3. Update .razor file
        string newRazorContent = RazorParser.RemoveBlocks(content);
        File.WriteAllText(razorPath, newRazorContent);

        return $"Successfully split {fileNameWithoutExt}.razor";
    }

    public static string BatchSplit(IEnumerable<string> paths)
    {
        var sb = new StringBuilder();
        int successCount = 0;
        int failCount = 0;

        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                {
                    SplitFile(path);
                    sb.AppendLine($"[OK] {Path.GetFileName(path)}");
                    successCount++;
                }
                else
                {
                    sb.AppendLine($"[SKIP] {Path.GetFileName(path)} (Not Found)");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[ERROR] {Path.GetFileName(path)}: {ex.Message}");
                failCount++;
            }
        }

        sb.Insert(0, $"Batch Split Complete. Success: {successCount}, Failed: {failCount}\n");
        return sb.ToString();
    }

    public static string BatchSplit(string pathsFilePath)
    {
        if (!File.Exists(pathsFilePath))
        {
            throw new FileNotFoundException("Paths file not found", pathsFilePath);
        }

        // Handle both JSON array and line-separated text
        string content = File.ReadAllText(pathsFilePath);
        IEnumerable<string> paths;

        if (content.TrimStart().StartsWith("["))
        {
            try
            {
                paths = System.Text.Json.JsonSerializer.Deserialize<string[]>(content) ?? Array.Empty<string>();
            }
            catch
            {
                // Fallback to line-based if JSON fails
                paths = File.ReadLines(pathsFilePath);
            }
        }
        else
        {
            paths = File.ReadLines(pathsFilePath);
        }

        return BatchSplit(paths.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string? FindProjectFile(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir != null)
        {
            var files = dir.GetFiles("*.csproj");
            if (files.Length > 0)
            {
                return files[0].FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }
}
