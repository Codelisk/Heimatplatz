# Heimatplatz.Maui

.NET MAUI App (urspruenglich als Migration der frueheren Uno-Platform-App entstanden, die inzwischen entfernt wurde) — Android, iOS, Mac Catalyst, Windows.

## Architektur

**Backend-First:** Alle Logik im Backend (`src/api`), die App ist reine Anzeige-/Bedienschicht.

Maximaler Shiny-Einsatz:

| Baustein | Shiny-Paket |
|---|---|
| Navigation/Pages/Dialoge | `Shiny.Maui.Shell` (`[ShellMap]`, `INavigator`, `IDialogs`, Source-Gen Routen) |
| Hosting/Lifecycle | `Shiny.Hosting.Maui` (`UseShiny()`) |
| DI | `Shiny.Extensions.DependencyInjection` (`[Singleton]` + `AddGeneratedServices()`) |
| Persistenz | `Shiny.Extensions.Stores` (`[Bind]`-Partial-Properties, Secure Store für Tokens) |
| API-Zugriff | `Shiny.Mediator` + `Shiny.Mediator.Maui`; OpenAPI-Client generiert via `MediatorHttp`-Item aus `src/api/openapi/Heimatplatz.Api.json` (Namespace `Heimatplatz.Maui.ApiClient.Generated`) |
| Push | `Shiny.Push` (FCM/APNs) + `Shiny.Notifications` (Foreground-Anzeige Android) |

## Struktur

```
Heimatplatz.Maui/
├── MauiProgram.cs              # UseShiny + UseShinyShell + Mediator + Stores + DI
├── App.xaml(.cs)               # Startup: Session-Restore + Push-Init (AppStartupService)
├── AppShell.xaml(.cs)          # ShinyShell (Flyout-Navigation)
├── Events/                     # Mediator-Events (Login/Logout)
├── Http/                       # IHttpHeaderContributor + AggregatingHttpRequestDecorator
├── Offline/                    # LocalFirst-Middleware, SQLite-Store, Offline-Konfiguration, Staleness
├── Services/                   # AppStartupService
├── Presentation/               # Shell-Ebene: Impressum, Datenschutz
├── Features/
│   ├── Auth/                   # AuthService (Shiny Stores), TokenRefresh, Login/Register/Profil
│   ├── Properties/             # Immobilien: Liste/Detail/Anlegen/Favoriten/Filter
│   ├── Notifications/          # Shiny.Push Delegate/Initializer, Einstellungen
│   └── AppUpdate/              # Google Play In-App-Update (Android)
├── Core/DeepLink/              # heimatplatz://property|foreclosure/{guid}
└── Core/Build/                 # AppChannels: Development/Internal/Production (Debug-Werkzeuge)
```

## Offline & Caching (Local-First + Delta-Sync)

Alle lesenden API-Requests aus `Offline/OfflineDataConfiguration.cs` werden persistent in SQLite
gecacht (`Shiny.DocumentDb`, `heimatplatz-offline.db` im AppDataDirectory) und offline ausgeliefert.
Pipeline: `LocalFirstRequestMiddleware` (Cache sofort, Refresh im Hintergrund) → persistenter Cache
→ Offline-Fallback → `OfflineNetworkGuardMiddleware` → HTTP.

**Delta-Sync fuer Immobilien** (`Features/Properties/Sync/`): `PropertySyncService` pollt
`GET /api/properties/changes?Since=<Watermark>` (App-Start, Resume, alle 60 s) und patcht die
lokalen Caches gezielt, statt Listen neu zu laden:

- **Updated**: Eintrag wird in allen gecachten Listen-Antworten in-place ersetzt; ein gecachtes
  Detail wird einzeln per `ForceCacheRefresh` nachgeladen.
