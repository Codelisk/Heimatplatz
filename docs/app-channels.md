# Auslieferungskanäle der MAUI-App (Debug-Werkzeuge im internen Test)

Interne Tester (Play-Testkanal, TestFlight) brauchen den Umschalter zwischen Test- und
Produktions-API. Endkunden dürfen ihn nie sehen. Der Kanal des laufenden Builds entscheidet das.

| Kanal | Wann | Debug-Eintrag im Flyout | API-Umschalter |
|---|---|---|---|
| `Development` | Debug-Build (Entwicklungsrechner, Emulator) | ja | Entwicklung / Test / Produktion |
| `Internal` | Play-Testkanäle (`internal`/`alpha`/`beta`), TestFlight, Ad-hoc-iOS-Builds | ja | Test / Produktion |
| `Production` | App Store, Play-Production-Track | **nein** | keiner, immer Produktions-API |

Code: [`Core/Build/AppChannels.cs`](../src/maui/src/Heimatplatz.Maui/Core/Build/AppChannels.cs).
`AppChannels.AreDeveloperToolsEnabled` ist der einzige Schalter — wer ein weiteres
Entwicklerwerkzeug einbaut, hängt es dort an und nirgends sonst.

## Warum die Erkennung pro Plattform verschieden ist

**Android — zur Build-Zeit.** Google Play liefert dieselbe AAB an jeden Track aus; ein Binary
kann seinen Track zur Laufzeit nicht erkennen. Der Kanal kommt deshalb aus der MSBuild-Property
`HeimatplatzChannel`, die die Konstante `HEIMATPLATZ_INTERNAL` setzt:

```
dotnet publish -f net10.0-android -c Release -p:HeimatplatzChannel=Internal
```

Der Release-Lauf macht das automatisch: `cake/BuildContext.cs` leitet den Kanal aus
`Android:Release:Track` ab (alles außer `production` → `Internal`),
`cake/Tasks/BuildAndroidTask.cs` gibt ihn an MSBuild weiter.

```
./release-android.ps1 -Track internal      # -> Kanal Internal, Werkzeuge an
./release-android.ps1                      # -> Track production, Kanal Production, Werkzeuge aus
```

> **Falle:** Ein Test-Bundle darf **nicht** über die Play-Konsole in den Production-Track
> promotet werden — der Kanal steckt fest im Binary. Für Production immer neu bauen.
> `ReleaseAndroidTask` warnt bei jedem Nicht-Production-Lauf genau darauf.

**iOS — zur Laufzeit.** Ein TestFlight-Build wird später unverändert zur App-Store-Version
befördert. Eine Build-Konstante würde den Umschalter also direkt zum Endkunden tragen. Apple
kennzeichnet die Auslieferung aber im Bundle, und genau das wertet `AppChannels` aus:

- TestFlight legt `sandboxReceipt` statt des regulären App-Store-Belegs ab
- Ad-hoc-/Enterprise-/Development-Builds tragen eine `embedded.mobileprovision`

Beides fehlt in einer aus dem Store geladenen App. Der iOS-Release braucht deshalb **keinen**
zusätzlichen Parameter — `codemagic.yaml` bleibt unverändert.

## Fail-closed

- Debug ist immer `Development`, Release ohne Angabe immer `Production`. Ein versehentlich ohne
  Parameter gebauter Store-Release kann die Werkzeuge nicht zeigen.
- Ein ungültiger Wert bricht den Build ab (`_HeimatplatzValidateChannel` in der csproj), statt
  still auf Produktion zurückzufallen. Der gewählte Kanal steht in jedem Build-Log:
  `Heimatplatz-Auslieferungskanal: Internal`.
- Schlägt die Apple-Abfrage fehl, gilt Produktion.
- `ApiEndpoints.GetSelectedEndpoint()` verwirft in Store-Builds eine gespeicherte Auswahl und
  löscht sie aus den Preferences. Das ist der Pfad, der auf iOS wirklich vorkommt: Ein Tester
  stellt in TestFlight auf Test und bekommt später das Store-Update über dieselbe Installation.

## Umgebungswechsel in der App

Flyout → **Debug** → Karte *API-Endpunkt*. Der Wechsel (`ApiEndpointService.SwitchEndpointAsync`):

1. schreibt die Base-URL in die `IConfiguration` — Shiny.Mediator liest sie pro Request, der
   Wechsel wirkt also sofort ohne Neustart;
2. beendet eine bestehende Anmeldung, weil Tokens nur für eine Umgebung gelten (sonst 401-Schleife,
   die wie ein Serverfehler aussieht);
3. lässt alle offenen Listen neu laden (`PropertyDataSyncedEvent` mit `FullResync: true`, derselbe
   Weg wie nach einem Voll-Refresh des Delta-Syncs).

Lokale Daten müssen dabei **nicht** gelöscht werden: Cache-/Offline-Schlüssel
(`UserScopedContractKeyProvider`) und der Delta-Sync-Wasserstand (`PropertySyncService`) enthalten
den Endpunkt. Jede Umgebung führt ihren eigenen Bestand, der beim Zurückschalten wieder da ist.

Die Flyout-Fußzeile zeigt in diesen Builds eine Pille mit Kanal und aktiver API
(z.B. „TestFlight · Test-API"), damit Fehlerberichte von Testern nicht mehr an der Frage hängen,
gegen welche Umgebung gemessen wurde.

Die Schnellanmeldungen mit Testkonten blendet die Seite gegen die Produktions-API aus — diese
Konten existieren nur in der Entwicklungs- und Test-Datenbank. Die lokale Entwicklungs-API ist nur
im Kanal `Development` wählbar; auf einem TestFlight-Gerät gibt es hinter `localhost` nichts.
