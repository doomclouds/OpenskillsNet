using Spectre.Console;
using OpenSkills.Cli.Utils;

namespace OpenSkills.Cli.Commands;

/// <summary>
/// Command to list all installed skills
/// </summary>
public static class ListCommand
{
    /// <summary>
    /// Execute list command
    /// </summary>
    public static void Execute()
    {
        AnsiConsole.MarkupLine("[bold]Available Skills:[/]\n");

        var skills = SkillsHelper.FindAllSkills();

        if (skills.Count == 0)
        {
            AnsiConsole.MarkupLine("No skills installed.\n");
            AnsiConsole.MarkupLine("Install skills:");
            AnsiConsole.MarkupLine("  [cyan]openskills install anthropics/skills[/]         [dim]# Project (default)[/]");
            AnsiConsole.MarkupLine("  [cyan]openskills install owner/skill --global[/]     [dim]# Global (advanced)[/]");
            return;
        }

        // Sort: project skills first, then global, alphabetically within each
        var sorted = skills.OrderBy(s => s.Location != "project" ? 1 : 0)
                           .ThenBy(s => s.Name)
                           .ToList();

        // Display with inline location labels
        foreach (var skill in sorted)
        {
            var locationLabel = skill.Location switch
            {
                "project" => "[blue](project)[/]",
                _ => "[dim](global)[/]"
            };

            AnsiConsole.MarkupLine($"  [bold]{skill.Name.PadRight(25)}[/] {locationLabel}");
            AnsiConsole.MarkupLine($"    [dim]{skill.Description}[/]\n");
        }

        // Summary
        var projectCount = skills.Count(s => s.Location == "project");
        var globalCount = skills.Count(s => s.Location == "global");

        AnsiConsole.MarkupLine($"[dim]Summary: {projectCount} project, {globalCount} global ({skills.Count} total)[/]");
    }
}
