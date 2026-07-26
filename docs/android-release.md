# Android Play-Store-Release

Der komplette Release läuft mit einem Befehl:

```powershell
./release-android.ps1                 # kompletter Release (Bump + Texte + Screenshots + AAB + Upload)
./release-android.ps1 -MetadataOnly   # nur Store-Eintrag aktualisieren (Texte/Bilder/Screenshots, kein Binary)
```

Äquivalent über Cake: `cake/build.ps1 -Target ReleaseAndroid` bzw. `-Target UpdateMetadataAndroid`.

## Ablauf (Task `ReleaseAndroid`)

1. **VersionBump** – höchster Version-Code aus dem Play Store (alle Tracks) + 1 → `Heimatplatz.Maui.csproj`.
2. **AndroidStoreTexts** – Claude CLI (Default-Modell, headless) prüft `title/short_description/full_description`
   in `cake/fastlane/metadata/android/{de-DE,en-US}/` gegen den aktuellen Funktionsumfang, aktualisiert bei Bedarf
   und schreibt die Release-Notes `changelogs/<versionCode>.txt` (beide Sprachen, aus dem Git-Log seit dem letzten
   `android-v*`-Tag). Danach harte Limit-Validierung in C# (30/80/4000/500 Zeichen) – bei Verstoß bricht der Lauf ab.
3. **AndroidScreenshots** – deterministische Screenshots im Emulator:
   - Release-APK (debug-signiert, `android-x64`), frische Installation
   - AVD aus `Android:Screenshots:Devices` (läuft er schon, wird er wiederverwendet), sonst
     Kaltstart mit `-no-snapshot`
   - Demo-Statusbar: 09:41, WLAN voll, Akku 100 %, keine Notifications
   - App gegen die gehostete Test-API (`https://test-api.heimatplatz.at`, geseedete Daten)
   - Auto-Login mit dem Test-User + Navigation pro Route, Aufnahme erst wenn zwei
     aufeinanderfolgende Screenshots identisch sind (Settle-Erkennung)
   - Steuerung über `adb shell setprop debug.heimatplatz.*` → `ScreenshotSysProps` (nur im Emulator aktiv)
     übersetzt sie in Env-Vars für den geteilten `ScreenshotMode`
   - Ablage direkt in `cake/fastlane/metadata/android/de-DE/images/phoneScreenshots/` (git-versioniert)
4. **BuildAndroid** – signiertes AAB nach `artifacts/android`.
5. **Play-Upload** – alles in einem Edit über die **Google Play Developer API** (nativ in C#, kein fastlane/Ruby):
   Listing-Texte (de-DE + en-US), Icon, Feature-Graphic, Screenshots (en-US fällt auf de-DE zurück,
   solange die App nicht lokalisiert ist), Kontaktdaten, AAB, Track-Release inkl. Release-Notes.
6. **Git-Commit + Tag** – `csproj` + Metadata werden committet, Tag `android-v<code>` gesetzt (kein Push;
   `git push --follow-tags` manuell). Abschaltbar über `Android:Release:GitCommit/GitTag`.

## Voraussetzungen

| Was | Wo |
|-----|----|
| Play-Service-Account-Key | `cake/secrets/play-store-key.json` |
| Keystore + Passwörter | `cake/secrets/heimatplatz.keystore`, Passwörter in `cake/appsettings.Local.json` |
| Android-Emulator (AVD) | `Android:Screenshots:Devices` in `cake/appsettings.json` (Default `pixel_5_-_api_35`) |
| Claude CLI | `claude` auf dem PATH (nutzt das Default-Modell) |

### AVD für die Screenshots

Neu anlegen (Pixel-5-Profil, 1080x2340 – passt zu den bestehenden Store-Screenshots):

```powershell
& "$env:ANDROID_HOME\cmdline-tools\latest\bin\avdmanager.bat" create avd `
    -n "pixel_5_-_api_35" -k "system-images;android-35;google_apis_playstore;x86_64" -d "pixel_5"
```

**Mindestens API 35 verwenden.** Der Karten-Shot rendert die Web-Faltkarte (`/karte-embed`, MapLibre)
im WebView; das API-30-Image bringt WebView 83 (Chrome 83, 2020) mit und zeigt dort nur eine leere
Seite. Die Statusleiste ist unter API 35 empfindlich – die Regeln stehen als Kommentar im Task:
Demo-Mode genau einmal und erst nach der Installation setzen, WLAN und Mobilfunk in einem einzigen
`network`-Befehl, Emulator immer kalt booten.

## Konfiguration (`cake/appsettings.json`)

- `Android:Screenshots` – AVDs (`ImageType`: `phoneScreenshots`/`sevenInchScreenshots`/`tenInchScreenshots`),
  Shots (Name + Shell-Route), Test-API-URL, Login, Emulator-Architektur.
- `Android:Release` – Track (`internal`/`production`), `ReleaseStatus` (`completed`/`draft`), Locales,
  Kontaktdaten, Git-Verhalten.
- `Android:StoreTexts:ClaudeModel` – leer = Default-Modell der Claude CLI.

## Diagnose

```powershell
cake/build.ps1 -Target PlayStoreVersionCheck   # Service-Account + höchster Version-Code
cake/build.ps1 -Target AndroidScreenshots      # nur Screenshots neu aufnehmen
cake/build.ps1 -Target AndroidStoreTexts       # nur Texte prüfen/aktualisieren
```

Schlägt ein Screenshot fehl, landet der relevante `logcat`-Auszug (inkl. `[ScreenshotMode]`-Zeilen
mit Login-/Navigationsstatus) im Build-Log.