- **Deleted**: aus Listen entfernt (inkl. `Total`), Detail-Cache verworfen.
- **Created**: Listen-Caches werden ueber die `CacheStalenessRegistry` als veraltet markiert
  (Filter-/Sortier-Einordnung ist Backend-Sache); der naechste Zugriff erneuert sie im Hintergrund.
- Danach wird `PropertyDataSyncedEvent` publiziert - Home/Favoriten/Blockierte/Meine-Immobilien/
  Detail-ViewModels patchen ihre sichtbaren Listen in-place.
- Watermark liegt pro API-Endpunkt in Preferences; `FullResyncRequired` (erster Lauf oder Stand
  aelter als die 30-Tage-Journal-Retention) verwirft alle Immobilien-Caches.
- **Watermark-Format: UTC mit `Z`, niemals `+00:00`** (`FormatWatermark`). Der generierte
  OpenAPI-Client haengt Query-Werte unkodiert an die URL - ein `+` kommt serverseitig als
  Leerzeichen an, `Since` ist dann unlesbar und der Server meldet bei jedem Sync einen
  Voll-Refresh (Pille "Neue Inserate" bei jedem App-Start). Gleiche Falle wie bei
  `CreatedAfter`, das der `AggregatingHttpRequestDecorator` nachtraeglich repariert.

Die `RefreshAfterSeconds` der Immobilien-Requests (900 s) sind dadurch nur noch Sicherheitsnetz.

## Weg von der Karte zur Detailseite

Damit die Detailseite beim Antippen sofort steht statt auf den Detail-Request zu warten
(`Features/Properties/Services/`):

- `PropertyDetailPreloader.Prepare()` laeuft beim Tap **vor** der Navigation: legt die
  Listendaten der Karte im `PropertyHandoffCache` ab und startet den Detail-Request. Die
  Detailseite holt genau diesen laufenden Request per `TryTakePendingRequest()` ab - es geht
  nie ein zweiter raus.
- Die Detail-ViewModels zeichnen aus dem Handoff sofort Kopf, Kernfakten und erstes Foto;
  die Detaildaten ersetzen den Zustand, sobald sie da sind. Das Busy-Overlay erscheint nur,
  wenn nach 250 ms noch nichts anzuzeigen ist.
- Bilder liefert der Server in drei Varianten (`ImageUrls` voll, `PreviewImageUrls` 1280px,
  `ThumbnailImageUrls` 640px = zeichengleich mit den Listen-URLs). `PropertyDetailImageResolver`
  zeigt die beste lokal vorhandene Variante und laedt die Vorschau im Hintergrund nach
  (`ImageUrls` ist eine feste `ObservableCollection` mit In-Place-Patch - kein Carousel-Rebuild).
  Die volle Aufloesung wird erst beim Oeffnen des Vollbild-Viewers geholt.
- Der Vollbild-Viewer (`Controls/PropertyImageViewerOverlay`, geteilt ueber `IImageViewerHost`)
  entsteht erst beim ersten Oeffnen. Er bekommt ausschliesslich lokale Dateipfade - eine
  entfernte URL laedt er unter WinUI nicht.
- `DetailNavigationTrace` protokolliert Tap → Seite sichtbar → Vorschau → Daten → Foto scharf.

## Wiederverwendet aus der Uno-App

- `Heimatplatz.Features.Notifications.Contracts` und `Heimatplatz.Features.AppUpdate.Contracts` (plain net10.0) als Projektreferenzen.
- Fachlogik (AuthService, TokenRefreshMiddleware, Filter-Services) 1:1 portiert; Storage auf Shiny Stores umgestellt.

## Konventionen

- ViewModels: `ObservableObject`, **nur Partial Properties** (`[ObservableProperty] public partial string X { get; set; }` — MVVMTK0045 auf Windows), `[RelayCommand]`, `[ShellMap<TPage>("Route")]`.
- Services: `[Singleton]`-Attribut, Registrierung automatisch via `AddGeneratedServices()`.
- API-Aufrufe ausschließlich via `IMediator.Request(new XxxHttpRequest())`.
- `NoWarn SHINY002`: Bug im Shiny.Maui.Shell 6.3.1 Generator (Warnung feuert fälschlich bei deaktivierten AI-Extensions).

