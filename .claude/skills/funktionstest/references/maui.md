# Funktionstest — .NET MAUI (`src/maui`)

App-ID `at.heimatplatz.app`, TFMs `net10.0-android`, `-ios`, `-maccatalyst`, `-windows10.0.19041.0`.

## Plattform waehlen

| Ziel | Wann | Wie |
|---|---|---|
| **Physisches Geraet** (erste Wahl, wenn angesteckt) | echter Funktionstest; ausserdem Push, Kamera, echte Performance | `adb devices -l` — Daniels S24 Ultra ist `R5CX33VK3YV` (1080×2340 @ dpi 450 = 384×832 dp); `adb reverse tcp:19223 tcp:19223` fuer den DevFlow-Broker |
| **Android-Emulator** | wenn kein Geraet haengt — Touch, Gesten, Zurueck-Taste, Berechtigungen, Offline | siehe unten |
| **Windows/WinUI** | schneller Zwischencheck einzelner Seiten | `src/maui/.claude/skills/verify/SKILL.md` — fertiges Build-/DevFlow-Rezept |

**Geraete-Vorsicht:** Das angesteckte Geraet ist Daniels privates Telefon und kann waehrend
des Tests **parallel bedient** werden (von ihm selbst oder einer anderen Session). Deshalb:
UI-Dump immer unmittelbar vor dem Tap im selben Schritt, vorher pruefen, dass die erwartete
Seite vorne ist, bei unerklaerlichem Ansichtswechsel abbrechen statt weiterklicken — und
niemals auf Koordinaten aus einem aelteren Dump tippen (ein Fehl-Tap landete so schon in
einer fremden App). Schreibende Aktionen serverseitig gegenpruefen; Offline-Tests
(`svc wifi/data disable`) danach SOFORT wieder aktivieren.

**WinUI ist kein Ersatz fuer einen Funktionstest** — dort gibt es eigene Layout- und
Input-Eigenheiten (CarouselView-Verzerrung, Overlay-Clipping, Tap-Probleme), die auf Android
nicht existieren und umgekehrt. Befunde immer mit der Plattform kennzeichnen.

## Vor dem Start — Falschbefunde ausschliessen

**1. Welcher Build laeuft ueberhaupt?**

```
git worktree list
```

Am 26.7. zeigte die App `349 000 €` statt `€ 289.000` — der Fehler steckte in einem fremden
Worktree, `master` war korrekt. Laeuft schon ein Emulator, der einer anderen Session gehoeren
koennte: **eigenen Emulator auf eigenem Port** starten (`emulator -avd <name> -port 5556`;
dieselbe AVD kann nicht zweimal laufen). Bei mehreren Geraeten immer `adb -s <serial>`.

Emulator-Start-Fallen: `emulator.exe ... | Select-Object -First N` **killt den Emulator**
(Pipeline schliesst) — nur per `Start-Process` mit `-RedirectStandardOutput` in eine Logdatei
starten. Eine AVD kann von einem haengenden qemu-Prozess gesperrt sein (dann scheitert auch
`-read-only`); gleichwertiger Ausweg: eigenen frischen Build per `adb uninstall` + install
auf einen bereits laufenden Emulator bringen — das sichert die Code-Herkunft genauso.

**2. Laeuft gerade die Release-/Screenshot-Pipeline?**
`AndroidScreenshots` (release-android.ps1) kapert einen laufenden Emulator, deinstalliert den
Debug-Build und installiert Release. Erkennungszeichen: Uhr `09:41` in der Statusleiste,
kein Debug-Eintrag im Flyout.

## Build & Deploy (Android)

```bash
cd src/maui/src/Heimatplatz.Maui
dotnet build -f net10.0-android -c Debug -t:Run -p:EmbedAssembliesIntoApk=true
```

- **`-p:EmbedAssembliesIntoApk=true` ist Pflicht**, sonst `XA0126` bzw. beim Start
  `No assemblies found in .__override__` (Fast Deployment).
- **`adb shell pm clear` niemals** auf einem Fast-Deploy-Build — die App crasht danach beim
  Start. Sauber zuruecksetzen: `adb uninstall at.heimatplatz.app` + neuer `-t:Run`.
- `-t:Run` ueberspringt manchmal die Kompilierung trotz geaenderter Quellen. Wenn eine Aenderung
  nicht wirkt: `-t:Rebuild` und DLL-Timestamp pruefen.
- Emulator-Pfad: `$ANDROID_HOME/emulator/emulator.exe` unter
  `C:\Program Files (x86)\Android\android-sdk` (**nicht** `$LOCALAPPDATA/Android/Sdk`).

