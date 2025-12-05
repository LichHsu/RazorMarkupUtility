using RazorMarkupUtility.Core;

namespace RazorMarkupUtility.Operations;

public static class RazorRefactorer
{
    public static BatchRenameResult BatchRenameClassUsage(string directory, string oldClass, string newClass, bool recursive = true)
    {
        var result = new BatchRenameResult();
        
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(directory, "*.razor", searchOption);

        foreach (var file in files)
        {
            try
            {
                string content = File.ReadAllText(file);
                string newContent = RazorDomModifier.RenameClassUsage(content, oldClass, newClass);

                if (content != newContent)
                {
                    // Create backup
                    string backupPath = file + ".bak";
                    if (!File.Exists(backupPath))
                    {
                        File.Copy(file, backupPath);
                    }

                    File.WriteAllText(file, newContent);
                    result.ModifiedFiles.Add(file);
                    result.TotalReplacements++; // Note: RenameClassUsage doesn't return count, so we just count files for now or assume at least 1.
                    // Ideally RenameClassUsage should return count, but for now we just track files.
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error processing {file}: {ex.Message}");
            }
        }

        return result;
    }
}

public class BatchRenameResult
{
    public List<string> ModifiedFiles { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int TotalReplacements { get; set; }
}
