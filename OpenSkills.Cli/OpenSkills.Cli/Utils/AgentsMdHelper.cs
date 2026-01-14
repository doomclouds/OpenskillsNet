using System.Text.RegularExpressions;
using OpenSkills.Cli.Models;

namespace OpenSkills.Cli.Utils;

/// <summary>
/// Helper for generating and updating AGENTS.md files
/// </summary>
public static class AgentsMdHelper
{
    /// <summary>
    /// Parse skill names currently in AGENTS.md
    /// </summary>
    /// <param name="content">AGENTS.md file content</param>
    /// <returns>List of skill names found in the file</returns>
    public static List<string> ParseCurrentSkills(string content)
    {
        // Match <skill><name>skill-name</name>...</skill>
        var skillRegex = new Regex(@"<skill>[\s\S]*?<name>([^<]+)</name>[\s\S]*?</skill>", RegexOptions.Multiline);
        var matches = skillRegex.Matches(content);

        return (from Match match in matches
                where match.Groups.Count > 1
                select match.Groups[1].Value.Trim()).ToList();
    }

    /// <summary>
    /// Generate skills XML section for AGENTS.md
    /// </summary>
    /// <param name="skills">List of skills to include</param>
    /// <returns>XML string for skills section</returns>
    public static string GenerateSkillsXml(List<Skill> skills)
    {
        var skillTags = string.Join("\n\n", skills.Select(s => $"""
            <skill>
            <name>{s.Name}</name>
            <description>{s.Description}</description>
            <location>{s.Location}</location>
            </skill>
            """));

        return $"""
            <skills_system priority="1">

            ## Available Skills

            <!-- SKILLS_TABLE_START -->
            <usage>
            When users ask you to perform tasks, check if any of the available skills below can help complete the task more effectively. Skills provide specialized capabilities and domain knowledge.

            How to use skills:
            - Invoke: Bash("openskills read <skill-name>")
            - The skill content will load with detailed instructions on how to complete the task
            - Base directory provided in output for resolving bundled resources (references/, scripts/, assets/)

            Usage notes:
            - Only use skills listed in <available_skills> below
            - Do not invoke a skill that is already loaded in your context
            - Each skill invocation is stateless
            </usage>

            <available_skills>

            {skillTags}

            </available_skills>
            <!-- SKILLS_TABLE_END -->

            </skills_system>
            """;
    }

    /// <summary>
    /// Replace or add skills section in AGENTS.md
    /// </summary>
    /// <param name="content">Current file content</param>
    /// <param name="newSection">New skills section to insert</param>
    /// <returns>Updated file content</returns>
    public static string ReplaceSkillsSection(string content, string newSection)
    {
        const string startMarker = "<skills_system";

        // Check for XML markers
        if (content.Contains(startMarker))
        {
            var regex = new Regex(@"<skills_system[^>]*>[\s\S]*?</skills_system>", RegexOptions.Multiline);
            return regex.Replace(content, newSection);
        }

        // Fallback to HTML comments
        const string htmlStartMarker = "<!-- SKILLS_TABLE_START -->";
        const string htmlEndMarker = "<!-- SKILLS_TABLE_END -->";

        if (content.Contains(htmlStartMarker))
        {
            // Extract content without outer XML wrapper
            var innerContent = Regex.Replace(newSection, @"<skills_system[^>]*>|</skills_system>", "");
            var regex = new Regex($@"{htmlStartMarker}[\s\S]*?{htmlEndMarker}", RegexOptions.Multiline);
            return regex.Replace(content, $"{htmlStartMarker}\n{innerContent}\n{htmlEndMarker}");
        }

        // No markers found - append to end of file
        return content.TrimEnd() + "\n\n" + newSection + "\n";
    }

    /// <summary>
    /// Remove skills section from AGENTS.md
    /// </summary>
    /// <param name="content">Current file content</param>
    /// <returns>Updated file content with skills section removed</returns>
    public static string RemoveSkillsSection(string content)
    {
        const string startMarker = "<skills_system";

        // Check for XML markers
        if (content.Contains(startMarker))
        {
            var regex = new Regex(@"<skills_system[^>]*>[\s\S]*?</skills_system>", RegexOptions.Multiline);
            return regex.Replace(content, "<!-- Skills section removed -->");
        }

        // Fallback to HTML comments
        const string htmlStartMarker = "<!-- SKILLS_TABLE_START -->";
        const string htmlEndMarker = "<!-- SKILLS_TABLE_END -->";

        if (content.Contains(htmlStartMarker))
        {
            var regex = new Regex($@"{htmlStartMarker}[\s\S]*?{htmlEndMarker}", RegexOptions.Multiline);
            return regex.Replace(content, $"{htmlStartMarker}\n<!-- Skills section removed -->\n{htmlEndMarker}");
        }

        // No markers found - nothing to remove
        return content;
    }
}
