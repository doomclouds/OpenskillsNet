namespace OpenSkills.Cli.Models;

/// <summary>
/// YAML frontmatter metadata from SKILL.md files
/// </summary>
public class SkillMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Context { get; set; }
}
