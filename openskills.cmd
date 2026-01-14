@echo off
REM OpenSkills CLI Wrapper for CMD/PowerShell
REM This script allows using 'openskills' command in both CMD and PowerShell
REM Usage: openskills <command> [arguments]
REM Or: .\openskills.cmd <command> [arguments]

setlocal enabledelayedexpansion

REM Get the directory where this script is located
set "SCRIPT_DIR=%~dp0"
set "EXE_PATH=%SCRIPT_DIR%OpenSkills.Cli.exe"

REM If not found in script directory, check common installation locations
if not exist "%EXE_PATH%" (
    set "EXE_PATH=%LOCALAPPDATA%\openskills\bin\OpenSkills.Cli.exe"
    if not exist "!EXE_PATH!" (
        set "EXE_PATH=%USERPROFILE%\.openskills\bin\OpenSkills.Cli.exe"
        if not exist "!EXE_PATH!" (
            set "EXE_PATH=%ProgramFiles%\openskills\bin\OpenSkills.Cli.exe"
        )
    )
)

REM If still not found, show error
if not exist "%EXE_PATH%" (
    echo Error: OpenSkills.Cli.exe not found. Please run deploy.cmd to install OpenSkills CLI.
    echo.
    echo Expected locations:
    echo   - %LOCALAPPDATA%\openskills\bin\OpenSkills.Cli.exe
    echo   - %USERPROFILE%\.openskills\bin\OpenSkills.Cli.exe
    echo   - %ProgramFiles%\openskills\bin\OpenSkills.Cli.exe
    exit /b 1
)

REM Pass all arguments to the executable
"%EXE_PATH%" %*
exit /b %ERRORLEVEL%
