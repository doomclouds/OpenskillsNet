using Spectre.Console;
using OpenSkills.Cli.Utils;

namespace OpenSkills.Cli.Commands;

/// <summary>
/// Command to sync installed skills to AGENTS.md
/// </summary>
public static class SyncCommand
{
    /// <summary>
    /// Execute sync command
    /// </summary>
    /// <param name="yes">Skip interactive prompts</param>
    /// <param name="output">Output file path (default: AGENTS.md)</param>
    public static void Execute(bool yes = false, string? output = null)
    {
        var outputPath = output ?? "AGENTS.md";
        var outputName = Path.GetFileName(outputPath);

        // Validate output file is markdown
        if (!outputPath.EndsWith(".md"))
        {
            AnsiConsole.MarkupLine("[red]Error: Output file must be a markdown file (.md)[/]");
            Environment.Exit(1);
            return;
        }

        // Create file if it doesn't exist
        if (!File.Exists(outputPath))
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && dir != "." && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var title = outputName.Replace(".md", "");
            File.WriteAllText(outputPath, $"# {title}\n\n");
            AnsiConsole.MarkupLine($"[dim]Created {outputPath}[/]");
        }

        var skills = SkillsHelper.FindAllSkills();

        if (skills.Count == 0)
        {
            AnsiConsole.MarkupLine("No skills installed. Install skills first:");
            AnsiConsole.MarkupLine($"  [cyan]openskills install anthropics/skills --project[/]");
            return;
        }

        // Interactive mode by default (unless -y flag)
        if (!yes)
        {
            // Parse what's currently in output file
            var currentContent = File.ReadAllText(outputPath);
            var currentSkills = AgentsMdHelper.ParseCurrentSkills(currentContent);

            // Sort: project first
            var sorted = skills.OrderBy(s => s.Location != "project" ? 1 : 0)
                              .ThenBy(s => s.Name)
                              .ToList();

            var prompt = new MultiSelectionPrompt<string>()
                .Title($"Select skills to sync to {outputName}")
                .PageSize(15);

            foreach (var skill in sorted)
            {
                var choice = prompt.AddChoice(skill.Name);
                // Default select all project skills (.claude directory)
                if (skill.Location == "project")
                {
                    choice.Select();
                }
                // Also select skills that already exist in AGENTS.md (for global skills)
                else if (currentSkills.Contains(skill.Name))
                {
                    choice.Select();
                }
            }

            var selected = AnsiConsole.Prompt(prompt);

            if (selected.Count == 0)
            {
                // User unchecked everything - remove skills section
                var removeContent = File.ReadAllText(outputPath);
                var removeUpdated = AgentsMdHelper.RemoveSkillsSection(removeContent);
                File.WriteAllText(outputPath, removeUpdated);
                AnsiConsole.MarkupLine($"[green]✓[/] Removed all skills from {outputName}");
                return;
            }

            // Filter skills to selected ones
            skills = skills.Where(s => selected.Contains(s.Name)).ToList();
        }

        var xml = AgentsMdHelper.GenerateSkillsXml(skills);
        var finalContent = File.ReadAllText(outputPath);
        var finalUpdated = AgentsMdHelper.ReplaceSkillsSection(finalContent, xml);

        File.WriteAllText(outputPath, finalUpdated);

        var hadMarkers = finalContent.Contains("<skills_system") || finalContent.Contains("<!-- SKILLS_TABLE_START -->");

        var message = hadMarkers
            ? $"[green]✓[/] Synced {skills.Count} skill(s) to {outputName}"
            : $"[green]✓[/] Added skills section to {outputName} ({skills.Count} skill(s))";
        AnsiConsole.MarkupLine(message);
    }
}
