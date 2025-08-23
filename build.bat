@echo off
REM ===============================
REM Build script for File Batch Renamer
REM ===============================

echo Cleaning old build files...

REM Change directory to the script's folder
cd /d "%~dp0"

REM Remove old build and dist folders if they exist
if exist build rmdir /s /q build
if exist dist rmdir /s /q dist
if exist __pycache__ rmdir /s /q __pycache__

echo Starting new build...

REM Run PyInstaller with the spec file
powershell -Command "pyinstaller GLBatchRenamer.spec"

echo.
echo Build complete! Check the 'dist' folder for File Batch Renamer.exe
pause