## API-Endpunkt setzen — die haeufigste Falle

Der **Android-Debug-Build zeigt per Default auf die lokale Dev-API** `http://10.0.2.2:5292`
(`ApiEndpoints.GetDefaultEndpoint`). Ohne laufende lokale API wirkt die ganze App "offline" und
jeder Empty-State sieht aus wie ein Bug.

Vor dem Testen auf **Test** umstellen — einer der Wege:

```bash
# per System-Property (wird beim App-Start gelesen), danach App neu starten
adb shell setprop debug.heimatplatz.apiurl https://test-api.heimatplatz.at
```

oder in der App: **Flyout → Debug → Endpunkt „Test"** (persistiert in Preferences
`debug_api_endpoint`, wirkt sofort). Auf Windows schlaegt `HEIMATPLATZ_API_URL` als Env die
Preference.

Gewaehlten Endpunkt zu Beginn des Berichts festhalten.

## Netzwerk am Emulator

Frischer Emulator meldet "Keine Internetverbindung", obwohl DNS aufloest — Androids
Captive-Portal-Probe schlaegt fehl, das Netz gilt als unvalidiert, die Offline-Middleware greift:

```bash
adb shell settings put global captive_portal_detection_enabled 0
adb shell settings put global captive_portal_mode 0
adb shell svc wifi disable; adb shell svc wifi enable
```

`ping` taugt **nicht** als Test (ICMP ist am Emulator immer geblockt) — stattdessen
`adb shell dumpsys connectivity | grep VALIDATED`.

**Offline gezielt simulieren:** `adb shell svc wifi disable && adb shell svc data disable`
(enable analog). Offline ist ein Pflichtteil des Tests: jede Sammlungs-Seite muss einen
Empty-/Fehlerzustand mit Wiederholen zeigen, nicht endlos laden und keinen Dialog werfen.

## Bedienen und beobachten

**Ground Truth zaehlt.** DevFlow-Screenshots sind auf Android teils stale oder gemischt:

```bash
MSYS_NO_PATHCONV=1 adb -s <serial> exec-out screencap -p > shot.png   # echter Frame
adb shell uiautomator dump /sdcard/ui.xml                              # echter UI-Baum
adb shell dumpsys window | grep mCurrentFocus                          # wer hat den Fokus?
```

- **Native Dialoge (DisplayAlert, ActionSheet, System-Permission) sind in DevFlow-Screenshots
  unsichtbar und fressen alle Taps.** Wenn Taps "nicht wirken": zuerst `mCurrentFocus` pruefen,
  dann per `uiautomator dump` die Bounds holen und `adb shell input tap <x> <y>`.
- **Taps:** Buttons gehen mit `maui devflow ui tap <id>`; TapGestureRecognizer (Filter-Header,
  Vorschlagslisten, Karten) zuverlaessig per `adb shell input tap`. Flyout-Zeilen haben
  AutomationIds `Flyout_<Route>` (Startseite = `Flyout_MainPage`).
- **Koordinaten umrechnen:** Geraeteaufloesung (`adb shell wm size`) vs. logische Screenshot-Breite
  — Faktor selbst bestimmen (S24 Ultra: 1080×2340 physisch, 384dp logisch → 2.8125).
- **`ui set-property` niemals auf gebundene Properties** — der lokale Wert zerstoert das Binding,
  die Instanz verhaelt sich danach kaputt und erzeugt Phantom-Befunde. Nach so einem Eingriff App
  neu starten.
- **DevFlow-Invoke beweist nie, dass ein echter Klick ankommt.** Was der Nutzer tun wuerde, auch
  so ausloesen.
- Der **Android-Agent hat keine REST-Actions-API** — die `[DevFlowAction]`s
  (`navigate-property-detail`, `navigate-foreclosure-detail`, `navigate-edit-property`,
  `mock-home-properties`, `clear-home-property-mock`) sind dort nicht abrufbar. Parametrisierte
  Navigation stattdessen per Deep Link:
  `adb shell am start -a android.intent.action.VIEW -d "heimatplatz://property/<guid>"`.
- Agent-Port wechselt bei jedem App-Start: `adb logcat -d | grep "Agent started on port"`,
  danach `adb forward tcp:<port> tcp:<port>` und `maui devflow … -ap <port>`. Stale
  Registrierungen fangen sonst Kommandos ab (`maui devflow list`, im Zweifel
  `adb -s <serial> forward --remove-all`).
