#!/usr/bin/env pwsh
# AIRoutine FullStack Project Initialization Script
# This script initializes git and adds required submodules

$ErrorActionPreference = "Stop"

Write-Host "Initializing AIRoutine FullStack project..." -ForegroundColor Cyan

# Check if git is installed
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host "Error: git is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

# Check if already a git repository
if (Test-Path ".git") {
    Write-Host "Git repository already exists" -ForegroundColor Yellow
} else {
    Write-Host "Initializing git repository..." -ForegroundColor Green
    git init
}

# Add submodules
$submodules = @(
    @{ Path = "subm/uno"; Url = "https://github.com/AIRoutine/UnoFramework.git" },
    @{ Path = "subm/cli"; Url = "https://github.com/AIRoutine/Automation.Cli.git" }
)

foreach ($submodule in $submodules) {
    if (Test-Path $submodule.Path) {
        Write-Host "Submodule '$($submodule.Path)' already exists" -ForegroundColor Yellow
    } else {
        Write-Host "Adding submodule: $($submodule.Path)..." -ForegroundColor Green
        git submodule add $submodule.Url $submodule.Path
    }
}

Write-Host ""
Write-Host "Project initialized successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Start 'claude' to setup Claude Code plugins"
Write-Host "  2. Build the solution with 'dotnet build'"
Write-Host ""
