namespace OpenSkills.Cli.Models;

/// <summary>
/// Represents a skill file location with path and base directory
/// </summary>
public class SkillLocation
{
    public string Path { get; set; } = string.Empty;
    public string BaseDir { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
