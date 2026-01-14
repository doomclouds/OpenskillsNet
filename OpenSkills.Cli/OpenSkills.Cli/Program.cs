using OpenSkills.Cli.Commands;
using OpenSkills.Cli.Models;

namespace OpenSkills.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return await Task.FromResult(0);
        }

        var cmd = args[0].ToLowerInvariant();

        try
        {
            switch (cmd)
            {
                case "list":
                    ListCommand.Execute();
                    break;

                case "install":
                    await HandleInstall(args);
                    break;

                case "read":
                    if (args.Length < 2)
                    {
                        Console.Error.WriteLine("Usage: openskills read <skill-name>");
                        return await Task.FromResult(1);
                    }
                    ReadCommand.Execute(args[1]);
                    break;

                case "sync":
                    HandleSync(args);
                    break;

                case "manage":
                    ManageCommand.Execute();
                    break;

                case "remove":
                case "rm":
                    if (args.Length < 2)
                    {
                        Console.Error.WriteLine("Usage: openskills remove <skill-name>");
                        return await Task.FromResult(1);
                    }
                    RemoveCommand.Execute(args[1]);
                    break;

                case "help":
                case "--help":
                case "-h":
                    ShowHelp();
                    break;

                default:
                    Console.Error.WriteLine($"Unknown command: {cmd}");
                    ShowHelp();
                    return await Task.FromResult(1);
            }

            return await Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return await Task.FromResult(1);
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("Universal skills loader for AI coding agents\n");
        Console.WriteLine("Usage: openskills <command> [options]\n");
        Console.WriteLine("Commands:");
        Console.WriteLine("  list                 List all installed skills");
        Console.WriteLine("  install <source>     Install skill from GitHub or Git URL");
        Console.WriteLine("  read <skill-name>    Read skill to stdout (for AI agents)");
        Console.WriteLine("  sync                 Update AGENTS.md with installed skills");
        Console.WriteLine("  manage               Interactively manage (remove) installed skills");
        Console.WriteLine("  remove|rm <name>     Remove specific skill");
    }

    static async Task HandleInstall(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: openskills install <source> [--global|-g] [--universal|-u] [--yes|-y]");
            return;
        }

        var source = args[1];
        var options = new InstallOptions();

        for (int i = 2; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "--global":
                case "-g":
                    options.Global = true;
                    break;
                case "--universal":
                case "-u":
                    options.Universal = true;
                    break;
                case "--yes":
                case "-y":
                    options.Yes = true;
                    break;
                default:
                    // ignore unknown options for now
                    break;
            }
        }

        await InstallCommand.Execute(source, options);
    }

    static void HandleSync(string[] args)
    {
        var yes = false;
        string? output = null;

        for (int i = 1; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "--yes":
                case "-y":
                    yes = true;
                    break;
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        output = args[i + 1];
                        i++;
                    }
                    break;
                default:
                    // ignore unknown
                    break;
            }
        }

        SyncCommand.Execute(yes, output);
    }
}
