using System.Text.RegularExpressions;

namespace OpenSkills.Cli.Utils;

/// <summary>
/// Helper for parsing YAML frontmatter from SKILL.md files
/// </summary>
public static class YamlHelper
{
    /// <summary>
    /// Check if content has valid YAML frontmatter
    /// </summary>
    /// <param name="content">File content to check</param>
    /// <returns>True if content starts with ---</returns>
    public static bool HasValidFrontmatter(string content) =>
        content.TrimStart().StartsWith("---");

    /// <summary>
    /// Extract field value from YAML frontmatter
    /// </summary>
    /// <param name="content">File content containing YAML</param>
    /// <param name="field">Field name to extract</param>
    /// <returns>Field value or empty string if not found</returns>
    public static string ExtractYamlField(string content, string field)
    {
        var pattern = $@"^{field}:\s*(.+?)$";
        var match = Regex.Match(content, pattern, RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }
}