## App-Icons

Pro Plattform ein eigenes `MauiIcon`-Item in der csproj (Resizetizer verarbeitet nur das erste
aktive Item; Duplikat-Namen ueber alle aktiven Items sind verboten, daher ueberall Conditions):

| Plattform | Background | Foreground | ForegroundScale | Grund |
|-----------|------------|------------|-----------------|-------|
| Android | `appicon.svg` (full-bleed Markenrot) | `appiconfg.svg` (volles Badge) | `0.62` | Badge bleibt in der 66/108dp-Safe-Zone des Adaptive Icons |
| Windows | `appiconwin.svg` (transparent) | `appiconfgwin.svg` (Badge ohne Textring) | `1.0` | Taskbar/Start erwarten Icons ohne Farbplatte; Textring ist bei 16-48px unlesbar |
| iOS/MacCatalyst | `appicon.svg` | `appiconfg.svg` | `0.75` | Opak (App-Store-Pflicht), Badge-Groesse nach Apple Icon Grid |

Zusaetzlich `Platforms/Windows/appicon.ico` (Multi-Size 16-256px) als `<ApplicationIcon>`:
Unpackaged-Apps (WindowsPackageType=None) nehmen das Taskbar-Icon aus dem EXE; das von MAUI
generierte ICO haette nur einen einzigen 64px-Eintrag. Generator-Skript und Quell-SVGs:
Badge = `src/web/public/favicon.svg`, vereinfachte Variante = `src/web/public/icon.svg`.

Nach Icon-Aenderungen: `obj/**/resizetizer` loeschen und App deinstallieren -
inkrementelle Builds cachen Icons (auch `ForegroundScale`-Aenderungen greifen sonst nicht).

## Auslieferungskanäle (Debug-Werkzeuge)

`Core/Build/AppChannels.cs` entscheidet, ob die App Entwicklerwerkzeuge zeigt — Flyout-Eintrag
„Debug" mit API-Umschalter (Entwicklung/Test/Produktion), Test-Anmeldungen und die Umgebungs-Pille
in der Flyout-Fußzeile.

| Kanal | Wann | Werkzeuge |
|---|---|---|
| `Development` | Debug-Build | ja |
| `Internal` | Play-Testkanäle, TestFlight, Ad-hoc-iOS | ja |
| `Production` | App Store, Play-Production-Track | nein |

- **Android** setzt den Kanal beim Build (`-p:HeimatplatzChannel=Internal`); der Release-Lauf leitet
  ihn aus dem Play-Track ab. Ein Test-Bundle darf nicht per Play-Promotion nach production wandern.
- **iOS** erkennt TestFlight zur Laufzeit (`sandboxReceipt` / `embedded.mobileprovision`), weil
  derselbe Build später zur Store-Version befördert wird — kein Build-Parameter nötig.
- Fail-closed: Release ohne Angabe ist immer `Production`, ein ungültiger Wert bricht den Build ab,
  und eine in TestFlight gespeicherte Endpunkt-Auswahl wird im Store-Build verworfen.

Details, Fallen und der Ablauf eines Umgebungswechsels: [`docs/app-channels.md`](../../../../docs/app-channels.md).

## Build

```
dotnet build src/maui/src/Heimatplatz.Maui/Heimatplatz.Maui.csproj -f net10.0-windows10.0.19041.0
dotnet build src/maui/src/Heimatplatz.Maui/Heimatplatz.Maui.csproj -f net10.0-android

# Interner Testkanal (Debug-Werkzeuge im Release-Build)
dotnet build src/maui/src/Heimatplatz.Maui/Heimatplatz.Maui.csproj -f net10.0-android -c Release -p:HeimatplatzChannel=Internal
```
