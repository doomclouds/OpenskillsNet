# OpenSkills CLI Wrapper for PowerShell
# This script allows using 'openskills' command in PowerShell
# Usage: .\openskills.ps1 <command> [arguments]
# Or: openskills <command> [arguments] (after deployment)

[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Arguments
)

# Try to find OpenSkills.Cli.exe
# 1. Check script directory (installation directory)
$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path -Parent $scriptPath
$exePath = Join-Path $scriptDir "OpenSkills.Cli.exe"

# 2. If not found, check common installation locations
if (-not (Test-Path $exePath)) {
    $possiblePaths = @(
        "$env:LOCALAPPDATA\openskills\bin\OpenSkills.Cli.exe",
        "$env:USERPROFILE\.openskills\bin\OpenSkills.Cli.exe",
        "$env:ProgramFiles\openskills\bin\OpenSkills.Cli.exe"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $exePath = $path
            break
        }
    }
}

# 3. If still not found, show error
if (-not (Test-Path $exePath)) {
    Write-Error "OpenSkills.Cli.exe not found. Please run deploy.ps1 to install OpenSkills CLI."
    Write-Host ""
    Write-Host "Expected locations:" -ForegroundColor Yellow
    Write-Host "  - $env:LOCALAPPDATA\openskills\bin\OpenSkills.Cli.exe" -ForegroundColor Gray
    Write-Host "  - $env:USERPROFILE\.openskills\bin\OpenSkills.Cli.exe" -ForegroundColor Gray
    Write-Host "  - $env:ProgramFiles\openskills\bin\OpenSkills.Cli.exe" -ForegroundColor Gray
    exit 1
}

& $exePath $Arguments
exit $LASTEXITCODE
