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
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--path" && i + 1 < args.Length)
                {
                    path = args[i + 1];
                    break;
                }
            }

            if (path == null)
            {
                Console.WriteLine("錯誤: 未指定路徑 (--path)");
                return;
            }

            RunRazorAudit(path);
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

    private static void RunRazorAudit(string path)
    {
        Console.WriteLine($"[Razor Audit] Scanning directory: {path}");
        if (!Directory.Exists(path))
        {
             Console.WriteLine($"錯誤: 目錄不存在 {path}");
             return;
        }

        var files = Directory.GetFiles(path, "*.razor", SearchOption.AllDirectories);
        Console.WriteLine($"Found {files.Length} Razor files.");
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
                
                if (File.Exists(cssFile))
                {
                    orphans = RazorOrphanScanner.ScanOrphans(file);
                }

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
        Console.WriteLine($"Files Scanned: {files.Length}");
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
}
