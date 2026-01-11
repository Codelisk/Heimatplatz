# Uno Platform Desktop App Starter
# This script starts the Uno app with Hot Reload enabled in the background
# Used by the CLI automation tool for visual validation

$env:DOTNET_MODIFIABLE_ASSEMBLIES = "debug"

# Find the .App.csproj in src folder
$appProject = Get-ChildItem -Path "$PSScriptRoot\src" -Filter "*.App.csproj" -Recurse | Select-Object -First 1

if (-not $appProject) {
    Write-Error "No .App.csproj found in $PSScriptRoot\src"
    exit 1
}

Write-Host "Starting: $($appProject.FullName)" -ForegroundColor Cyan

# Start as background job for automation, or foreground for manual use
if ($args -contains "-Background") {
    Start-Job -ScriptBlock {
        param($projectPath)
        $env:DOTNET_MODIFIABLE_ASSEMBLIES = "debug"
        dotnet run -f net10.0-desktop --project $projectPath
    } -ArgumentList $appProject.FullName
} else {
    # Foreground with watch for manual development
    dotnet watch --project $appProject.FullName -f net10.0-desktop
}
