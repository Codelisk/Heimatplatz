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
└── Core/DeepLink/              # heimatplatz://property|foreclosure/{guid}
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

Die `RefreshAfterSeconds` der Immobilien-Requests (900 s) sind dadurch nur noch Sicherheitsnetz.

## Wiederverwendet aus der Uno-App

- `Heimatplatz.Features.Notifications.Contracts` und `Heimatplatz.Features.AppUpdate.Contracts` (plain net10.0) als Projektreferenzen.
- Fachlogik (AuthService, TokenRefreshMiddleware, Filter-Services) 1:1 portiert; Storage auf Shiny Stores umgestellt.

## Konventionen

- ViewModels: `ObservableObject`, **nur Partial Properties** (`[ObservableProperty] public partial string X { get; set; }` — MVVMTK0045 auf Windows), `[RelayCommand]`, `[ShellMap<TPage>("Route")]`.
- Services: `[Singleton]`-Attribut, Registrierung automatisch via `AddGeneratedServices()`.
- API-Aufrufe ausschließlich via `IMediator.Request(new XxxHttpRequest())`.
- `NoWarn SHINY002`: Bug im Shiny.Maui.Shell 6.3.1 Generator (Warnung feuert fälschlich bei deaktivierten AI-Extensions).

## Build

```
dotnet build src/maui/src/Heimatplatz.Maui/Heimatplatz.Maui.csproj -f net10.0-windows10.0.19041.0
dotnet build src/maui/src/Heimatplatz.Maui/Heimatplatz.Maui.csproj -f net10.0-android
```
