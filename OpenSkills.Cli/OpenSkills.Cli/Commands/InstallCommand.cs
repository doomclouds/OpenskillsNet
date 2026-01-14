using System.Diagnostics;
using System.IO;
using Spectre.Console;
using OpenSkills.Cli.Models;
using OpenSkills.Cli.Utils;

namespace OpenSkills.Cli.Commands;

/// <summary>
/// Command to install skills from GitHub, local path, or Git URL
/// </summary>
public static class InstallCommand
{
    /// <summary>
    /// Execute install command
    /// </summary>
    public static async Task Execute(string source, InstallOptions options)
    {
        var folder = options.Universal ? ".agent/skills" : ".claude/skills";
        var isProject = !options.Global; // Default to project unless --global specified
        var targetDir = DirectoryHelper.GetSkillsDir(isProject, options.Universal);

        var location = isProject
            ? $"[blue]project ({folder})[/]"
            : $"[dim]global (~/{folder})[/]";

        AnsiConsole.MarkupLine($"Installing from: [cyan]{source}[/]");
        AnsiConsole.MarkupLine($"Location: {location}\n");

        // Handle local path installation
        if (IsLocalPath(source))
        {
            var localPath = ExpandPath(source);
            await InstallFromLocal(localPath, targetDir, options);
            PrintPostInstallHints(isProject);
            return;
        }

        // Parse git source
        string repoUrl;
        string skillSubpath = string.Empty;

        if (IsGitUrl(source))
        {
            // Full git URL (SSH, HTTPS, git://)
            repoUrl = source;
        }
        else
        {
            // GitHub shorthand: owner/repo or owner/repo/skill-path
            var parts = source.Split('/');
            if (parts.Length == 2)
            {
                repoUrl = $"https://github.com/{source}";
            }
            else if (parts.Length > 2)
            {
                repoUrl = $"https://github.com/{parts[0]}/{parts[1]}";
                skillSubpath = string.Join("/", parts.Skip(2));
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error: Invalid source format[/]");
                AnsiConsole.MarkupLine("Expected: owner/repo, owner/repo/skill-name, git URL, or local path");
                Environment.Exit(1);
                return;
            }
        }

        // Clone and install from git
        var tempDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            $".openskills-temp-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        );
        Directory.CreateDirectory(tempDir);

        try
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Cloning repository...", ctx =>
                {
                    try
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = $"clone --depth 1 --quiet \"{repoUrl}\" \"{Path.Combine(tempDir, "repo")}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using var process = Process.Start(processInfo);
                        if (process != null)
                        {
                            process.WaitForExit();
                            if (process.ExitCode != 0)
                            {
                                var error = process.StandardError.ReadToEnd();
                                throw new Exception($"Git clone failed: {error}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine("[red]Failed to clone repository[/]");
                        AnsiConsole.MarkupLine($"[dim]{ex.Message}[/]");
                        AnsiConsole.MarkupLine("[yellow]\nTip: For private repos, ensure git SSH keys or credentials are configured[/]");
                        Environment.Exit(1);
                    }
                });

            var repoDir = Path.Combine(tempDir, "repo");

            if (!string.IsNullOrEmpty(skillSubpath))
            {
                await InstallSpecificSkill(repoDir, skillSubpath, targetDir, isProject, options);
            }
            else
            {
                await InstallFromRepo(repoDir, targetDir, options);
            }
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }

        PrintPostInstallHints(isProject);
    }

    private static bool IsLocalPath(string source) =>
        source.StartsWith("/") ||
        source.StartsWith("./") ||
        source.StartsWith("../") ||
        source.StartsWith("~/");

    private static bool IsGitUrl(string source) =>
        source.StartsWith("git@") ||
        source.StartsWith("git://") ||
        source.StartsWith("http://") ||
        source.StartsWith("https://") ||
        source.EndsWith(".git");

