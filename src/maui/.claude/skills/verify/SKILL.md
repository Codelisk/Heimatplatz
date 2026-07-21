---
name: verify
description: MAUI-App (Windows) bauen, gegen Test-API starten und per DevFlow durchklicken, um Aenderungen end-to-end zu verifizieren
---

# Heimatplatz MAUI verifizieren (Windows + DevFlow)

## Build & Start

```bash
cd src/maui/src/Heimatplatz.Maui
dotnet build -f net10.0-windows10.0.19041.0 -c Debug          # Android-Check: -f net10.0-android
# Laeuft noch eine alte Instanz, schlaegt der Copy-Step fehl: taskkill //IM Heimatplatz.Maui.exe //F
cd bin/Debug/net10.0-windows10.0.19041.0/win-x64
HEIMATPLATZ_API_URL="https://test-api.heimatplatz.at" ./Heimatplatz.Maui.exe &   # IMMER Test-API bei Schreib-Flows!
maui devflow agent status    # Agent-Check (wait kann haengen, status reicht)
```

Login-Zustand persistiert zwischen Starts (Shiny Stores). Seed-User: `test.seller@heimatplatz.dev` / `max.mustermann@heimatplatz.dev` (Käufer+Verkäufer, Screenshot-User "Max Mustermann"), Passwort `Test123!`.

## DevFlow-Gotchas (Windows)

- **Navigation:** `maui devflow ui navigate "///MyProperties"` — DREI Slashes fuer Shell-Root-Routen; `".."` = zurueck. Gepushte Routen (z.B. `PropertyWizard`) ohne Slashes.
- **Fill/Tap sind positional:** `maui devflow ui fill Wizard_TitleEntry "neuer Text"`. Die Variante `--automationId X "text"` scheitert STILL (kein Output, nichts passiert) — Ergebnis immer per `ui query ... | grep '"text"'` gegenpruefen.
- **Keine Koordinaten-Taps** — nur Element-IDs/AutomationIds. `--x/--y` werden still ignoriert.
- **PropertyCard antippen:** Karte selbst/innere Buttons klappen nicht; den ersten `Border`-Child der `PropertyCard` tappen (traegt den TapGestureRecognizer). IDs via `ui tree --format compact` + Python-Filter.
- Element-IDs sind pro App-Lauf neu — nach Neustart neu quer(y)en.
- `maui devflow extensions list` ist leer — die `[DevFlowAction]`s aus DevFlowActions.cs werden nicht gefunden (Stand 18.7.2026), UI-Weg nutzen.
- Screenshot: `maui devflow ui screenshot --output x.png`. Einmaliger App-Crash nach `ui scroll` beobachtet (nicht reproduzierbar, WinUI-Flake).
- Shell-Zurueck-Pfeil (Titelleiste) ist NICHT im Visual Tree — Back-Prompts lassen sich headless nicht ausloesen.

## Server-seitig gegenpruefen

```bash
TOKEN=$(curl -s -X POST https://test-api.heimatplatz.at/api/auth/login -H "Content-Type: application/json" \
  -d '{"Email":"test.seller@heimatplatz.dev","Password":"Test123!"}' | python -c "import json,sys;print(json.load(sys.stdin)['AccessToken'])")
curl -s "https://test-api.heimatplatz.at/api/properties/user?page=0&pageSize=20" -H "Authorization: Bearer $TOKEN"   # eigene Inserate
curl -s "https://test-api.heimatplatz.at/api/property-drafts/" -H "Authorization: Bearer $TOKEN"                     # Entwuerfe
curl -s "https://test-api.heimatplatz.at/api/properties/{id}"                                                        # oeffentlich
```

`/api/properties?pageSize=100` liefert 0 Items (Server-Cap) — pageSize<=20 verwenden.

## Logs

`maui devflow logs --limit 80` liefert JSON-Eintraege inkl. aller HTTP-Requests — ideal um zu pruefen, ob PUT/GET wirklich rausgingen (Cache-Bypass sichtbar als echter GET direkt nach Aktion).
