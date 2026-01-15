using OpenSkills.Cli.Commands;
using OpenSkills.Cli.Models;

if (args.Length == 0)
{
    ShowHelp();
    return 0;
}

var cmd = args[0].ToLowerInvariant();

try
{
    switch (cmd)
    {
        case "list":
            ListCommand.Execute();
            return 0;

        case "install":
            return await HandleInstall(args);

        case "read":
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: openskills read <skill-name>");
                return 1;
            }

            ReadCommand.Execute(args[1]);
            return 0;

        case "sync":
            HandleSync(args);
            return 0;

        case "manage":
            ManageCommand.Execute();
            return 0;

        case "remove" or "rm":
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: openskills remove <skill-name>");
                return 1;
            }

            RemoveCommand.Execute(args[1]);
            return 0;

        case "version" or "--version" or "-v":
            VersionCommand.Execute();
            return 0;

        case "help" or "--help" or "-h":
            ShowHelp();
            return 0;

        default:
            Console.Error.WriteLine($"Unknown command: {cmd}");
            ShowHelp();
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
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
    Console.WriteLine("  version              Show version information");
}

static async Task<int> HandleInstall(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: openskills install <source> [--global|-g] [--universal|-u] [--yes|-y]");
        return 1;
    }

    var source = args[1];
    var options = new InstallOptions();

    for (int i = 2; i < args.Length; i++)
    {
        var arg = args[i];
        switch (arg)
        {
            case "--global" or "-g":
                options.Global = true;
                break;
            case "--universal" or "-u":
                options.Universal = true;
                break;
            case "--yes" or "-y":
                options.Yes = true;
                break;
        }
    }

    await InstallCommand.Execute(source, options);
    return 0;
}

static void HandleSync(string[] args)
{
    var yes = false;
    string? output = null;

    for (int i = 1; i < args.Length; i++)
    {
        var arg = args[i];
        switch (arg)
        {
            case "--yes" or "-y":
                yes = true;
                break;
            case "--output" or "-o":
                if (i + 1 < args.Length)
                {
                    output = args[i + 1];
                    i++;
                }

                break;
        }
    }

    SyncCommand.Execute(yes, output);
}