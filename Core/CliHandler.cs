using RazorMarkupUtility.Operations;
using RazorMarkupUtility.Models;

namespace RazorMarkupUtility.Core;

public static class CliHandler
{
    public static void Handle(string[] args)
    {
        if (args.Length < 2) return;

        string command = args[0].ToLower(); // audit
        string subCommand = args[1].ToLower(); // razor

        if (command == "audit" && subCommand == "razor")
        {
            string? path = null;
            string? globalCssPath = null;
            string? ignoreFilePath = null;

            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--path" && i + 1 < args.Length)
                {
                    path = args[i + 1];
                    i++; // Skip next arg
                }
                else if (args[i] == "--global-css" && i + 1 < args.Length)
                {
                    globalCssPath = args[i + 1];
                    i++;
                }
                else if (args[i] == "--ignore-file" && i + 1 < args.Length)
                {
                    ignoreFilePath = args[i + 1];
                    i++;
                }
            }

            if (path == null)
            {
                Console.WriteLine("錯誤: 未指定路徑 (--path)");
                return;
            }

            HashSet<string> globalWhitelist = new();
            if (!string.IsNullOrEmpty(globalCssPath) && File.Exists(globalCssPath))
            {
                Console.WriteLine($"[Config] Loading Global CSS Whitelist from: {globalCssPath}");
                try
                {
                    var cssContent = File.ReadAllText(globalCssPath);
                    // Match simple class definitions .classname
                    var regex = new System.Text.RegularExpressions.Regex(@"\.(-?[_a-zA-Z]+[_a-zA-Z0-9-]*)");
                    foreach (System.Text.RegularExpressions.Match match in regex.Matches(cssContent))
                    {
                        globalWhitelist.Add(match.Groups[1].Value);
                    }
                    Console.WriteLine($"[Config] Loaded {globalWhitelist.Count} global classes.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Warning] Failed to load global CSS: {ex.Message}");
                }
            }

            List<System.Text.RegularExpressions.Regex> ignorePatterns = new();
            
            // Auto-Discovery: If no specific file provided, check for default 'tailwind-ignore.txt' in app directory
            if (string.IsNullOrEmpty(ignoreFilePath))
            {
                string defaultIgnore = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tailwind-ignore.txt");
                if (File.Exists(defaultIgnore))
                {
                    Console.WriteLine($"[Config] Auto-detected default ignore file: {defaultIgnore}");
                    ignoreFilePath = defaultIgnore;
                }
            }

            if (!string.IsNullOrEmpty(ignoreFilePath) && File.Exists(ignoreFilePath))
            {
                Console.WriteLine($"[Config] Loading Ignore Patterns from: {ignoreFilePath}");
                try
                {
                    var lines = File.ReadAllLines(ignoreFilePath);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                        
                        try 
                        {
                            ignorePatterns.Add(new System.Text.RegularExpressions.Regex(trimmed, System.Text.RegularExpressions.RegexOptions.Compiled));
                        }
                        catch (Exception regexEx)
                        {
                             Console.WriteLine($"[Warning] Invalid regex pattern '{trimmed}': {regexEx.Message}");
                        }
                    }
                    Console.WriteLine($"[Config] Loaded {ignorePatterns.Count} ignore patterns.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Warning] Failed to load ignore file: {ex.Message}");
                }
            }

            RunRazorAudit(path, globalWhitelist, ignorePatterns);
        }
        else if (command == "--scan") // Keep backward compatibility for IPC if needed, or deprecate
        {
             // IPC Scan (JSON Output)
             // Usage: --scan <path>
             string scanPath = args.Length >= 2 ? args[1] : "";
             if (!string.IsNullOrEmpty(scanPath))
             {
                 try 
                 {
                    var classes = RazorAnalyzer.GetUsedClassesFromDirectory(scanPath, true);
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(classes));
                 }
                 catch (Exception ex)
                 {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message }));
                 }
             }
        }
        else
        {
            Console.WriteLine($"未知指令: {command}");
        }
    }

    private static void RunRazorAudit(string path, HashSet<string> globalWhitelist, List<System.Text.RegularExpressions.Regex> ignorePatterns)
    {
        Console.WriteLine($"[Razor Audit] Scanning directory: {path}");
        if (!Directory.Exists(path))
        {
             Console.WriteLine($"錯誤: 目錄不存在 {path}");
             return;
        }

        var files = GetFilesRecursively(path, "*.razor");
        Console.WriteLine($"Found {files.Count} Razor files.");
        Console.WriteLine("---------------------------------------------------");

        int totalErrors = 0;
        int filesWithOrphans = 0;
        int totalOrphans = 0;

        foreach (var file in files)
        {
            try
            {
                // 1. Structure Check
                var dom = RazorDomParser.GetStructure(File.ReadAllText(file));
                
                // 2. Orphan Check (if Scoped CSS exists)
                string cssFile = file + ".css";
                List<string> orphans = new();
                
                // Auto-scan with generic patterns
                orphans = RazorOrphanScanner.ScanOrphans(file, globalWhitelist, ignorePatterns);

                if (orphans.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"FILE: {Path.GetRelativePath(path, file)}");
                    Console.WriteLine($"  [Warning] Found {orphans.Count} potential orphan classes:");
                    foreach(var o in orphans.Take(5)) Console.WriteLine($"    - {o}");
                    if (orphans.Count > 5) Console.WriteLine($"    ... and {orphans.Count - 5} more.");
                    Console.ResetColor();
                    Console.WriteLine();

                    filesWithOrphans++;
                    totalOrphans += orphans.Count;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"FILE: {Path.GetRelativePath(path, file)}");
                Console.WriteLine($"  [Critical] Parse Error: {ex.Message}");
                Console.ResetColor();
                totalErrors++;
            }
        }

        Console.WriteLine("---------------------------------------------------");
        Console.WriteLine($"Audit Complete.");
        Console.WriteLine($"Files Scanned: {files.Count}");
        Console.WriteLine($"Parse Errors: {totalErrors}");
        
        if (totalOrphans > 0)
        {
             Console.ForegroundColor = ConsoleColor.Yellow;
             Console.WriteLine($"Orphan Warnings: {totalOrphans} classes in {filesWithOrphans} files.");
             Console.ResetColor();
        }
        else
        {
             Console.ForegroundColor = ConsoleColor.Green;
             Console.WriteLine("No orphan classes found in scoped files.");
             Console.ResetColor();
        }
    }

    private static List<string> GetFilesRecursively(string path, string searchPattern)
    {
        var result = new List<string>();
        try
        {
            // Add files in current directory
            foreach (var file in Directory.GetFiles(path, searchPattern))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith(".")) continue; // Skip hidden/dot files
                result.Add(file);
            }

            // Recurse into subdirectories
            foreach (var dir in Directory.GetDirectories(path))
            {
                var dirName = Path.GetFileName(dir);
                // Exclude bin, obj, and hidden directories
                if (dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                    dirName.StartsWith("."))
                {
                    continue;
                }
                
                result.AddRange(GetFilesRecursively(dir, searchPattern));
            }
        }
        catch (UnauthorizedAccessException) 
        { 
            // Ignore permission errors 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Failed to scan directory {path}: {ex.Message}");
        }
        return result;
    }
}
