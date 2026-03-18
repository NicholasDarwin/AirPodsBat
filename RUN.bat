@echo off
REM AirPods Battery Overlay - One-Click Launcher
REM This script will build and run the application automatically

echo.
echo ========================================
echo   AirPods Battery Overlay
echo   Launcher
echo ========================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET 6.0 SDK is not installed or not in PATH
    echo.
    echo Please install .NET 6.0 SDK from:
    echo https://dotnet.microsoft.com/download/dotnet/6.0
    echo.
    pause
    exit /b 1
)

echo [OK] .NET SDK found
dotnet --version
echo.

REM Build the project
echo Building AirPods Battery Overlay...
dotnet build -c Release

if errorlevel 1 (
    echo.
    echo ERROR: Build failed. Check the error messages above.
    echo.
    pause
    exit /b 1
)

echo.
echo [OK] Build successful!
echo.

REM Run the application
echo Launching AirPods Battery Overlay...
echo.

cd /d "%~dp0"
start "" bin\Release\net6.0-windows10.0.19041.0\AirPodBat.exe

echo Application launched! Look for the overlay in the bottom-right corner of your screen.
echo.
timeout /t 3 /nobreak
