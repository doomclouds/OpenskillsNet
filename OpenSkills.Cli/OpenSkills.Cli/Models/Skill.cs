namespace OpenSkills.Cli.Models;

/// <summary>
/// Represents an installed skill with metadata
/// </summary>
public class Skill
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty; // "project" or "global"
    public string Path { get; set; } = string.Empty;
}
