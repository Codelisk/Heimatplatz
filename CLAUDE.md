# Heimatplatz

## Agent-Kontext

Diese Datei ist die gemeinsame fachliche und architektonische Regelbasis fuer Claude und
Codex. Codex wird ueber `AGENTS.md` auf diese Regeln verwiesen.

- Claude-Skills liegen unter `.claude/skills/`.
- Codex-Skills liegen unter `.codex/skills/`.
- Inhaltlich gemeinsame Skills muessen in beiden Verzeichnissen vorhanden sein.
- Agent-spezifisches Skill-Frontmatter darf abweichen: Claude nutzt zusaetzlich
  `auto_invoke` und `triggers`; Codex bildet die Ausloeser in `description` ab.
- Web/Astro-Aufgaben nutzen `astro-ai-development`.
- Vollstaendige manuelle QA-Laeufe nutzen `funktionstest`.
- Lokalisierungsaufgaben mit `Shiny.Extensions.Localization.Generator` nutzen `localizegen`.
- MAUI-/Shiny-Aufgaben nutzen den jeweils fachlich passenden `shiny-*` Skill.

## Technische Details

- **Sprache:** C# latest
- **Framework:** .NET 10
- **Frontend Web:** Astro (`src/web`, nutze Astro-AI-Skill und Astro Docs MCP)
- **Frontend Mobile/Desktop:** .NET MAUI (`src/maui`, Shiny-First, nutze definierte Shiny-Skills)
- **Backend:** ASP.NET (nutze Microsoft Docs MCP und definierte Skills)
- **Architektur:** Shiny Mediator Pattern ([GitHub](https://github.com/shinyorg/mediator))

## Architekturprinzip: Backend-First

**Alle Logik im Backend. Frontend nur für Anzeige.**

| Backend (API) | Frontend (MAUI/Web) |
|---------------|----------------|
| Geschäftslogik, Validierung, Berechnungen | UI, Navigation, API-Aufrufe |
| Datenbank, Security, externe Services | Loading-States, UX-Feedback |

## Dependency Injection

Nutze `Shiny.Extensions.DependencyInjection` für automatische Service-Registrierung.

### Attribute

**API-Services:**
```csharp
using Heimatplatz.Api;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class MyService : IMyService { }
```

**MAUI-Services:**
```csharp
[Singleton]
public class MyService : IMyService { }
```
HttpClient-basierte Services brauchen zusaetzlich explizite Registrierung ueber `AddHttpClient<TInterface, TImpl>()` in der Feature-`ServiceCollectionExtensions.cs` (siehe `shiny-di` Skill).

### Registrierung

```csharp
services.AddShinyServiceRegistry();
```

### Konstanten

| Klasse | Namespace | Projekt | Lifetime | TryAdd |
|--------|-----------|---------|----------|--------|
| `ApiService` | `Heimatplatz.Api` | `src/api/src/Shared/Api.Shared` | `Scoped` | `true` |

MAUI nutzt keine eigene Wrapper-Konstante, sondern die Shiny-DI-Attribute (`[Singleton]`, `[Scoped]`, `[Transient]`) direkt.

Referenz: [shinylib.net/extensions/di](https://shinylib.net/extensions/di/)

## Projektstruktur

```
Heimatplatz/
├── src/
│   ├── api/                        # Backend (ASP.NET)
│   │   ├── src/
│   │   │   ├── *.Api/                             # Host
│   │   │   ├── Shared/
│   │   │   │   └── *.Api.Shared/                  # ApiService DI-Konstanten, ApiException, Policies
│   │   │   ├── Core/
│   │   │   │   ├── *.Api.Core.Data/               # DbContext, BaseEntity, Provider-Switch
│   │   │   │   ├── *.Api.Core.Data.Migrations.Postgres/  # EF-Migrations (Postgres)
│   │   │   │   ├── *.Api.Core.Data.Seeding/       # ISeeder-Infrastruktur, SeederRunner
│   │   │   │   ├── *.Api.Core.AiConnectorClient/  # Generierter AiConnector-Client
│   │   │   │   └── *.Api.Core.Startup/            # DI Setup, DB-Init
│   │   │   └── Features/{Name}/
│   │   │       ├── *.Api.Features.{Name}/         # Services, Data, Handlers
│   │   │       └── *.Api.Features.{Name}.Contracts/
│   │   └── tests/
│   │       ├── UnitTests/                         # *.Api.UnitTests, *.Api.Core.UnitTests
│   │       └── IntegrationTests/                  # *.Api.IntegrationTests, *.Api.Core.IntegrationTests
│   │
│   ├── web/                        # Web Frontend (Astro)
│   │   ├── src/
│   │   │   ├── components/         # Layout, Feature-Komponenten, Starwind UI
│   │   │   ├── config/             # Site-/SEO-Konfiguration
│   │   │   ├── features/           # fachliche Web-Slices
│   │   │   ├── layouts/            # BaseLayout mit SEO-Metadata
│   │   │   └── pages/              # Astro file-based routes
│   │   └── package.json
│   │
│   └── maui/                        # Mobile/Desktop Frontend (.NET MAUI, Shiny-First)
│       └── src/
│           ├── Heimatplatz.Maui/                  # Hauptprojekt (Single-Project, kein Feature-Split)
│           │   ├── Core/                          # DeepLink, Startup etc.
│           │   └── Features/{Name}/               # Configuration, Handlers, Presentation, Services
│           ├── Heimatplatz.Maui.ApiClient/        # Generierter OpenAPI-Client (eigenes Projekt, s.u.)
│           └── ThirdParty/                        # Gevendorter Fremdcode (MapLibre-Binding, BSD-2);
│                                                  #   eigene Directory.*.props: kein CPM, Warnungen
│                                                  #   keine Fehler - Details in den READMEs dort
```

> `*` = `Heimatplatz` Namespace-Prefix

## Projektdokumentation

**Jedes Projekt (Core, Features, ThirdParty) MUSS eine `README.md` Datei im Projektstammverzeichnis enthalten.**

Die README.md dokumentiert:
- Zweck und Verantwortlichkeiten des Projekts
- Öffentliche APIs und Services
- Abhängigkeiten zu anderen Projekten
- Konfigurationsoptionen (falls vorhanden)
- Beispiele zur Verwendung

## API Feature-Erweiterungsstruktur

Bei neuen API-Features erstelle Projekte unter `src/api/src/Features/{FeatureName}/`:
1. **Hauptprojekt** (`Api.Features.{FeatureName}`) - Services, Data, Handlers
2. **Contracts-Projekt** (`Api.Features.{FeatureName}.Contracts`) - Request/Response DTOs

**Jedes dieser Projekte MUSS eine `README.md` enthalten.**

### Namenskonventionen

| Typ | Ordner | Benennung |
|-----|--------|-----------|
| Features | `src/api/src/Features/` | `Heimatplatz.Api.Features.{FeatureName}` |
| Core Features | `src/api/src/Core/` | `Heimatplatz.Api.Core.{FeatureName}` |

### API Feature Hauptprojekt

```
src/api/src/Features/{FeatureName}/Heimatplatz.Api.Features.{FeatureName}/
├── README.md                             # Projektdokumentation (PFLICHT)
├── Configuration/
│   └── ServiceCollectionExtensions.cs    # Add{FeatureName}Feature()
├── Data/
│   ├── Entities/
│   │   └── {Entity}.cs                   # : BaseEntity
│   └── Configurations/
│       └── {Entity}Configuration.cs      # IEntityTypeConfiguration<T>
├── Handlers/
│   └── {Action}Handler.cs                # IRequestHandler<TRequest, TResponse>
└── Services/
    ├── I{Service}.cs                     # Service-Interfaces
    └── {Service}.cs                      # Service-Implementierungen
```

### API Contracts-Projekt

```
src/api/src/Features/{FeatureName}/Heimatplatz.Api.Features.{FeatureName}.Contracts/
├── README.md                             # Projektdokumentation (PFLICHT)
└── Mediator/
    └── Requests/
        ├── {Action}Request.cs            # IRequest<TResponse>
        └── {Action}Response.cs           # Response DTO (embedded record)
```

### Feature Registration

Features werden in `Core.Startup/ServiceCollectionExtensions.cs` registriert:

```csharp
public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
{
    services.AddShinyServiceRegistry();  // [Service] Attribute scannen
    services.AddShinyMediator();
    services.AddAppData(configuration);  // Zentraler DbContext
    services.Add{FeatureName}Feature();
    return services;
}
```

### Entity Configuration Pattern

Entities erben von `BaseEntity` und werden via `IEntityTypeConfiguration<T>` konfiguriert:

### Datenbank-Konfiguration

Der Provider wird per `Database:Provider` gewaehlt (Switch in
`Core.Data/Configuration/ServiceCollectionExtensions.cs`):

- **Postgres** — Produktion und Test (Hetzner, `deploy/hetzner/docker-compose.yml`,
  `Database__Provider: Postgres`). EF-Migrations liegen in der eigenen Assembly
  `Heimatplatz.Api.Core.Data.Migrations.Postgres`.
- **SQLite** — nur lokale Entwicklung; wird ohne gesetzten Provider automatisch am
  Connection-String erkannt (`appsettings.Development.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  }
}
```

Weitere Optionen: `Database:AutoMigrate` (Migrations beim Start) und
`Database:EnableSeeding` (Demo-Daten; Referenzdaten-Seeder laufen immer).

**Auto-Discovery:** Entities die von `BaseEntity` erben und `IEntityTypeConfiguration<T>` Implementierungen werden automatisch aus allen `Heimatplatz.*` Assemblies geladen - keine manuelle Registrierung noetig.

### Mock-Daten / Seeding

Seeder werden im Projekt `Core.Data.Seeding` verwaltet und beim API-Start automatisch ausgefuehrt.

**Seeder erstellen im Feature:**

```csharp
// Features/{Name}/Data/Seeding/{Name}Seeder.cs
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Microsoft.EntityFrameworkCore;

public class MyFeatureSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 10; // Reihenfolge (niedrig = zuerst)

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: Nur seeden wenn leer
        if (await dbContext.Set<MyEntity>().AnyAsync(cancellationToken))
            return;

        dbContext.Set<MyEntity>().AddRange(
            new MyEntity { Name = "Test 1" },
            new MyEntity { Name = "Test 2" }
        );

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

**Seeder registrieren:**

```csharp
// In Feature ServiceCollectionExtensions.cs
using Heimatplatz.Api.Core.Data.Seeding.Configuration;

public static IServiceCollection AddMyFeature(this IServiceCollection services)
{
    services.AddSeeder<MyFeatureSeeder>();
    return services;
}
```

**Wichtig:**
- Seeder MUSS idempotent sein (pruefen ob Daten existieren)
- Mindestens 5-10 realistische Eintraege pro Entity
- Datenbank wird NICHT geloescht, Daten wachsen kontinuierlich

## MAUI Feature-Erweiterungsstruktur

`Heimatplatz.Maui` ist ein Single-Project (kein Feature-Split in separate Projekte wie bei API). Neue Features leben als Ordner unter `src/maui/src/Heimatplatz.Maui/Features/{FeatureName}/`:

```
Features/{FeatureName}/
├── Configuration/
│   └── ServiceCollectionExtensions.cs    # nur falls explizite Registrierung noetig (z.B. HttpClient)
├── Services/
│   ├── I{Service}.cs                     # Service-Interfaces
│   └── {Service}.cs                      # [Singleton]/[Scoped]-Implementierungen
├── Handlers/                             # Shiny.Mediator Request/Command-Handler
└── Presentation/
    ├── {Page}Page.xaml
    ├── {Page}Page.xaml.cs
    └── {Page}ViewModel.cs                # [ObservableProperty] nur auf partial properties (MVVMTK0045)
```

**OpenAPI-Client bleibt im eigenen Projekt** (`Heimatplatz.Maui.ApiClient`) - Source-Generatoren sehen einander nicht, sonst brechen `[RelayCommand]`/`[ObservableProperty]` mit generierten DTO-Typen (CS0246).

Features werden in `MauiProgram.cs` / `AddAppServices()` registriert (`services.AddShinyServiceRegistry()` scannt die `[Singleton]`/`[Scoped]`-Attribute automatisch). HttpClient-basierte Services brauchen zusaetzlich `services.AddHttpClient<IAuthApiClient, AuthApiClient>()` in der Feature-`ServiceCollectionExtensions.cs`.

Referenz: `shiny-maui-shell`, `shiny-mediator`, `shiny-di` Skills.
