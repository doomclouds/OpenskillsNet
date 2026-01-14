using Spectre.Console;
using OpenSkills.Cli.Utils;

namespace OpenSkills.Cli.Commands;

/// <summary>
/// Command to interactively manage (remove) installed skills
/// </summary>
public static class ManageCommand
{
    /// <summary>
    /// Execute manage command
    /// </summary>
    public static void Execute()
    {
        var skills = SkillsHelper.FindAllSkills();

        if (skills.Count == 0)
        {
            AnsiConsole.MarkupLine("No skills installed.");
            return;
        }

        // Sort: project first
        var sorted = skills.OrderBy(s => s.Location != "project" ? 1 : 0)
                          .ThenBy(s => s.Name)
                          .ToList();

        var prompt = new MultiSelectionPrompt<string>()
            .Title("Select skills to remove")
            .PageSize(15);

        foreach (var skill in sorted)
        {
            prompt.AddChoice(skill.Name);
        }

        var toRemove = AnsiConsole.Prompt(prompt);

        if (toRemove.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No skills selected for removal.[/]");
            return;
        }

        // Remove selected skills
        foreach (var skillName in toRemove)
        {
            var skill = SkillsHelper.FindSkill(skillName);
            if (skill is not null)
            {
                Directory.Delete(skill.BaseDir, recursive: true);
                var currentDir = Directory.GetCurrentDirectory();
                var location = skill.Source.Contains(currentDir, StringComparison.Ordinal) ? "project" : "global";
                AnsiConsole.MarkupLine($"[green]✓[/] Removed: {skillName} ({location})");
            }
        }

        AnsiConsole.MarkupLine($"[green]\n✓ Removed {toRemove.Count} skill(s)[/]");
    }
}
