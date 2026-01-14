using System.Diagnostics;
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
        var (repoUrl, skillSubpath) = ParseGitSource(source);
        if (repoUrl is null)
        {
            AnsiConsole.MarkupLine("[red]Error: Invalid source format[/]");
            AnsiConsole.MarkupLine("Expected: owner/repo, owner/repo/skill-name, git URL, or local path");
            Environment.Exit(1);
            return;
        }

        // Clone and install from git
        var tempDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            $".openskills-temp-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        );
        Directory.CreateDirectory(tempDir);

        try
        {
            var repoDir = Path.Combine(tempDir, "repo");
            var cloneSucceeded = false;
            string? cloneError = null;

            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Cloning repository...", _ =>
                {
                    try
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = $"clone --depth 1 --quiet \"{repoUrl}\" \"{repoDir}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using var process = Process.Start(processInfo);
                        if (process is null)
                        {
                            return;
                        }

                        process.WaitForExit();
                        if (process.ExitCode == 0)
                        {
                            cloneSucceeded = true;
                            return;
                        }

                        cloneError = process.StandardError.ReadToEnd();
                        // Check if repository was actually cloned despite the error
                        if (IsRepositoryCloned(repoDir))
                        {
                            // Repository exists, likely a Gitea/server-side issue, continue
                            AnsiConsole.MarkupLine("[yellow]Warning: Git clone reported errors, but repository appears to be cloned successfully.[/]");
                            if (!string.IsNullOrEmpty(cloneError))
                            {
                                AnsiConsole.MarkupLine($"[dim]{cloneError}[/]");
                            }
                            cloneSucceeded = true;
                        }
                        else
                        {
                            // Real failure
                            throw new Exception($"Git clone failed: {cloneError}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Check if repository exists despite exception
                        if (IsRepositoryCloned(repoDir))
                        {
                            AnsiConsole.MarkupLine("[yellow]Warning: Git clone reported errors, but repository appears to be cloned successfully.[/]");
                            AnsiConsole.MarkupLine($"[dim]{ex.Message}[/]");
                            cloneSucceeded = true;
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red]Failed to clone repository[/]");
                            AnsiConsole.MarkupLine($"[dim]{ex.Message}[/]");
                            AnsiConsole.MarkupLine("[yellow]\nTip: For private repos, ensure git SSH keys or credentials are configured[/]");
                            Environment.Exit(1);
                        }
                    }
                });

            if (!cloneSucceeded)
            {
                AnsiConsole.MarkupLine("[red]Failed to clone repository[/]");
                if (!string.IsNullOrEmpty(cloneError))
                {
                    AnsiConsole.MarkupLine($"[dim]{cloneError}[/]");
                }
                AnsiConsole.MarkupLine("[yellow]\nTip: For private repos, ensure git SSH keys or credentials are configured[/]");
                Environment.Exit(1);
                return;
            }

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

    private static (string? RepoUrl, string SkillSubpath) ParseGitSource(string source)
    {
        if (IsGitUrl(source))
        {
            return (source, string.Empty);
        }

        // GitHub shorthand: owner/repo or owner/repo/skill-path
        var parts = source.Split('/');
        return parts.Length switch
        {
            2 => ($"https://github.com/{source}", string.Empty),
            > 2 => ($"https://github.com/{parts[0]}/{parts[1]}", string.Join("/", parts.Skip(2))),
            _ => (null, string.Empty)
        };
    }

    private static bool IsLocalPath(string source) =>
        source.StartsWith('/') ||
        source.StartsWith("./", StringComparison.Ordinal) ||
        source.StartsWith("../", StringComparison.Ordinal) ||
        source.StartsWith("~/", StringComparison.Ordinal);

    private static bool IsRepositoryCloned(string repoDir) =>
        Directory.Exists(repoDir) && Directory.GetFileSystemEntries(repoDir).Length > 0;

    private static bool IsGitUrl(string source) =>
        source.StartsWith("git@", StringComparison.Ordinal) ||
        source.StartsWith("git://", StringComparison.Ordinal) ||
        source.StartsWith("http://", StringComparison.Ordinal) ||
        source.StartsWith("https://", StringComparison.Ordinal) ||
        source.EndsWith(".git", StringComparison.Ordinal);

    private static string ExpandPath(string source) =>
        source.StartsWith("~/", StringComparison.Ordinal)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                source[2..])
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
            var currentDir = Directory.GetCurrentDirectory();
            var isProject = targetDir.Contains(currentDir, StringComparison.Ordinal);
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
        var content = await File.ReadAllTextAsync(skillMdPath);

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
        
        if (!ValidateInstallationPath(targetPath, targetDir))
        {
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
        var content = await File.ReadAllTextAsync(skillMdPath);
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
        
        if (!ValidateInstallationPath(targetPath, targetDir))
        {
            return;
        }
        
        CopyDirectory(skillDir, targetPath);

        AnsiConsole.MarkupLine($"[green]✓[/] Installed: {skillName}");
        AnsiConsole.MarkupLine($"   Location: {targetPath}");
    }

    private static Task InstallFromRepo(string repoDir, string targetDir, InstallOptions options)
    {
        // Check for .cursor folder in repository root
        var repoCursorPath = Path.Combine(repoDir, ".cursor");
        var hasRepoCursor = Directory.Exists(repoCursorPath);
        var shouldCopyRepoCursor = false;

        if (hasRepoCursor)
        {
            shouldCopyRepoCursor = options.Yes || AnsiConsole.Confirm(
                "[yellow]Repository contains .cursor folder in root. Copy to project root?[/]",
                false
            );
        }

        // Find all skills
        var skillDirs = FindSkills(repoDir);

        if (skillDirs.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error: No SKILL.md files found in repository[/]");
            Environment.Exit(1);
            return Task.CompletedTask;
        }

        AnsiConsole.MarkupLine($"[dim]Found {skillDirs.Count} skill(s)\n[/]");

        // Build skill info list
        var skillInfos = new List<(string SkillDir, string SkillName, string Description, string TargetPath, long Size)>();
        
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
            var size = GetDirectorySize(skillDir);

            skillInfos.Add((skillDir, skillName, description, targetPath, size));
        }

        if (skillInfos.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error: No valid SKILL.md files found[/]");
            Environment.Exit(1);
            return Task.CompletedTask;
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
                // Default select skills that already exist
                var choice = prompt.AddChoice(info.SkillName);
                if (Directory.Exists(info.TargetPath))
                {
                    choice.Select();
                }
            }

            var selected = AnsiConsole.Prompt(prompt);

            if (selected.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No skills selected. Installation cancelled.[/]");
                return Task.CompletedTask;
            }

            skillsToInstall = skillInfos.Where(info => selected.Contains(info.SkillName)).ToList();
        }

        // Install selected skills
        var currentDir = Directory.GetCurrentDirectory();
        var isProject = targetDir == Path.Combine(currentDir, ".claude/skills");
        var installedCount = 0;

        // Check for existing skills and prompt once for all overwrites
        var existingSkills = skillsToInstall.Where(info => Directory.Exists(info.TargetPath)).ToList();

        if (existingSkills.Count > 0 && !options.Yes)
        {
            AnsiConsole.MarkupLine($"[yellow]The following {existingSkills.Count} skill(s) already exist:[/]");
            foreach (var existing in existingSkills)
            {
                AnsiConsole.MarkupLine($"  [dim]- {existing.SkillName}[/]");
            }
            AnsiConsole.MarkupLine("");

            if (!AnsiConsole.Confirm("[yellow]Overwrite all existing skills?[/]", false))
            {
                AnsiConsole.MarkupLine("[yellow]Installation cancelled.[/]");
                return Task.CompletedTask;
            }
        }

        foreach (var info in skillsToInstall)
        {
            // Check marketplace conflicts (global install only)
            if (!isProject && MarketplaceSkills.AnthropicMarketplaceSkills.Contains(info.SkillName))
            {
                AnsiConsole.MarkupLine($"[yellow]\n⚠️  Warning: '{info.SkillName}' matches an Anthropic marketplace skill[/]");
                AnsiConsole.MarkupLine("[dim]   Installing globally may conflict with Claude Code plugins.[/]");
                AnsiConsole.MarkupLine("[dim]   If you re-enable Claude plugins, this will be overwritten.[/]");
            }

            Directory.CreateDirectory(targetDir);
            
            if (!ValidateInstallationPath(info.TargetPath, targetDir))
            {
                continue;
            }
            
            CopyDirectory(info.SkillDir, info.TargetPath);

            AnsiConsole.MarkupLine($"[green]✓[/] Installed: {info.SkillName}");
            installedCount++;
        }

        AnsiConsole.MarkupLine($"[green]\n✓ Installation complete: {installedCount} skill(s) installed[/]");

        // Copy repository root .cursor folder if requested
        if (shouldCopyRepoCursor)
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var targetCursorPath = Path.Combine(projectRoot, ".cursor");
            
            try
            {
                if (Directory.Exists(targetCursorPath))
                {
                    Directory.Delete(targetCursorPath, recursive: true);
                }
                CopyDirectory(repoCursorPath, targetCursorPath, true);
                AnsiConsole.MarkupLine($"[green]✓[/] Copied .cursor folder to project root");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Failed to copy .cursor folder: {ex.Message}[/]");
            }
        }
        
        return Task.CompletedTask;
    }

    private static List<string> FindSkills(string dir)
    {
        var skills = new List<string>();

        try
        {
            foreach (var entry in Directory.GetFileSystemEntries(dir))
            {
                if (!Directory.Exists(entry)) continue;

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
        catch
        {
            // Skip directories we can't read
        }

        return skills;
    }

    private static Task<bool> WarnIfConflict(string skillName, string targetPath, bool isProject, bool skipPrompt)
    {
        // Check if overwriting existing skill
        if (Directory.Exists(targetPath))
        {
            if (skipPrompt)
            {
                AnsiConsole.MarkupLine($"[dim]Overwriting: {skillName}[/]");
                return Task.FromResult(true);
            }

            if (!AnsiConsole.Confirm($"[yellow]Skill '{skillName}' already exists. Overwrite?[/]", false))
            {
                return Task.FromResult(false);
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

        return Task.FromResult(true);
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

    private static bool ValidateInstallationPath(string targetPath, string targetDir)
    {
        var resolvedTargetPath = Path.GetFullPath(targetPath);
        var resolvedTargetDir = Path.GetFullPath(targetDir);
        if (resolvedTargetPath.StartsWith(resolvedTargetDir + Path.DirectorySeparatorChar))
        {
            return true;
        }

        AnsiConsole.MarkupLine("[red]Security error: Installation path outside target directory[/]");
        Environment.Exit(1);
        return false;
    }

    private static void CopyDirectory(string sourceDir, string targetDir, bool includeCursorFolder = false)
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
            
            // Skip .cursor folder unless explicitly included
            if (dirName.Equals(".cursor", StringComparison.OrdinalIgnoreCase) && !includeCursorFolder)
            {
                continue;
            }
            
            var destDir = Path.Combine(targetDir, dirName);
            CopyDirectory(dir, destDir, includeCursorFolder);
        }
    }
}
