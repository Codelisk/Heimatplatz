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
    BuildAstro           Build Astro web (src/web -> dist)
    DeployAndroid        Build and deploy to Play Store internal (fastlane, legacy)
    DeployIos            Build and deploy to TestFlight (macOS only)
    DeployAstro          Build and deploy Astro web (Prod) to Hetzner
    DeployAstroTest      Build and deploy Astro web (Test, test.heimatplatz.at) to Hetzner
    DeployAll            Deploy to all platforms (Android, iOS, Astro web Prod)
    ComplianceCheck      Check store agreements
    ReleaseAndroid       Full Play Store release: bump + Claude texts + screenshots + AAB + upload (see release-android.ps1)
    AndroidStoreTexts    Claude CLI: review/update Play texts (de/en) + release notes
    AndroidScreenshots   Deterministic Play Store screenshots via Android emulator
    UpdateMetadataAndroid Upload Play listing texts/images/screenshots only (no binary)
    PlayStoreVersionCheck Show highest Play version code (connectivity check)

Examples:
    ./build.ps1                           # Run default task
    ./build.ps1 -Target VersionBump       # Bump version only
    ./build.ps1 -Target DeployAndroid     # Build and deploy Android
    ./build.ps1 -Target DeployAstro       # Build and deploy Astro web
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
