namespace OpenSkills.Cli.Utils;

/// <summary>
/// Helper for resolving skill directory paths
/// </summary>
public static class DirectoryHelper
{
    /// <summary>
    /// Get skills directory path
    /// </summary>
    /// <param name="projectLocal">If true, use project directory; otherwise use home directory</param>
    /// <param name="universal">If true, use .agent/skills</param>
    /// <param name="cursor">If true, use .cursor/skills; ignored when universal is true</param>
    /// <returns>Full path to skills directory</returns>
    public static string GetSkillsDir(bool projectLocal = false, bool universal = false, bool cursor = false)
    {
        var folder = universal ? ".agent/skills" : (cursor ? ".cursor/skills" : ".claude/skills");
        var baseDir = projectLocal
            ? Directory.GetCurrentDirectory()
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        return Path.Combine(baseDir, folder);
    }

    /// <summary>
    /// Get all searchable skill directories in priority order
    /// Priority: project .agent > global .agent > project .claude > project .cursor > global .claude > global .cursor
    /// </summary>
    /// <returns>Array of directory paths in priority order</returns>
    public static string[] GetSearchDirs()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return
        [
            Path.Combine(currentDir, ".agent/skills"),   // 1. Project universal (.agent)
            Path.Combine(homeDir, ".agent/skills"),      // 2. Global universal (.agent)
            Path.Combine(currentDir, ".claude/skills"), // 3. Project claude
            Path.Combine(currentDir, ".cursor/skills"), // 4. Project cursor
            Path.Combine(homeDir, ".claude/skills"),    // 5. Global claude
            Path.Combine(homeDir, ".cursor/skills"),    // 6. Global cursor
        ];
    }
}
