# AirPods Battery Overlay - One-Click Launcher (PowerShell)
# This script will build and run the application automatically

Write-Host ""
Write-Host "========================================"
Write-Host "  AirPods Battery Overlay"
Write-Host "  Launcher"
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Check if .NET SDK is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "[OK] .NET SDK found" -ForegroundColor Green
    Write-Host $dotnetVersion
    Write-Host ""
} catch {
    Write-Host "ERROR: .NET 6.0 SDK is not installed or not in PATH" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install .NET 6.0 SDK from:" -ForegroundColor Yellow
    Write-Host "https://dotnet.microsoft.com/download/dotnet/6.0"
    Write-Host ""
    Read-Host "Press Enter to close"
    exit 1
}

# Build the project
Write-Host "Building AirPods Battery Overlay..." -ForegroundColor Cyan
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Build failed. Check the error messages above." -ForegroundColor Red
    Write-Host ""
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host ""
Write-Host "[OK] Build successful!" -ForegroundColor Green
Write-Host ""

# Run the application
Write-Host "Launching AirPods Battery Overlay..." -ForegroundColor Cyan
Write-Host ""

$exePath = "$PSScriptRoot\bin\Release\net6.0-windows10.0.19041.0\AirPodBat.exe"
Start-Process -FilePath $exePath

Write-Host "Application launched! Look for the overlay in the bottom-right corner of your screen." -ForegroundColor Green
Write-Host ""
Start-Sleep -Seconds 2
