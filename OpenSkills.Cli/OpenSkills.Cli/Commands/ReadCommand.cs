using Spectre.Console;
using OpenSkills.Cli.Utils;

namespace OpenSkills.Cli.Commands;

/// <summary>
/// Command to read skill content to stdout (for AI agents)
/// </summary>
public static class ReadCommand
{
    /// <summary>
    /// Execute read command
    /// </summary>
    /// <param name="skillName">Name of the skill to read</param>
    public static void Execute(string skillName)
    {
        var skill = SkillsHelper.FindSkill(skillName);

        if (skill is null)
        {
            AnsiConsole.MarkupLine($"[red]Error: Skill '{skillName}' not found[/]");
            AnsiConsole.MarkupLine("\nSearched:");
            AnsiConsole.MarkupLine("  .agent/skills/ (project universal)");
            AnsiConsole.MarkupLine("  ~/.agent/skills/ (global universal)");
            AnsiConsole.MarkupLine("  .claude/skills/ (project)");
            AnsiConsole.MarkupLine("  ~/.claude/skills/ (global)");
            AnsiConsole.MarkupLine("\nInstall skills: openskills install owner/repo");
            Environment.Exit(1);
            return;
        }

        var content = File.ReadAllText(skill.Path);

        // Output in Claude Code format
        Console.WriteLine($"Reading: {skillName}");
        Console.WriteLine($"Base directory: {skill.BaseDir}");
        Console.WriteLine();
        Console.WriteLine(content);
        Console.WriteLine();
        Console.WriteLine($"Skill read: {skillName}");
    }
}
