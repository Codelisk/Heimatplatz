#!/usr/bin/env pwsh
# Bootstrap script for Cake Frosting build (Windows/PowerShell)

param(
    [string]$Target = "Default",
    [string]$Configuration = "Release",
    [switch]$Help
)

$ErrorActionPreference = "Stop"
$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if ($Help) {
    Write-Host @"
Heimatplatz Build Script (Cake Frosting)

Usage:
    ./build.ps1 [options]

Options:
    -Target <task>        The Cake task to run (default: Default)
    -Configuration <cfg>  Build configuration (default: Release)
    -Help                 Show this help message

Available Targets:
    Default              Run default task (VersionBump)
    VersionBump          Increment minor version
    BuildAndroid         Build Android APK
    BuildIos             Build iOS IPA (macOS only)
    BuildWasm            Build WebAssembly
    DeployAndroid        Build and deploy to Play Store internal
    DeployIos            Build and deploy to TestFlight (macOS only)
    DeployWasm           Build and deploy to Azure Static Web Apps
    DeployAll            Deploy to all platforms
    ComplianceCheck      Check store agreements

Examples:
    ./build.ps1                           # Run default task
    ./build.ps1 -Target VersionBump       # Bump version only
    ./build.ps1 -Target DeployAndroid     # Build and deploy Android
    ./build.ps1 -Target DeployWasm        # Build and deploy WASM
    ./build.ps1 -Target DeployAll         # Deploy to all platforms
"@
    exit 0
}

Write-Host "=== Heimatplatz Build Script ===" -ForegroundColor Cyan
Write-Host "Target: $Target"
Write-Host "Configuration: $Configuration"
Write-Host ""

# Restore and run
Push-Location $PSScriptRoot
try {
    Write-Host "Restoring dependencies..." -ForegroundColor Yellow
    dotnet restore Build.csproj

    Write-Host "Running Cake task: $Target" -ForegroundColor Yellow
    dotnet run --project Build.csproj --no-restore -- --target $Target --configuration $Configuration

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "Build completed successfully!" -ForegroundColor Green
}
finally {
    Pop-Location
}
