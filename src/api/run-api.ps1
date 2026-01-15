# API Build and Run Script
# Baut und startet das API-Projekt

param(
    [switch]$Release,
    [switch]$NoBuild,
    [switch]$Watch
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "src\Heimatplatz.Api\Heimatplatz.Api.csproj"
$configuration = if ($Release) { "Release" } else { "Debug" }

Write-Host "=== Heimatplatz API ===" -ForegroundColor Cyan
Write-Host "Konfiguration: $configuration" -ForegroundColor Yellow

if (-not $NoBuild) {
    Write-Host "`nBaue Projekt..." -ForegroundColor Green
    dotnet build $projectPath -c $configuration

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build fehlgeschlagen!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Build erfolgreich!" -ForegroundColor Green
}

Write-Host "`nStarte API..." -ForegroundColor Green

if ($Watch) {
    dotnet watch run --project $projectPath -c $configuration
} else {
    dotnet run --project $projectPath -c $configuration
}
