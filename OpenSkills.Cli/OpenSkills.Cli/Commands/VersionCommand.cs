using System.Reflection;

namespace OpenSkills.Cli.Commands;

/// <summary>
/// Command to display version information
/// </summary>
public static class VersionCommand
{
    /// <summary>
    /// Execute version command
    /// </summary>
    public static void Execute()
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Try to get version from AssemblyInformationalVersionAttribute first (most accurate)
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (informationalVersion is { InformationalVersion: not null and var infoVersion } && !string.IsNullOrEmpty(infoVersion))
        {
            // Remove Git commit hash if present (format: "1.3.0+hash")
            var plusIndex = infoVersion.IndexOf('+');
            var versionString = plusIndex >= 0 ? infoVersion[..plusIndex] : infoVersion;
            Console.WriteLine(versionString);
            return;
        }
        
        // Fallback to AssemblyVersion
        var version = assembly.GetName().Version;
        if (version is not null)
        {
            // Format as Major.Minor.Build (remove Revision if it's 0)
            var versionString = version.Revision == 0
                ? $"{version.Major}.{version.Minor}.{version.Build}"
                : version.ToString();
            Console.WriteLine(versionString);
            return;
        }
        
        // Last fallback: use AssemblyFileVersionAttribute
        var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
        if (fileVersion is { Version: not null and var fileVer } && !string.IsNullOrEmpty(fileVer))
        {
            Console.WriteLine(fileVer);
        }
        else
        {
            Console.WriteLine("1.x.x"); // Default fallback
        }
    }
}