    private static string ExpandPath(string source) =>
        source.StartsWith("~/")
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                source.Substring(2))
            : Path.GetFullPath(source);

    private static void PrintPostInstallHints(bool isProject)
    {
        AnsiConsole.MarkupLine($"\n[dim]Read skill:[/] [cyan]openskills read <skill-name>[/]");
        if (isProject)
        {
            AnsiConsole.MarkupLine($"[dim]Sync to AGENTS.md:[/] [cyan]openskills sync[/]");
        }
    }

    private static async Task InstallFromLocal(string localPath, string targetDir, InstallOptions options)
    {
        if (!Directory.Exists(localPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: Path does not exist: {localPath}[/]");
            Environment.Exit(1);
            return;
        }

        var info = new DirectoryInfo(localPath);
        if (!info.Exists)
        {
            AnsiConsole.MarkupLine("[red]Error: Path must be a directory[/]");
            Environment.Exit(1);
            return;
        }

        // Check if this is a single skill (has SKILL.md) or a directory of skills
        var skillMdPath = Path.Combine(localPath, "SKILL.md");
        if (File.Exists(skillMdPath))
        {
            // Single skill directory
            var isProject = targetDir.Contains(Directory.GetCurrentDirectory());
            await InstallSingleLocalSkill(localPath, targetDir, isProject, options);
        }
        else
        {
            // Directory containing multiple skills
            await InstallFromRepo(localPath, targetDir, options);
        }
    }

    private static async Task InstallSingleLocalSkill(
        string skillDir,
        string targetDir,
        bool isProject,
        InstallOptions options)
    {
        var skillMdPath = Path.Combine(skillDir, "SKILL.md");
        var content = File.ReadAllText(skillMdPath);

        if (!YamlHelper.HasValidFrontmatter(content))
        {
            AnsiConsole.MarkupLine("[red]Error: Invalid SKILL.md (missing YAML frontmatter)[/]");
            Environment.Exit(1);
            return;
        }

        var skillName = Path.GetFileName(skillDir);
        var targetPath = Path.Combine(targetDir, skillName);

        var shouldInstall = await WarnIfConflict(skillName, targetPath, isProject, options.Yes);
        if (!shouldInstall)
        {
            AnsiConsole.MarkupLine($"[yellow]Skipped: {skillName}[/]");
            return;
        }

        Directory.CreateDirectory(targetDir);
        
        // Security: ensure target path stays within target directory
        var resolvedTargetPath = Path.GetFullPath(targetPath);
        var resolvedTargetDir = Path.GetFullPath(targetDir);
        if (!resolvedTargetPath.StartsWith(resolvedTargetDir + Path.DirectorySeparatorChar))
        {
            AnsiConsole.MarkupLine("[red]Security error: Installation path outside target directory[/]");
            Environment.Exit(1);
            return;
        }

        CopyDirectory(skillDir, targetPath);

        AnsiConsole.MarkupLine($"[green]✓[/] Installed: {skillName}");
        AnsiConsole.MarkupLine($"   Location: {targetPath}");
    }

    private static async Task InstallSpecificSkill(
        string repoDir,
        string skillSubpath,
        string targetDir,
        bool isProject,
        InstallOptions options)
    {
        var skillDir = Path.Combine(repoDir, skillSubpath);
        var skillMdPath = Path.Combine(skillDir, "SKILL.md");

        if (!File.Exists(skillMdPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: SKILL.md not found at {skillSubpath}[/]");
            Environment.Exit(1);
            return;
        }

        // Validate
        var content = File.ReadAllText(skillMdPath);
        if (!YamlHelper.HasValidFrontmatter(content))
        {
            AnsiConsole.MarkupLine("[red]Error: Invalid SKILL.md (missing YAML frontmatter)[/]");
            Environment.Exit(1);
            return;
        }

        var skillName = Path.GetFileName(skillSubpath);
        var targetPath = Path.Combine(targetDir, skillName);

        // Warn about potential conflicts
        var shouldInstall = await WarnIfConflict(skillName, targetPath, isProject, options.Yes);
        if (!shouldInstall)
        {
            AnsiConsole.MarkupLine($"[yellow]Skipped: {skillName}[/]");
            return;
        }

        Directory.CreateDirectory(targetDir);
        
        // Security: ensure target path stays within target directory
        var resolvedTargetPath = Path.GetFullPath(targetPath);
        var resolvedTargetDir = Path.GetFullPath(targetDir);
        if (!resolvedTargetPath.StartsWith(resolvedTargetDir + Path.DirectorySeparatorChar))
        {
            AnsiConsole.MarkupLine("[red]Security error: Installation path outside target directory[/]");
            Environment.Exit(1);
            return;
        }
        
        CopyDirectory(skillDir, targetPath);

        AnsiConsole.MarkupLine($"[green]✓[/] Installed: {skillName}");
        AnsiConsole.MarkupLine($"   Location: {targetPath}");
    }

    private static async Task InstallFromRepo(string repoDir, string targetDir, InstallOptions options)
    {
        // Find all skills
        var skillDirs = FindSkills(repoDir);

        if (skillDirs.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error: No SKILL.md files found in repository[/]");
            Environment.Exit(1);
            return;
        }

        AnsiConsole.MarkupLine($"[dim]Found {skillDirs.Count} skill(s)\n[/]");

        // Build skill info list
        List<(string SkillDir, string SkillName, string Description, string TargetPath, long Size)> skillInfos = [];
        
        foreach (var skillDir in skillDirs)
        {
            var skillMdPath = Path.Combine(skillDir, "SKILL.md");
            var content = File.ReadAllText(skillMdPath);

            if (!YamlHelper.HasValidFrontmatter(content))
            {
                continue;
            }

            var skillName = Path.GetFileName(skillDir);
            var description = YamlHelper.ExtractYamlField(content, "description");
            var targetPath = Path.Combine(targetDir, skillName);

            // Get size
            var size = GetDirectorySize(skillDir);

            skillInfos.Add((skillDir, skillName, description, targetPath, size));
        }

        if (skillInfos.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error: No valid SKILL.md files found[/]");
            Environment.Exit(1);
            return;
        }

        // Interactive selection (unless -y flag or single skill)
        var skillsToInstall = skillInfos;

        if (!options.Yes && skillInfos.Count > 1)
        {
            var prompt = new MultiSelectionPrompt<string>()
                .Title("Select skills to install")
                .PageSize(15);

            foreach (var info in skillInfos)
            {
                prompt.AddChoice(info.SkillName);
            }

            var selected = AnsiConsole.Prompt(prompt);

            if (selected.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No skills selected. Installation cancelled.[/]");
                return;
            }

            skillsToInstall = skillInfos.Where(info => selected.Contains(info.SkillName)).ToList();
        }

        // Install selected skills
        var isProject = targetDir == Path.Combine(Directory.GetCurrentDirectory(), ".claude/skills");
        var installedCount = 0;

        foreach (var info in skillsToInstall)
        {
            // Warn about conflicts
            var shouldInstall = await WarnIfConflict(info.SkillName, info.TargetPath, isProject, options.Yes);
            if (!shouldInstall)
            {
                AnsiConsole.MarkupLine($"[yellow]Skipped: {info.SkillName}[/]");
                continue; // Skip this skill, continue with next
            }

            Directory.CreateDirectory(targetDir);
            
            // Security: ensure target path stays within target directory
            var resolvedTargetPath = Path.GetFullPath(info.TargetPath);
            var resolvedTargetDir = Path.GetFullPath(targetDir);
            if (!resolvedTargetPath.StartsWith(resolvedTargetDir + Path.DirectorySeparatorChar))
            {
                AnsiConsole.MarkupLine("[red]Security error: Installation path outside target directory[/]");
                continue;
            }
            
            CopyDirectory(info.SkillDir, info.TargetPath);

            AnsiConsole.MarkupLine($"[green]✓[/] Installed: {info.SkillName}");
            installedCount++;
        }

        AnsiConsole.MarkupLine($"[green]\n✓ Installation complete: {installedCount} skill(s) installed[/]");
    }

    private static List<string> FindSkills(string dir)
    {
        List<string> skills = [];

        try
        {
            var entries = Directory.GetFileSystemEntries(dir);

            foreach (var entry in entries)
            {
                if (Directory.Exists(entry))
                {
                    var skillMdPath = Path.Combine(entry, "SKILL.md");
                    if (File.Exists(skillMdPath))
                    {
                        skills.Add(entry);
                    }
                    else
                    {
                        skills.AddRange(FindSkills(entry));
                    }
                }
            }
        }
        catch
        {
            // Skip directories we can't read
        }

        return skills;
    }

    private static async Task<bool> WarnIfConflict(string skillName, string targetPath, bool isProject, bool skipPrompt)
    {
        // Check if overwriting existing skill
        if (Directory.Exists(targetPath))
        {
            if (skipPrompt)
            {
                // Auto-overwrite in non-interactive mode
                AnsiConsole.MarkupLine($"[dim]Overwriting: {skillName}[/]");
                return true;
            }

            var shouldOverwrite = AnsiConsole.Confirm(
                $"[yellow]Skill '{skillName}' already exists. Overwrite?[/]",
                false
            );

            if (!shouldOverwrite)
            {
                return false; // Skip this skill, continue with others
            }
        }

        // Warn about marketplace conflicts (global install only)
        if (!isProject && MarketplaceSkills.AnthropicMarketplaceSkills.Contains(skillName))
        {
            AnsiConsole.MarkupLine($"[yellow]\n⚠️  Warning: '{skillName}' matches an Anthropic marketplace skill[/]");
            AnsiConsole.MarkupLine("[dim]   Installing globally may conflict with Claude Code plugins.[/]");
            AnsiConsole.MarkupLine("[dim]   If you re-enable Claude plugins, this will be overwritten.[/]");
            AnsiConsole.MarkupLine("[dim]   Recommend: Use --project flag for conflict-free installation.\n[/]");
        }

        return true; // OK to proceed
    }

    private static long GetDirectorySize(string dirPath)
    {
        long size = 0;

        try
        {
            var entries = Directory.GetFileSystemEntries(dirPath);
            foreach (var entry in entries)
            {
                if (File.Exists(entry))
                {
                    var info = new FileInfo(entry);
                    size += info.Length;
                }
                else if (Directory.Exists(entry))
                {
                    size += GetDirectorySize(entry);
                }
            }
        }
        catch
        {
            // Skip files we can't read
        }

        return size;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes}B",
        < 1048576 => $"{bytes / 1024.0:F1}KB",
        _ => $"{bytes / 1048576.0:F1}MB"
    };

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(targetDir, fileName);
            File.Copy(file, destFile, true);
        }

        // Copy all subdirectories
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            var destDir = Path.Combine(targetDir, dirName);
            CopyDirectory(dir, destDir);
        }
    }
}
