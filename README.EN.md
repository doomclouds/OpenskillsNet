# OpenSkills C# CLI Deployment Guide

**[中文](README.md) | [English](README.EN.md)**

This guide explains how to globally deploy the C# version of OpenSkills CLI on Windows systems, enabling it to be executed from any location in cmd or PowerShell.

## Prerequisites

- **.NET 10.0 SDK or higher**
  - Download: https://dotnet.microsoft.com/download
  - Verify installation: Run `dotnet --version`

## Quick Deployment

### Method 1: Using PowerShell Script (Recommended)

1. Open PowerShell as Administrator
2. Navigate to the project root directory
3. Run the deployment script:

```powershell
.\deploy.ps1
```

To force overwrite existing installation:

```powershell
.\deploy.ps1 -Force
```

Custom installation path:

```powershell
.\deploy.ps1 -InstallPath "C:\Tools\openskills\bin"
```

Create self-contained deployment (includes .NET runtime, no need to install .NET separately):

```powershell
.\deploy.ps1 -SelfContained
```

Self-contained deployment generates larger files but can run on systems without .NET runtime installed.

### Method 2: Using CMD Script

1. Open Command Prompt as Administrator
2. Navigate to the project root directory
3. Run the deployment script:

```cmd
deploy.cmd
```

Force overwrite:

```cmd
deploy.cmd -Force
```

## Deployment Process

The deployment script performs the following operations:

1. **Check .NET SDK** - Verify that .NET 10.0 SDK is installed
2. **Build Project** - Compile the C# CLI in Release configuration
3. **Publish Application** - Create publishable file package
4. **Copy Files** - Copy executable files to installation directory (default: `%LOCALAPPDATA%\openskills\bin`)
5. **Add to PATH** - Add installation directory to user PATH environment variable

## Usage After Installation

### Method 1: Using openskills Command (Recommended)

The deployment script automatically installs wrapper scripts. After restarting the terminal, you can use directly:

**PowerShell:**
```powershell
# Direct usage (recommended, requires terminal restart for PATH to take effect)
openskills list
openskills install anthropics/skills
openskills sync

# Or use relative path (if script is in current directory)
.\openskills list
```

**CMD:**
```cmd
openskills list
openskills install anthropics/skills
openskills sync
```

**Note:** In PowerShell, if you encounter execution policy restrictions, you can use:
- `.\openskills.cmd list` (using CMD wrapper script, recommended)
- `.\openskills.ps1 list` (requires execution policy permission)

Wrapper scripts automatically call `OpenSkills.Cli.exe`, no additional configuration needed.

### Method 2: Direct Use of Executable

If wrapper scripts are unavailable, you can use directly:

```cmd
OpenSkills.Cli.exe list
OpenSkills.Cli.exe install anthropics/skills
OpenSkills.Cli.exe sync
```

## Verify Installation

Run the following command to verify successful installation:

**PowerShell:**
```powershell
openskills --version
```

**CMD:**
```cmd
openskills --version
```

Or use the executable directly:
```cmd
OpenSkills.Cli.exe --version
```

## Uninstallation

To uninstall OpenSkills CLI:

1. Remove installation directory from PATH environment variable
2. Delete installation directory (default: `%LOCALAPPDATA%\openskills\bin`)

## Troubleshooting

### PATH Not Taking Effect

If the command is still not found after restarting the terminal:

1. **PowerShell**: Run the following command to refresh environment variables:
   ```powershell
   $env:Path = [System.Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path','User')
   ```

2. **CMD**: Close and reopen Command Prompt

### Build Failures

If errors occur during build:

1. Ensure .NET 10.0 SDK is installed
2. Clean build cache:
   ```cmd
   dotnet clean OpenSkills.sln
   ```
3. Re-run deployment script

### Permission Issues

If you encounter permission errors:

1. Ensure script is run as Administrator
2. Check write permissions for installation directory
3. If PATH update fails, manually add to system environment variables

## Default Installation Paths

- **User-level installation**: `%LOCALAPPDATA%\openskills\bin` (recommended)
- **System-level installation**: `%ProgramFiles%\openskills\bin` (requires Administrator privileges)

## Related Files

- `deploy.ps1` - PowerShell deployment script
- `deploy.cmd` - CMD deployment script
- `openskills.ps1` - PowerShell wrapper script (auto-installed)
- `openskills.cmd` - CMD wrapper script (auto-installed)

## Notes

- Deployment script modifies user PATH environment variable
- If installation directory already exists, will prompt for overwrite by default (use `-Force` to skip prompt)
- Terminal restart required after first use for PATH changes to take effect
