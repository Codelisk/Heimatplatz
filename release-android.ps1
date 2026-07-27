#!/usr/bin/env pwsh
# Kompletter Android-Play-Store-Release in einem Lauf.
#
# Ablauf (Cake-Task "ReleaseAndroid", Details: docs/android-release.md):
#   1. Version-Bump (hoechster Play-Version-Code + 1)
#   2. Claude CLI prueft/aktualisiert Store-Texte (de-DE/en-US) + Release-Notes
#   3. Deterministische Emulator-Screenshots (Test-API, Auto-Login, Demo-Statusbar)
#   4. AAB-Build (signiert)
#   5. Upload: Listing, Bilder, Screenshots, AAB, Track-Release (Play Developer API, ohne fastlane)
#   6. Git-Commit + Tag android-v<code> (kein Push)
#
# Voraussetzungen: Android-Emulator (AVD siehe cake/appsettings.json), Claude CLI,
# cake/secrets/play-store-key.json, Keystore-Passwoerter in cake/appsettings.Local.json.
#
# Nur Metadaten/Screenshots ohne neues Binary hochladen:
#   ./release-android.ps1 -MetadataOnly
#
# Track waehlen (Default: production aus cake/appsettings.json):
#   ./release-android.ps1 -Track internal
#
# Der Track bestimmt den Auslieferungskanal des Binaries (docs/app-channels.md):
# alles ausser "production" baut mit Entwicklerwerkzeugen (Flyout "Debug" mit
# API-Umschalter Test/Produktion). Ein so gebautes Bundle darf NICHT ueber die
# Play-Konsole nach production promotet werden - fuer Production neu bauen.

param(
    [switch]$MetadataOnly,
    [ValidateSet("production", "internal", "alpha", "beta")]
    [string]$Track
)

$ErrorActionPreference = "Stop"
$target = $MetadataOnly ? "UpdateMetadataAndroid" : "ReleaseAndroid"

if ($Track) {
    # Ueberschreibt Android:Release:Track aus appsettings.json (BuildContext laedt Env-Vars)
    $env:Android__Release__Track = $Track
}

& "$PSScriptRoot/cake/build.ps1" -Target $target
exit $LASTEXITCODE
