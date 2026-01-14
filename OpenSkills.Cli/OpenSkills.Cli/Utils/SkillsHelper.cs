using OpenSkills.Cli.Models;

namespace OpenSkills.Cli.Utils;

/// <summary>
/// Helper for normalizing paths to use forward slashes consistently
/// </summary>
internal static class PathHelper
{
    /// <summary>
    /// Normalize path to use forward slashes consistently
    /// </summary>
    public static string NormalizePath(string path) => path.Replace('\\', '/');
}

/// <summary>
/// Helper for finding and enumerating skills
/// </summary>
public static class SkillsHelper
{
    /// <summary>
    /// Check if a directory entry is a directory or a symlink pointing to a directory
    /// </summary>
    private static bool IsDirectoryOrSymlinkToDirectory(string fullPath)
    {
        try
        {
            var info = new FileInfo(fullPath);
            if (info.Attributes.HasFlag(FileAttributes.Directory))
            {
                return true;
            }
            
            // Check if it's a symlink that points to a directory
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                var target = Directory.ResolveLinkTarget(fullPath, true);
                if (target is not null && target.Exists)
                {
                    return target.Attributes.HasFlag(FileAttributes.Directory);
                }
            }
        }
        catch
        {
            // Broken symlink or permission error
            return false;
        }
        
        return false;
    }

    /// <summary>
    /// Find all installed skills across directories
    /// </summary>
    /// <returns>List of all installed skills</returns>
    public static List<Skill> FindAllSkills()
    {
        var skills = new List<Skill>();
        var seen = new HashSet<string>();
        var dirs = DirectoryHelper.GetSearchDirs();
        var currentDir = Directory.GetCurrentDirectory();

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;

            try
            {
                var entries = Directory.GetFileSystemEntries(dir);

                foreach (var entry in entries)
                {
                    if (!IsDirectoryOrSymlinkToDirectory(entry)) continue;

                    var skillName = Path.GetFileName(entry);
                    
                    // Deduplicate: only add if we haven't seen this skill name yet
                    if (!seen.Add(skillName)) continue;

                    var skillPath = Path.Combine(entry, "SKILL.md");
                    if (!File.Exists(skillPath)) continue;

                    var content = File.ReadAllText(skillPath);
                    var isProjectLocal = dir.Contains(currentDir, StringComparison.Ordinal);

                    skills.Add(new Skill
                    {
                        Name = skillName,
                        Description = YamlHelper.ExtractYamlField(content, "description"),
                        Location = isProjectLocal ? "project" : "global",
                        Path = PathHelper.NormalizePath(entry)
                    });
                }
            }
            catch
            {
                // Skip directories we can't read
            }
        }

        return skills;
    }

    /// <summary>
    /// Find specific skill by name
    /// </summary>
    /// <param name="skillName">Name of the skill to find</param>
    /// <returns>SkillLocation if found, null otherwise</returns>
    public static SkillLocation? FindSkill(string skillName)
    {
        var dirs = DirectoryHelper.GetSearchDirs();

        foreach (var dir in dirs)
        {
            var skillPath = Path.Combine(dir, skillName, "SKILL.md");
            if (File.Exists(skillPath))
            {
                return new SkillLocation
                {
                    Path = PathHelper.NormalizePath(skillPath),
                    BaseDir = PathHelper.NormalizePath(Path.Combine(dir, skillName)),
                    Source = PathHelper.NormalizePath(dir)
                };
            }
        }

        return null;
    }
}