- Laufendes Log mitschneiden: `adb logcat -c` vor dem Test, danach
  `adb logcat -d > logcat.txt` — Exceptions und Netzwerkfehler gehoeren in den Bericht.

## Testbenutzer

Passwort `Test123!` fuer alle Seed-User (Test-DB **und** lokale Dev-DB, gleiche GUIDs):

`test.buyer@` · `test.seller@` · `test.broker@` · `test.verwaltung@` ·
`max.mustermann@` (Kaeufer+Verkaeufer, kuratierte Favoriten) — alle `@heimatplatz.dev`.
Admin: `admin@heimatplatz.dev` / `Admin123!`.

Mindestens **einmal als Kaeufer und einmal als Verkaeufer** durchgehen — Sammlungs-Seiten haben
eigene Zustaende fuer "nicht angemeldet" und "kein Verkaeufer".

## Seiten als Ausgangsliste

Vollstaendig aus `Features/*/Presentation/*Page.xaml` erheben (Stand Juli 2026):

Home · PropertyMap · PropertyDetail · ForeclosureDetail · Favorites · Blocked ·
MyProperties · PropertyWizard (Anlegen **und** Bearbeiten) · FilterSettings ·
NotificationSettings · UserProfile · Login · Register · ForgotPassword ·
FeedbackList · FeedbackNewMessage · FeedbackThread · Imprint · PrivacyPolicy · Debug

Zu jeder Seite das ViewModel oeffnen und **jedes `[RelayCommand]` als eigenen Testpunkt**
aufnehmen — Befehle ohne sichtbaren Button (Wischen, Long-Press, Pull-to-Refresh) werden sonst
uebersehen.

## Pflicht-Durchgaenge

- **Start:** Kaltstart aus dem Launcher, Warmstart aus dem Hintergrund, Zurueck-Taste
  auf jeder Ebene (auch auf der Startseite — App darf nicht unerwartet beenden)
- **Kein Querformat.** Nur Hochformat testen — Landscape ist ausdruecklich kein Testfall.
- **Theme:** Umschalter im Profil-Hero, dazu System-Dark-Mode im Hintergrund umstellen
  (Statusleiste und native Control-Tints mitpruefen)
- **Karte:** Marker, Pille, Wechsel Karte/Liste, Detail-Aufruf aus der Karte; die Karte laeuft
  ueber die Web-Route `/karte-embed` als WebView — Befunde dort betreffen ggf. das Web
- **Bilder:** Galerie-Swipe in beide Richtungen mehrfach (bekannter Wackelkandidat), Lightbox
  oeffnen/schliessen, Zurueck-Taste im Viewer, Zaehler
- **Detailfelder je Typ:** Haus / Grundstueck / Zwangsversteigerung — `usertests/PropertyDetailPage_Testfaelle.md`
  ist die Soll-Vorgabe; bei ZV zusaetzlich "Gericht"
- **Inserieren:** Foto aus der Galerie (mehrere), Diktat-Feld, KI-Beschreibung, Veroeffentlichen,
  danach in MyProperties, in der Suche **und** im Web gegenpruefen
- **Favoriten/Blockiert:** Umschalten im Detail muss die Liste sofort nachziehen (LocalFirst-Cache)
- **Feedback:** neue Anfrage mit Bild und Sprachnachricht, Thread lesen, Umbenennen
- **Push & Deep-Link:** Benachrichtigung im Vordergrund und aus dem Hintergrund antippen — landet
  man auf dem richtigen Objekt?
- **Offline:** Flugmodus mitten in einer Liste, waehrend eines Speicherns, beim App-Start
- **Sync-Konsistenz:** eine Aenderung im Web machen und in der App wiederfinden (Delta-Sync)

## Serverseitig gegenpruefen

Wenn unklar ist, ob die App oder die API falsch liegt:

```bash
TOKEN=$(curl -s -X POST https://test-api.heimatplatz.at/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"Email":"test.seller@heimatplatz.dev","Password":"Test123!"}' \
  | python -c "import json,sys;print(json.load(sys.stdin)['AccessToken'])")

curl -s "https://test-api.heimatplatz.at/api/properties/user?page=0&pageSize=20" -H "Authorization: Bearer $TOKEN"
curl -s "https://test-api.heimatplatz.at/api/property-drafts/" -H "Authorization: Bearer $TOKEN"
curl -s "https://test-api.heimatplatz.at/api/properties/{id}"
```

Im Befund dann festhalten, auf welcher Seite der Fehler liegt.
