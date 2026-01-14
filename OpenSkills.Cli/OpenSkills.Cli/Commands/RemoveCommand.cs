using System.IO;
using Spectre.Console;
using OpenSkills.Cli.Utils;

namespace OpenSkills.Cli.Commands;

/// <summary>
/// Command to remove an installed skill
/// </summary>
public static class RemoveCommand
{
    /// <summary>
    /// Execute remove command
    /// </summary>
    /// <param name="skillName">Name of the skill to remove</param>
    public static void Execute(string skillName)
    {
        var skill = SkillsHelper.FindSkill(skillName);

        if (skill is null)
        {
            AnsiConsole.MarkupLine($"[red]Error: Skill '{skillName}' not found[/]");
            Environment.Exit(1);
            return;
        }

        Directory.Delete(skill.BaseDir, recursive: true);

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var location = skill.Source.Contains(homeDir) ? "global" : "project";
        
        AnsiConsole.MarkupLine($"[green]✓[/] Removed: {skillName}");
        AnsiConsole.MarkupLine($"   From: {location} ({skill.Source})");
    }
}
