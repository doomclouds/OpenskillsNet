@echo off
REM OpenSkills C# CLI Deployment Script (CMD version)
REM This script builds and installs the C# CLI globally on Windows

setlocal enabledelayedexpansion

set "INSTALL_PATH=%LOCALAPPDATA%\openskills\bin"
set "FORCE=0"

REM Parse arguments
:parse_args
if "%~1"=="" goto :end_parse
if /i "%~1"=="-Force" set "FORCE=1"
if /i "%~1"=="--force" set "FORCE=1"
if /i "%~1"=="-InstallPath" (
    set "INSTALL_PATH=%~2"
    shift
)
if /i "%~1"=="--install-path" (
    set "INSTALL_PATH=%~2"
    shift
)
shift
goto :parse_args
:end_parse

echo OpenSkills C# CLI Deployment Script
echo ====================================
echo.

REM Check if .NET SDK is installed
echo Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo Error: .NET SDK not found. Please install .NET 10.0 SDK or later.
    echo Download from: https://dotnet.microsoft.com/download
    exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version') do set DOTNET_VERSION=%%v
echo Found .NET SDK version: !DOTNET_VERSION!
echo.

REM Get the script directory (project root)
set "SCRIPT_DIR=%~dp0"
set "PROJECT_PATH=%SCRIPT_DIR%OpenSkills.Cli\OpenSkills.Cli\OpenSkills.Cli.csproj"

if not exist "%PROJECT_PATH%" (
    echo Error: Project file not found at: %PROJECT_PATH%
    exit /b 1
)

REM Publish the application (publish automatically builds)
echo Building and publishing Release version...
set "PUBLISH_DIR=%SCRIPT_DIR%OpenSkills.Cli\OpenSkills.Cli\bin\Release\net10.0\publish"
dotnet publish "%PROJECT_PATH%" --configuration Release --output "%PUBLISH_DIR%"
if errorlevel 1 (
    echo Build/Publish failed!
    exit /b 1
)
echo Build and publish successful!
echo.

REM Create install directory
echo Preparing install directory: %INSTALL_PATH%
if exist "%INSTALL_PATH%" (
    if "%FORCE%"=="1" (
        echo Removing existing installation...
        rmdir /s /q "%INSTALL_PATH%"
    ) else (
        echo Warning: Install directory already exists. Use -Force to overwrite.
        set /p RESPONSE="Continue anyway? (y/N): "
        if /i not "!RESPONSE!"=="y" (
            echo Installation cancelled.
            exit /b 0
        )
        rmdir /s /q "%INSTALL_PATH%"
    )
)

mkdir "%INSTALL_PATH%" 2>nul

REM Copy files
echo Copying files...
xcopy /E /I /Y "%PUBLISH_DIR%\*" "%INSTALL_PATH%\"
if errorlevel 1 (
    echo Error copying files!
    exit /b 1
)

REM Copy wrapper scripts
set "SCRIPT_DIR=%~dp0"
if exist "%SCRIPT_DIR%openskills.ps1" (
    copy /Y "%SCRIPT_DIR%openskills.ps1" "%INSTALL_PATH%\" >nul
    echo Copied wrapper script: openskills.ps1
)
if exist "%SCRIPT_DIR%openskills.cmd" (
    copy /Y "%SCRIPT_DIR%openskills.cmd" "%INSTALL_PATH%\" >nul
    echo Copied wrapper script: openskills.cmd
)

echo Files copied successfully!
echo.

REM Add to PATH
echo Adding to PATH environment variable...
REM Use PowerShell to reliably update PATH (avoids setx length limitations)
powershell -NoProfile -Command "$installPath = '%INSTALL_PATH%'; $currentPath = [Environment]::GetEnvironmentVariable('Path', 'User'); if ($currentPath -notlike ('*' + $installPath + '*')) { $newPath = $currentPath + ';' + $installPath; [Environment]::SetEnvironmentVariable('Path', $newPath, 'User'); Write-Host ('Added ' + $installPath + ' to user PATH') } else { Write-Host 'PATH already contains install directory' }"
if errorlevel 1 (
    echo Warning: Could not update PATH automatically.
    echo Please manually add %INSTALL_PATH% to your PATH environment variable.
    echo.
    echo To add manually:
    echo   1. Open System Properties ^> Environment Variables
    echo   2. Edit User PATH variable
    echo   3. Add: %INSTALL_PATH%
) else (
    echo.
    echo Note: You may need to restart your terminal for PATH changes to take effect.
)

REM Verify installation
echo.
echo Verifying installation...
set "EXE_PATH=%INSTALL_PATH%\OpenSkills.Cli.exe"
if exist "%EXE_PATH%" (
    echo Installation successful!
    echo.
    echo Executable location: %EXE_PATH%
    echo.
    echo To use OpenSkills CLI:
    echo   1. Restart your terminal
    echo   2. Use the 'openskills' command:
    echo      - PowerShell: openskills list
    echo      - CMD: openskills list
    echo.
    echo Note: Wrapper scripts (openskills.ps1 and openskills.cmd) have been installed.
) else (
    echo Error: Executable not found at expected location!
    exit /b 1
)

echo.
echo Deployment completed successfully!
endlocal
