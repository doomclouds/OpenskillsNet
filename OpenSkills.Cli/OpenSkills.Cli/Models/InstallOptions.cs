namespace OpenSkills.Cli.Models;

/// <summary>
/// Installation options for skills
/// </summary>
public class InstallOptions
{
    public bool Global { get; set; }
    public bool Universal { get; set; }
    public bool Yes { get; set; }
    public string? Branch { get; set; }
}
