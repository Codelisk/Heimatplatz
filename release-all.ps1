#!/usr/bin/env pwsh
# Kompletter Release-Lauf in EINEM Befehl:
#
#   ./release-all.ps1
#
# Ablauf (iOS-Build laeuft parallel auf Codemagic, Rest lokal/CI):
#   0. Preflight: Working Tree muss clean und gepusht sein (Codemagic + CI bauen aus master!)
#   1. iOS-Build auf Codemagic starten (ios-testflight, laeuft im Hintergrund)
#   2. API deployen (Prod + Test-API, Cake DeployApi mit Health-Gates)
#   3. Play-Store-Release (ReleaseAndroid, Track production) + Push des Release-Commits
#      - bewusst FRUEH: der Version-Bump beider Stores soll von derselben Baseline
#        rechnen, bevor der iOS-Build seinen neuen Build nach TestFlight laedt;
#        die Emulator-Screenshots laufen ausserdem gegen die frisch deployte Test-API
#   4. Test-Web deployen (GitHub-Workflow DeployAstroTest, rsync gibt es nur in CI)
#   5. Prod-Web deployen (GitHub-Workflow DeployAstro)
#   6. Auf iOS-Build warten, dann SubmitIos (wartet auf Apple-Processing, reicht zur Review ein)
#
# Optionen zum Ueberspringen einzelner Etappen:
#   ./release-all.ps1 -SkipApi -SkipWeb -SkipAndroid -SkipIos

param(
    [switch]$SkipApi,
    [switch]$SkipWeb,
    [switch]$SkipAndroid,
    [switch]$SkipIos
)

$ErrorActionPreference = "Stop"
$repo = $PSScriptRoot

function Step([string]$Message) {
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
}

# --- 0. Preflight ---------------------------------------------------------
Step "Preflight: Git-Status"
$dirty = git -C $repo status --porcelain
if ($dirty) {
    Write-Host $dirty
    throw "Working Tree ist nicht clean - erst committen/stashen. Codemagic (iOS) und die Web-Workflows bauen aus master; ein Release aus einem dreckigen Tree waere inkonsistent."
}
git -C $repo push origin master
if ($LASTEXITCODE -ne 0) { throw "git push fehlgeschlagen." }

$codemagicToken = $null
$codemagicBuildId = $null
if (-not $SkipIos) {
    $envFile = Join-Path $repo ".env"
    if (Test-Path $envFile) {
        $line = (Get-Content $envFile | Where-Object { $_ -match "^CODEMAGIC_API_TOKEN=" } | Select-Object -First 1)
        if ($line) { $codemagicToken = $line.Split("=", 2)[1].Trim() }
    }
    if (-not $codemagicToken) { throw "CODEMAGIC_API_TOKEN nicht in .env gefunden (fuer den iOS-Build noetig)." }
}

# Play-Baseline VOR den Uploads: daraus ergibt sich die erwartete neue Build-Nummer
# (VersionBump = hoechster Store-Code + 1) fuer das SubmitIos-Warten.
$expectedBuild = $null
if (-not $SkipIos) {
    Step "Preflight: Store-Baseline ermitteln"
    $baselineOutput = & "$repo/cake/build.ps1" -Target PlayStoreVersionCheck 2>&1 | Out-String
    if ($baselineOutput -match "Next release would be versionCode (\d+)") {
        $expectedBuild = [int]$Matches[1]
        Write-Host "Erwartete neue Versionsnummer: $expectedBuild"
    }
    else {
        Write-Warning "Baseline nicht ermittelbar - SubmitIos wartet dann ohne Mindest-Build-Nummer."
    }
}

# --- 1. iOS-Build parallel starten ---------------------------------------
if (-not $SkipIos) {
    Step "iOS-Build auf Codemagic starten (laeuft parallel weiter)"
    $body = @{ appId = "6a4fa7762451d1f76ba798cd"; workflowId = "ios-testflight"; branch = "master" } | ConvertTo-Json
    $response = Invoke-RestMethod -Method Post -Uri "https://api.codemagic.io/builds" `
        -Headers @{ "x-auth-token" = $codemagicToken } -ContentType "application/json" -Body $body
    $codemagicBuildId = $response.buildId
    Write-Host "Codemagic-Build gestartet: $codemagicBuildId"
}

# --- 2. API (Prod + Test) --------------------------------------------------
if (-not $SkipApi) {
    Step "API deployen (Prod + Test, mit Health-Gates)"
    & "$repo/cake/build.ps1" -Target DeployApi
    if ($LASTEXITCODE -ne 0) { throw "DeployApi fehlgeschlagen." }
}

# --- 3. Play Store (frueh: gleiche Versions-Baseline wie der laufende iOS-Build) ---
if (-not $SkipAndroid) {
    Step "Play-Store-Release (Track production)"
    & "$repo/release-android.ps1"
    if ($LASTEXITCODE -ne 0) { throw "ReleaseAndroid fehlgeschlagen." }

    Write-Host "Pushe Release-Commit + Tag..."
    git -C $repo push --follow-tags origin master
    if ($LASTEXITCODE -ne 0) { throw "Push des Release-Commits fehlgeschlagen." }
}

# --- 4./5. Web (Test, dann Prod) ueber GitHub Actions ----------------------
if (-not $SkipWeb) {
    foreach ($target in @("DeployAstroTest", "DeployAstro")) {
        Step "Web deployen: $target (GitHub-Workflow)"
        gh workflow run deploy-apps.yml -f target=$target
        if ($LASTEXITCODE -ne 0) { throw "Workflow-Trigger $target fehlgeschlagen." }
        Start-Sleep 10
        $runId = gh run list --workflow=deploy-apps.yml --limit 1 --json databaseId --jq ".[0].databaseId"
        Write-Host "Warte auf Workflow-Run $runId..."
        gh run watch $runId --exit-status
        if ($LASTEXITCODE -ne 0) { throw "$target-Workflow fehlgeschlagen (Run $runId)." }
    }
}

# --- 6. iOS: auf Build warten + einreichen ---------------------------------
if (-not $SkipIos) {
    Step "Auf Codemagic-iOS-Build warten"
    while ($true) {
        $build = (Invoke-RestMethod -Uri "https://api.codemagic.io/builds/$codemagicBuildId" `
            -Headers @{ "x-auth-token" = $codemagicToken }).build
        Write-Host "  Codemagic: $($build.status)"
        if ($build.status -in @("finished", "failed", "canceled", "timeout", "skipped")) { break }
        Start-Sleep 60
    }
    if ($build.status -ne "finished") {
        throw "Codemagic-Build endete mit Status '$($build.status)' - siehe https://codemagic.io/app/6a4fa7762451d1f76ba798cd/build/$codemagicBuildId"
    }

    Step "iOS zur App-Store-Review einreichen (SubmitIos)"
    if ($expectedBuild) { $env:SUBMIT_MIN_BUILD = "$expectedBuild" }
    try {
        & "$repo/cake/build.ps1" -Target SubmitIos
        if ($LASTEXITCODE -ne 0) { throw "SubmitIos fehlgeschlagen." }
    }
    finally {
        Remove-Item Env:SUBMIT_MIN_BUILD -ErrorAction SilentlyContinue
    }
}

Step "Release-Lauf abgeschlossen"
Write-Host "API + Web deployed, Play-Store-Release eingereicht, iOS zur Review eingereicht." -ForegroundColor Green
