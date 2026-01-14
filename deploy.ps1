# OpenSkills C# CLI Deployment Script
# This script builds and installs the C# CLI globally on Windows

param(
    [switch]$Force,
    [switch]$SelfContained,
    [string]$InstallPath = "$env:LOCALAPPDATA\openskills\bin"
)

$ErrorActionPreference = "Stop"

Write-Host "OpenSkills C# CLI Deployment Script" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is installed
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "Found .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "Error: .NET SDK not found. Please install .NET 10.0 SDK or later." -ForegroundColor Red
    Write-Host "Download from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# Get the script directory (project root)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptDir "OpenSkills.Cli\OpenSkills.Cli\OpenSkills.Cli.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Host "Error: Project file not found at: $projectPath" -ForegroundColor Red
    exit 1
}

# Publish the application (publish automatically builds)
Write-Host ""
$publishDir = Join-Path $scriptDir "OpenSkills.Cli\OpenSkills.Cli\bin\Release\net10.0\publish"

if ($SelfContained) {
    Write-Host "Building and publishing Self-Contained Release version..." -ForegroundColor Yellow
    Write-Host "(This will include .NET runtime, larger file size)" -ForegroundColor Gray
    $publishArgs = @("publish", $projectPath, "--configuration", "Release", "--output", $publishDir, "--self-contained", "true", "-r", "win-x64")
} else {
    Write-Host "Building and publishing Framework-Dependent Release version..." -ForegroundColor Yellow
    Write-Host "(Requires .NET 10.0 Runtime, smaller file size)" -ForegroundColor Gray
    $publishArgs = @("publish", $projectPath, "--configuration", "Release", "--output", $publishDir)
}

$publishOutput = & dotnet $publishArgs 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build/Publish failed!" -ForegroundColor Red
    Write-Host $publishOutput
    exit 1
}
Write-Host "Build and publish successful!" -ForegroundColor Green

# Create install directory
Write-Host ""
Write-Host "Preparing install directory: $InstallPath" -ForegroundColor Yellow
if (Test-Path $InstallPath) {
    if ($Force) {
        Write-Host "Removing existing installation..." -ForegroundColor Yellow
        Remove-Item -Path $InstallPath -Recurse -Force
    } else {
        Write-Host "Warning: Install directory already exists. Use -Force to overwrite." -ForegroundColor Yellow
        $response = Read-Host "Continue anyway? (y/N)"
        if ($response -ne "y" -and $response -ne "Y") {
            Write-Host "Installation cancelled." -ForegroundColor Yellow
            exit 0
        }
        Remove-Item -Path $InstallPath -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

# Copy files
Write-Host "Copying files..." -ForegroundColor Yellow
Copy-Item -Path "$publishDir\*" -Destination $InstallPath -Recurse -Force

# Copy wrapper scripts
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$wrapperScripts = @(
    "openskills.ps1",
    "openskills.cmd"
)

foreach ($scriptName in $wrapperScripts) {
    $script = Join-Path $scriptDir $scriptName
    if (Test-Path $script) {
        Copy-Item -Path $script -Destination $InstallPath -Force
        Write-Host "Copied wrapper script: $scriptName" -ForegroundColor Gray
    }
}

Write-Host "Files copied successfully!" -ForegroundColor Green

# Add to PATH
Write-Host ""
Write-Host "Adding to PATH environment variable..." -ForegroundColor Yellow

$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($currentPath -notlike "*$InstallPath*") {
    $newPath = $currentPath + ";$InstallPath"
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    Write-Host "Added $InstallPath to user PATH" -ForegroundColor Green
    Write-Host ""
    Write-Host "Note: You may need to restart your terminal for PATH changes to take effect." -ForegroundColor Yellow
} else {
    Write-Host "PATH already contains install directory" -ForegroundColor Green
}

# Verify installation
Write-Host ""
Write-Host "Verifying installation..." -ForegroundColor Yellow
$exePath = Join-Path $InstallPath "OpenSkills.Cli.exe"
if (Test-Path $exePath) {
    Write-Host "Installation successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Executable location: $exePath" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To use OpenSkills CLI:" -ForegroundColor Yellow
    Write-Host "  1. Restart your terminal (or run: `$env:Path = [System.Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path','User')`)" -ForegroundColor White
    Write-Host "  2. Use the 'openskills' command:" -ForegroundColor White
    Write-Host "     - PowerShell: openskills list" -ForegroundColor Cyan
    Write-Host "     - CMD: openskills list" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Note: Wrapper scripts (openskills.ps1 and openskills.cmd) have been installed." -ForegroundColor Gray
} else {
    Write-Host "Error: Executable not found at expected location!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Deployment completed successfully!" -ForegroundColor Green
