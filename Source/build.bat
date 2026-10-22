@echo off
REM Builds GLMapPacker.exe from this Source folder.
REM Requires the .NET SDK (any version 6 or newer): https://dotnet.microsoft.com/download
cd /d "%~dp0"
dotnet build GLMapPacker.csproj -c Release
if errorlevel 1 (
    echo.
    echo Build FAILED.
    pause
    exit /b 1
)
echo.
echo Build complete: %~dp0bin\Release\GLMapPacker.exe
pause
