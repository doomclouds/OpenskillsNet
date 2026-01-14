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
        var skills = SkillsHelper.FindAllSkills();

        if (skills.Count == 0)
        {
            Console.WriteLine("No skills installed.");
            return;
        }

        // Sort alphabetically
        var sorted = skills.OrderBy(s => s.Name).ToList();

        // Display only skill names
        foreach (var skill in sorted)
        {
            Console.WriteLine(skill.Name);
        }
    }
}
