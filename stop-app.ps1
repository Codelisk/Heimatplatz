# Finde und beende PowerShell-Prozesse, die start-app2.ps1 ausführen
Get-Process powershell | Where-Object {
    $_.CommandLine -like '*start-app2.ps1*'
} | Stop-Process -Force

# Finde und beende dotnet-Prozesse aus dem Heimatplatz.App Verzeichnis
Get-WmiObject Win32_Process | Where-Object {
    $_.Name -eq 'dotnet.exe' -and
    $_.CommandLine -like '*Heimatplatz.App*'
} | ForEach-Object {
    Write-Host "Beende Prozess $($_.ProcessId): $($_.CommandLine)"
    Stop-Process -Id $_.ProcessId -Force
}

# Finde und beende alle Heimatplatz.App.exe Prozesse
Get-Process | Where-Object {
    $_.ProcessName -like '*Heimatplatz*'
} | Stop-Process -Force

Write-Host "Alle Heimatplatz App-Prozesse wurden beendet."
