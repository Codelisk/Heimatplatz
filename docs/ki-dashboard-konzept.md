# KI-Dashboard: Persönliche Übersicht nach Nutzerwunsch

**Stand:** 06.08.2026 · **Status:** Konzept (nicht umgesetzt)

Der Nutzer beschreibt in eigenen Worten, wonach er sucht und wie er es sehen möchte.
Die KI entwirft daraus ein persönliches Dashboard — Layout, Widgets, Datenauswahl —
und die Plattform zeigt ihm nur noch genau das, was er sich gewünscht hat.

> *„Ich suche ein Haus im Bezirk Vöcklabruck bis 400.000 €. Zeig mir zuerst die
> neuesten Angebote, dazu eine Karte und wie viele neue Inserate pro Woche dazukommen."*

---

## 1. Grundsatzentscheidungen

Diese fünf Entscheidungen tragen das gesamte Konzept. Alles Weitere folgt aus ihnen.

### 1.1 Die KI entwirft eine Beschreibung, keinen Code (Server-Driven UI)

Die KI generiert **niemals** HTML, XAML oder CSS. Sie generiert eine
**Dashboard-Definition**: ein versioniertes JSON-Dokument, das ausschließlich aus
Bausteinen eines festen, im Backend definierten **Widget-Katalogs** besteht.
Web und MAUI sind reine Renderer dieser Definition.

Warum:
- **Sicherheit**: kein KI-generierter Code läuft je beim Nutzer. Kein XSS, kein Stil-Bruch.
- **Zwei Frontends, ein Format**: dieselbe Definition rendert Astro heute und MAUI morgen
  (exakt das Muster, das `PropertyDetailSection`/`BindableLayout` in MAUI schon vorlebt).
- **Skalierbarkeit**: ein neues Widget = eine Resolver-Klasse im Backend + je ein Renderer
  pro Frontend. Die KI lernt es automatisch mit (Katalog wird aus dem Code generiert, §5.3).

### 1.2 Zwei getrennte Ebenen: Definition und Daten

- **Definitions-Ebene** (KI beteiligt, langsam, selten): Wunsch → Definition erzeugen/verfeinern.
  Läuft asynchron als TickerQ-Job mit Polling — exakt das erprobte Muster der
  KI-Inseratsbeschreibung (`PropertyDrafts`).
- **Daten-Ebene** (keine KI, schnell, oft): Ein Endpoint löst die Queries aller Widgets
  serverseitig auf und liefert anzeigefertige Daten. Läuft bei jedem Dashboard-Aufruf,
  kostet nichts Besonderes, ist offline-cachebar (MAUI).

Die KI sieht **nie Inseratsdaten** — sie sieht nur den Nutzerwunsch und den Widget-Katalog.
Daten fließen ausschließlich über die bestehenden, autorisierten Query-Pfade.

### 1.3 Backend-First bleibt: Fail-closed-Validierung im Backend

Alles, was die KI liefert, durchläuft im Backend eine strenge Validierungspipeline
(§6.5), bevor es gespeichert wird. Unbekannte Widget-Arten, Felder oder Werte werden
**verworfen, nie durchgereicht**. Die Frontends bekommen nur garantiert gültige
Definitionen. Orte löst der Server auf (Locations-Feature), nicht die KI.

### 1.4 Die KI gestaltet Komposition, nicht Optik

Designsystem (Anschlagtafel/Zettel, Tokens, Light/Dark-1:1-Regel) bleibt unantastbar.
Die KI entscheidet über: **welche Widgets, in welcher Reihenfolge, in welcher Größe,
mit welchen Daten, mit welcher Darstellungsvariante, mit welchen Titeln/Texten**.
Sie entscheidet nie über Farben, Fonts oder CSS. Das hält jedes generierte Dashboard
im Marken-Look („Design: klassisch statt clever") und macht Rendering trivial.

### 1.5 Nicht Erfüllbares wird sichtbar gemacht, nicht erfunden

Wünscht der Nutzer etwas, das der Katalog (noch) nicht kann („zeig mir die
Preisentwicklung als Diagramm"), erfindet die KI nichts, sondern trägt es in eine
`unsupportedWishes`-Liste der Definition ein. Das Frontend zeigt es ehrlich an
(„Das kann Ihre Übersicht noch nicht: …"). Nebeneffekt: Diese Liste ist die
**Produkt-Roadmap aus echten Nutzerwünschen** — welche Widgets als Nächstes gebaut
werden, sagt uns der Bestand an unerfüllten Wünschen.

---

## 2. Nutzererlebnis

Öffentlicher Name (Empfehlung, kein Technik-Jargon — vgl. Maklerverzeichnis-Regel):
**„Meine Übersicht"**, Route `/meine-uebersicht/` (Web) bzw. Flyout-Eintrag (MAUI).
Angemeldeten Nutzern vorbehalten (AuthGate), `noindex`, nicht in der Sitemap.

1. **Erstellen**: Freitextfeld mit Beispiel-Chips („Häuser bis 300.000 € in …",
   „Nur Grundstücke, sortiert nach Preis, mit Karte"). Absenden → Fortschrittsanzeige
   („Ihre Übersicht wird zusammengestellt, das dauert bis zu einer Minute").
2. **Ansehen**: Das fertige Dashboard rendert mit Live-Daten. Kopfzeile: KI-Titel
   + Intro-Satz. Danach die Widgets im Raster.
3. **Verfeinern**: Eine „Wunsch"-Zeile unter dem Dashboard („Mach die Karte größer",
   „nur noch Privatanbieter") startet eine neue KI-Runde auf Basis der aktuellen
   Definition — dasselbe Runden-Muster wie der Marketing-KI-Check
   (`Instruction` + bisheriger Stand). Vorherige Fassung bleibt als Revision erhalten
   → „Rückgängig".
4. **Verwalten**: Mehrere Übersichten pro Nutzer (Limit 5), umbenennen, löschen.
   Einstieg über Profil-Quicklink + Hauptnavigation (authOnly).

Nicht erfüllbare Wünsche erscheinen als dezente Hinweiszeile (§1.5).

---

## 3. Gesamtarchitektur

```
Nutzer ──Freitext──▶ Web (Astro) / MAUI
                          │  POST /api/dashboards/generate   (Bearer-Auth)
                          ▼
              API-Feature „Dashboards"
                          │ 1. UserDashboard anlegen (Status Queued)
                          │ 2. TickerQ-Job einplanen (+2s, Retries 30/120/300)
                          ▼
              DashboardGenerationProcessor (Job)
                          │ Prompt = Wunsch + Widget-Katalog (aus Code generiert)
                          │        + Ausgabe-Schema + ggf. bisherige Definition
                          ▼
              AiConnector  POST /api/prompt  (RunPromptHttpRequest,
                          │  WorkspaceId "projects/heimatplatz",
                          │  Section sections/dashboard/AGENTS.md)
                          ▼
              JSON-Output → Parser → Validierungspipeline (fail-closed)
                          │ Orte auflösen, Limits kappen, Unbekanntes verwerfen
                          ▼
              UserDashboard.DefinitionJson (versioniert) + Revision
                          ▲
        Client pollt GET /api/dashboards/{id}  bis Status Finished
                          │
        dann GET /api/dashboards/{id}/data
                          ▼
              Widget-Resolver (je Kind eine Klasse) rufen IN-PROCESS die
              bestehenden Mediator-Requests auf (GetProperties, MapPins, …)
              → anzeigefertige WidgetData-Payloads (fail-soft je Widget)
```

Kein Streaming nötig: der AiConnector kennt keins, und das Polling-Muster ist mit
`PropertyDrafts` (MAUI) bereits produktionserprobt. Der geteilte
`RunPromptHttpRequestHandler`-Timeout-Override (5 min) deckt auch dieses Feature ab.

---

## 4. Die Dashboard-Definition (das zentrale Datenformat)

### 4.1 Beispiel

```json
{
  "schemaVersion": 1,
  "title": "Häuser rund um Vöcklabruck",
  "intro": "Ihre Übersicht für Häuser bis 400.000 € im Bezirk Vöcklabruck.",
  "widgets": [
    {
      "id": "w1", "kind": "stat-row", "size": "full",
      "query": { "types": ["house"], "locations": ["Vöcklabruck"], "priceMax": 400000 },
      "options": { "tiles": ["total", "newLast7Days", "medianPrice"] }
    },
    {
      "id": "w2", "kind": "property-list", "size": "l", "title": "Neueste Angebote",
      "query": { "types": ["house"], "locations": ["Vöcklabruck"], "priceMax": 400000,
                 "sort": "newest", "limit": 6 },
      "options": { "variant": "grid" }
    },
    {
      "id": "w3", "kind": "map", "size": "m",
      "query": { "types": ["house"], "locations": ["Vöcklabruck"], "priceMax": 400000 }
    },
    {
      "id": "w4", "kind": "highlight", "size": "full", "title": "Günstigstes Angebot",
      "query": { "types": ["house"], "locations": ["Vöcklabruck"], "priceMax": 400000,
                 "sort": "price-asc", "limit": 1 }
    }
  ],
  "unsupportedWishes": ["Preisentwicklung als Diagramm"]
}
```

### 4.2 Regeln des Formats

- **`schemaVersion`** in jeder Definition. Validator akzeptiert nur die aktuelle
  Version von der KI; gespeicherte ältere Definitionen werden beim Laden migriert
  (Migrations-Hook, v1: trivial). Renderer sind **tolerant reader**: unbekannte
  Felder werden ignoriert, unbekannte Widget-Kinds übersprungen — damit können API
  und Frontends unabhängig deployen.
- **`size`** ist semantisch (`s | m | l | full`), nicht pixelbasiert. Web mappt auf
  ein 12-Spalten-Grid (s=4, m=6, l=8, full=12; mobil stapelt alles), MAUI aufs
  Mittelspalten-System (Tablet: full = ganze Breite, s/m/l = halbe) bzw. Phone: alles
  gestapelt. Die KI muss nichts über Bildschirme wissen.
- **`query`** ist ein einheitliches, stark typisiertes Objekt (`PropertyQuery`) für
  alle immobilienbasierten Widgets — dieselben Achsen wie `PropertyQueryFilters` im
  Backend: `types`, `sellers`, `locations`, `priceMin/Max`, `areaMin/Max`, `roomsMin`,
  `newBuild`, `zv` (default aus, Produktregel), `searchText`, `sort`, `limit`.
  Wichtig: `PriceMin/Max`, `AreaMin/Max`, `RoomsMin` sind in der API **schon
  implementiert** (`GetPropertiesRequest`), nur vom Web bisher ungenutzt — das
  Dashboard nutzt sie ohne Backend-Neubau.
- **`locations`** liefert die KI als Freitext (Ort/Bezirk, wie der Nutzer ihn nennt).
  Der Validator löst sie über das Locations-Feature in `MunicipalityIds` auf
  (Bezirk → alle Gemeinden) und speichert **beides**: IDs (für Queries) und
  Originaltext (für Anzeige + Verfeinerungsrunden). Unauflösbares wandert nach
  `unsupportedWishes`.
- **Texte** (`title`, `intro`, Widget-Titel) schreibt die KI (deutsch, Sie-Form —
  steht in der Workspace-Section). Sie werden beim Rendern immer als Text escaped,
  nie als HTML interpretiert.

### 4.3 „Nur die Daten, die der Nutzer sich wünscht"

Zwei Mechanismen decken den Kern-Wunsch ab:

1. **Query-Ebene**: jedes Widget zeigt nur die gefilterte Treffermenge.
2. **Darstellungs-Ebene**: `options.variant` pro Widget steuert die Informationsdichte.
   v1 mit festen Varianten statt freier Feldauswahl (Designsprache bleibt intakt):
   - `property-list`: `grid` (Zettel-Karten wie Startseite) · `list` (kompakte Zeilen) ·
     `minimal` (nur Titel, Preis, Ort)
   - `stat-row`: Auswahl der Kacheln (`tiles`)
   - v2-Erweiterung: echte Feldauswahl (`fields: ["price", "area", …]`) — im Schema
     bereits vorgesehen, Renderer-seitig später.

---

## 5. Widget-Katalog

### 5.1 Katalog v1

| Kind | Zweck | Datenquelle (in-process) |
|---|---|---|
| `property-list` | Trefferliste (grid/list/minimal, limit ≤ 24) | `GetPropertiesRequest` |
| `stat-row` | Kennzahl-Kacheln: Trefferzahl, neu in 7 Tagen, Median-/Min-/Maxpreis | neuer `GetPropertyStatsRequest` (§6.4) |
| `map` | Faltkarte mit Pins der Treffermenge | `GetPropertyMapPinsRequest` |
| `highlight` | Ein hervorgehobenes Top-Inserat (Hero-Karte) | `GetPropertiesRequest` (limit 1) |
| `new-listings` | „Neu seit …"-Feed (CreatedAfter) | `GetPropertiesRequest` |
| `text-note` | Statischer KI-Text (Einordnung, Hinweise) | keine (Text steht in der Definition) |

Bewusst klein: sechs Kinds reichen, um die häufigsten Wünsche abzudecken, und der
`unsupportedWishes`-Kanal sagt uns, was als Nächstes fehlt. Kandidaten für später:
ZV-Terminkalender (`AuctionDate` existiert im DTO), Favoriten-Widget,
Preisverteilungs-Balken, Vergleichstabelle zweier Orte.

### 5.2 Backend-Muster: selbstbeschreibende Resolver

```csharp
public interface IDashboardWidgetResolver
{
    string Kind { get; }
    WidgetDescriptor Descriptor { get; }        // Beschreibung für Katalog/Prompt (§5.3)
    IReadOnlyList<string> Validate(WidgetInstance widget);          // fail-closed
    Task<WidgetDataDto> ResolveAsync(WidgetInstance widget, ResolveContext ctx,
        CancellationToken ct);
}
```

- Implementierungen tragen `[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]`
  und werden als `IEnumerable<IDashboardWidgetResolver>` injiziert — **neues Widget =
  neue Klasse, null Registrierungscode** (Shiny-DI-Registry macht den Rest).
- Resolver rufen die bestehenden Mediator-Requests **in-process** auf (kein HTTP,
  Muster `DraftDescriptionProcessor`). Dadurch gelten Blockiert-Ausschluss,
  `IsHidden`-Moderation, ZV-Default-aus und Bild-URL-Regeln (`w=640` in Listen!)
  automatisch — keine zweite Query-Logik, die auseinanderlaufen kann.
- `WidgetDescriptor` beschreibt Kind, Zweck, unterstützte Query-Felder, Optionen mit
  erlaubten Werten und Grenzen — maschinenlesbar.

### 5.3 Der Katalog generiert sich in den Prompt (kein Drift)

Der Prompt-Builder rendert die `WidgetDescriptor`-Liste zur Laufzeit als kompakten
Katalog-Block **direkt in den Prompt** (wenige KB). Damit ist der Katalog, den die KI
sieht, **immer identisch** mit dem, was der Validator akzeptiert — eine Quelle der
Wahrheit im C#-Code, kein Sync-Schritt, kein Drift zwischen Repo und Workspace.
Präzedenzfall: der Marketing-KI-Check definiert sein Ausgabe-JSON auch im Prompt
selbst, nicht in der Workspace-Datei.

---

## 6. API-Feature `Dashboards`

### 6.1 Projekte (Standardstruktur laut CLAUDE.md, beide mit README.md)

```
src/api/src/Features/Dashboards/
├── Heimatplatz.Api.Features.Dashboards/
│   ├── Configuration/ServiceCollectionExtensions.cs   # AddDashboardsFeature(cfg, backgroundJobsEnabled)
│   ├── Configuration/DashboardOptions.cs              # §6.6
│   ├── Data/Entities/UserDashboard.cs
│   ├── Data/Entities/UserDashboardRevision.cs
│   ├── Data/Configurations/…
│   ├── Handlers/                                      # HTTP-Endpoints (§6.3)
│   ├── Jobs/DashboardGenerationJob.cs                 # MapTicker-Delegate, Muster DraftDescriptionJob
│   ├── Services/
│   │   ├── IDashboardDesigner.cs                      # KI-Abstraktion
│   │   ├── MockDashboardDesigner.cs                   # Dev-Default, kanonische Beispiel-Definition + Delay
│   │   ├── AiConnectorDashboardDesigner.cs            # Prompt-Builder + RunPromptHttpRequest
│   │   ├── DashboardOutputParser.cs                   # StripFences + { … } -Extraktion, tolerant
│   │   ├── DashboardDefinitionValidator.cs            # §6.5
│   │   ├── DashboardCatalogPromptBuilder.cs           # §5.3
│   │   ├── IDashboardGenerationJobScheduler.cs        # + TickerQ- und NoOp-Variante
│   │   ├── DashboardGenerationProcessor.cs
│   │   ├── Widgets/…Resolver.cs                       # je Kind einer (§5.2)
│   │   └── DashboardsUserDataEraser.cs                # IUserDataEraser (Konto-Löschung)
│   └── README.md
└── Heimatplatz.Api.Features.Dashboards.Contracts/
    ├── Mediator/Requests/…                            # §6.3
    ├── Models/DashboardDefinition.cs                  # + WidgetInstance, PropertyQuery, Enums
    ├── Models/DashboardGenerationStatus.cs            # Queued/InProgress/Finished/Failed
    └── README.md
```

Registrierung (die zwei Pflicht-Zeilen, sonst existiert nichts):
`AddDashboardsFeature(...)` in `Core.Startup/ServiceCollectionExtensions.AddApiServices`
**und** `Heimatplatz.Api.Features.Dashboards.MediatorEndpoints.MapGeneratedMediatorEndpoints(app)`
in `MapEndpoints`.

### 6.2 Entities

```csharp
public class UserDashboard : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = "";            // KI-Titel, umbenennbar
    public string? DefinitionJson { get; set; }        // validierte Definition (aktuelle Fassung)
    public int SchemaVersion { get; set; }
    public int SortOrder { get; set; }

    // Generierungs-Status als EIGENE Spalten, nie im JSON-Blob
    // (Lehre aus PropertyDrafts: Upserts dürfen Job-Fortschritt nicht überschreiben)
    public DashboardGenerationStatus GenerationStatus { get; set; }
    public string? GenerationError { get; set; }
    public DateTimeOffset? GenerationRequestedAt { get; set; }
    public DateTimeOffset? GenerationCompletedAt { get; set; }
}

public class UserDashboardRevision : BaseEntity
{
    public Guid DashboardId { get; set; }
    public string UserPrompt { get; set; } = "";       // Wunsch bzw. Verfeinerung
    public string? DefinitionJson { get; set; }        // Ergebnis der Runde
    public string? RawOutputExcerpt { get; set; }      // KI-Rohausgabe, gekappt (Debug/Prompt-Tuning)
}
```

Migration in beiden Provider-Sets (Muster der letzten Features). Index auf
`(UserId)`, Revision auf `(DashboardId, CreatedAt)`.

### 6.3 Endpoints (`[MediatorHttpGroup("/api/dashboards")]`, alle `RequiresAuthorization`)

| Route | Request | Verhalten |
|---|---|---|
| `POST /generate` | `GenerateDashboardRequest(string Prompt)` | Quoten prüfen → `UserDashboard` (Queued) + Revision anlegen → Job einplanen → `{ DashboardId }` |
| `GET /` | `GetDashboardsRequest` | Liste des Nutzers (Id, Titel, Status, UpdatedAt) |
| `GET /{Id}` | `GetDashboardRequest` | Status + validierte Definition (Polling-Endpoint) |
| `POST /{Id}/refine` | `RefineDashboardRequest(string Instruction)` | wie Generate, aber Prompt enthält aktuelle Definition; Doppel-Start bei Queued/InProgress abgelehnt (Guard wie `GenerateDraftDescriptionHandler`) |
| `POST /{Id}/revert` | `RevertDashboardRequest` | letzte Revision mit Definition zurückspielen (kein KI-Aufruf) |
| `PUT /{Id}` | `UpdateDashboardRequest(string? Title, int? SortOrder)` | umbenennen/sortieren |
| `DELETE /{Id}` | `DeleteDashboardRequest` | löschen inkl. Revisionen |
| `GET /{Id}/data` | `GetDashboardDataRequest(Guid Id, string? WidgetIdsJson)` | Daten-Ebene, §6.4 |

Ownership-Prüfung in jedem Handler (`UserId` aus Claims), Fehlerbild wie üblich
ProblemDetails; KI-Fehler landen als `GenerationStatus=Failed` + nutzerfreundlicher
`GenerationError`, nie als 500 beim Polling.

### 6.4 Daten-Ebene

`GetDashboardDataResponse` enthält pro Widget ein `WidgetDataDto`:

```csharp
public record WidgetDataDto(
    string WidgetId,
    string Kind,
    bool Success,
    string? Error,                    // nutzerfreundlich, fail-soft je Widget
    PropertyListData? PropertyList,   // genau EIN payload-Feld gesetzt, je nach Kind
    StatRowData? StatRow,
    MapData? Map,
    TextNoteData? TextNote);
```

Bewusst **nullable-typisierte Payload-Felder statt Polymorphie**: der OpenAPI-
Generator (MAUI-Client!) kann das sauber abbilden, neue Widgets sind additive,
nicht-brechende Erweiterungen. `PropertyListData` transportiert die bestehende
`PropertyListItemDto` — MAUI rendert mit der vorhandenen `PropertyCard`, das Web mit
der vorhandenen Karten-Renderfunktion. Jedes Widget resolvet unabhängig in
try/catch (Vorbild `/intern`: eine kaputte Quelle reißt nie die Seite).

**Neuer Baustein `GetPropertyStatsRequest`** (Features/Properties, wiederverwendbar
auch für die normale Suche): liefert zu einem Filtersatz `Total`, `NewLast7Days`,
`MinPrice`, `MaxPrice`, `MedianPrice` — eine kleine Aggregat-Query über
`PropertyQueryFilters.ApplyCommonFilters`, damit Kennzahlen und Trefferliste
garantiert dieselbe Menge meinen.

### 6.5 Validierungspipeline (fail-closed)

1. **Parsen**: `DashboardOutputParser` (StripFences, `{`…`}`-Extraktion,
   case-insensitive — Muster `MarketingEmailOutputParser`).
2. **Schema**: `schemaVersion` == aktuell; Pflichtfelder; max. 8 Widgets.
3. **Semantik je Widget**: Kind existiert im Resolver-Registry → dessen
   `Validate()`; unbekannte Kinds/Optionen/Felder werden **verworfen** und als
   Warnung in die Revision geloggt; Limits gekappt (`limit ≤ 24` usw.).
4. **Orte auflösen**: Freitext → `MunicipalityIds` via Locations-Feature;
   unauflösbar → `unsupportedWishes`.
5. **Ergebnis**: ≥ 1 gültiges Widget → speichern (Finished). 0 gültige Widgets →
   Failed mit verständlicher Meldung („Ihren Wunsch konnte ich noch nicht in eine
   Übersicht übersetzen — formulieren Sie ihn bitte etwas konkreter.").

### 6.6 Konfiguration & Limits

```json
"Dashboards": {
  "Provider": "Mock",                          // Prod-Compose: AiConnector
  "AiConnector": {
    "WorkspaceId": "projects/heimatplatz",
    "SectionPath": "sections/dashboard",
    "Model": null
  },
  "Limits": {
    "MaxPerUser": 5,
    "MaxGenerationsPerDay": 20,                // Generate + Refine zusammen, je Nutzer
    "MaxWidgets": 8,
    "MaxListItems": 24,
    "MaxPromptChars": 1000
  },
  "MockDelaySeconds": 8
}
```

- Gleicher Provider-Switch wie AiListing/Marketing, `services.AddAiConnectorClient(cfg)`
  nur im AiConnector-Zweig (Registrierung ist idempotent).
- Startup-Warnung in `Program.cs` analog AiListing, Compose-Env
  `Dashboards__Provider: AiConnector` für prod **und** test.
- Tagesquote schützt die AiConnector-CLI-Kosten; Überschreitung → freundlicher
  Fehler im Generate/Refine-Handler, kein Job.
- TickerQ-Fallen aus dem Bestand übernehmen: `ExecutionTime = UtcNow.AddSeconds(2)`,
  `MapTicker`-Delegate (kein Source-Gen), NoOp-Scheduler ohne ConnectionString
  (Build-Zeit-OpenAPI/Tests).

---

## 7. AiConnector-Workspace: Aufbau der Dashboard-Section

Auf dem aiconnector-Server (`/srv/aiconnector/workspaces/projects/heimatplatz/`,
Änderungen wirken sofort, Commits als `aiconnector`-User):

```
sections/dashboard/
├── AGENTS.md        # Rolle + stabile Regeln (unten)
└── examples.md      # kuratierte Wunsch→JSON-Paare inkl. Verfeinerungsrunden
```

**Arbeitsteilung Prompt ↔ Workspace** (die eigentliche Skalierungs-Grundlage):

| Lebt im **Prompt** (aus C# generiert, immer aktuell) | Lebt in der **Workspace-Section** (stabil, ohne Deploy änderbar) |
|---|---|
| Widget-Katalog aus `WidgetDescriptor`n (§5.3) | Rolle: „Du bist der Übersichts-Designer von Heimatplatz…" |
| Ausgabe-JSON-Kontrakt + `schemaVersion` | Gestaltungsprinzipien (erst Kennzahlen, dann Liste; Karte nur bei Ortsbezug; max. Widgets sparsam einsetzen) |
| Nutzerwunsch in `"""`-Fences | Sprachregeln: Deutsch, Sie-Form, Preisformat „€ 520.000" |
| Bei Refine: aktuelle Definition + Instruction | Umgang mit Unerfüllbarem → `unsupportedWishes`, NIE erfinden |
| | Verweis auf `examples.md` |

Erste Prompt-Zeile wie überall: *„Lies zuerst die Datei sections/dashboard/AGENTS.md
in diesem Workspace und folge deren Regeln exakt."* Die Wurzel-`AGENTS.md`
(Dispatcher) bekommt eine Zeile für die neue Section.

Damit skaliert das System auf beiden Seiten unabhängig: neue Widgets erscheinen
automatisch im Prompt-Katalog (Code), Ton-/Design-Nachschärfungen passieren live in
der Section (Workspace), und `examples.md` wächst mit den Lehren aus echten
`RawOutputExcerpt`-Revisionen.

---

## 8. Umsetzung Astro (Phase 1)

Slice-Konvention: `features/` = nur `.ts`, UI in `components/`, Route in `pages/`.

```
src/web/src/features/dashboard/
├── spec.ts          # DashboardDefinition/WidgetData-Typen + tolerante Guards
│                    #   (client-safe, keine Server-Imports — Muster search-query.ts)
└── labels.ts        # Kind/Status → deutsche Labels
src/web/src/components/dashboard/
├── DashboardApp.astro       # Shell: AuthGate, Erstell-Formular, Polling, Raster
├── DashboardTemplates.astro # <template data-widget-template="stat-row"> je Kind —
│                            #   Markup bleibt in .astro, Script klont nur (statt DOM-Bau in JS)
└── DashboardScript.astro    # apiRequest()-Aufrufe, Statusmaschine, Refine-Zeile
src/web/src/pages/meine-uebersicht/index.astro   # BaseLayout noindex + <DashboardApp/>
src/web/src/i18n/de/dashboard.ts                 # Texte, in i18n/index.ts registrieren
```

Entscheidende Punkte:

- **Kein SSR der Nutzerdaten**: Auth ist clientseitig (localStorage-Session), der
  Server kennt den Nutzer beim SSR nicht. Die Seite folgt dem
  `UserListPage`-Muster: SSR-Shell + `AuthGate` + Client-Hydration über
  `apiRequest()` aus `PropertyStateScript` (Token-Refresh, 401-Retry inklusive).
  **Kein Astro-Proxy nötig** — die Endpoints sind normale Bearer-Auth-Endpoints,
  kein Admin-Key.
- **Rendering über `<template>`-Klone** statt JS-DOM-Bau: hält das Markup im
  Designsystem (`--zettel-shadow`, Starwind-Karten) und vermeidet eine vierte Kopie
  der Property-Karten-Logik — Inserate rendert die bestehende
  `createApiPropertyCard()`-Funktion (bzw. deren geplante Extraktion nach
  `features/properties/`).
- **Karten-Widget** kapselt `PropertyMapPanel` bzw. eine abgespeckte
  Pins-Only-Variante; Pins kommen fertig aus `MapData` (Privacy-Jitter serverseitig).
- **Polling**: 3–5 s Intervall auf `GET /{id}`, Statustexte Queued/InProgress,
  Failsafe-Timeout mit Fehlermeldung + „Erneut versuchen".
- **Refine-Zeile**: Enter per `preventDefault` abfangen (bekannte Formular-Falle aus
  dem KI-Check), Buttons während laufender Runde sperren.
- Navigation: `SiteHeader.astro` `navItems` (authOnly), Profil-`quickLinks`,
  **nicht** in `sitemap.xml.ts`.
- Skeleton-Zustände mit der vorhandenen Starwind-`skeleton`-Komponente.

---

## 9. Umsetzung MAUI (Phase 2 — jetzt mitgedacht)

- **Client-Generierung gratis**: API bauen → `src/api/openapi/Heimatplatz.Api.json`
  aktualisiert sich → MAUI-Build generiert `GenerateDashboardHttpRequest` & Co.
  Die nullable-typisierten `WidgetDataDto`-Payloads (§6.4) sind genau deshalb so
  geschnitten, dass der Generator sie sauber abbildet.
- **Feature-Ordner** `Features/Dashboard/` nach Hausmuster: `DashboardListPage`/
  `DashboardPage` + ViewModels (`[ShellMap]`), `[Singleton]`-Service für
  Polling/Cache, Flyout-Eintrag.
- **Rendering**: `BindableLayout` über eine `ObservableCollection<WidgetViewModel>`.
  Für die heterogenen Widget-Kinds empfehle ich einen kleinen
  `DashboardWidgetTemplateSelector` (`DataTemplateSelector`) — wäre der erste im
  Repo, ist aber der Standard-MAUI-Weg für genau diesen Fall; Alternative ohne
  Selector (ein Template + `IsVisible`-DataTrigger je Kind) funktioniert, skaliert
  aber schlechter mit wachsendem Katalog. Inserate rendert die bestehende
  `PropertyCard` (CardMode-Varianten = `variant`-Mapping), Kennzahlen das
  StatTile-Muster der Detailseite, Karte die native `PropertyLocationMapView`.
- **Offline**: `GetDashboardsHttpRequest`, `GetDashboardHttpRequest`,
  `GetDashboardDataHttpRequest` in die `OfflineDataConfiguration`-Allowlist
  (RefreshAfterSeconds z. B. 300) — Dashboard bleibt offline sichtbar,
  `NullResponseGuardMiddleware` greift.
- **Polling** wie beim Wizard (5 s, eigene CTS, Resume nach App-Neustart über den
  Status im GET).

---

## 10. Sicherheit, Datenschutz, Kosten

- **Prompt-Inhalt**: ausschließlich Nutzer-Freitext + Katalog + ggf. bisherige
  Definition. Keine E-Mail, kein Name, keine Inseratsdaten, keine IDs mit
  Personenbezug. (Gleiches Prinzip wie Marketing: Kontakt-Mail geht nie an die KI.)
- **Kein KI-Code im Client** (§1.1); alle KI-Texte werden escaped gerendert.
- **Autorisierung**: Bearer-Auth + Ownership-Check je Handler; Daten-Ebene läuft
  unter dem Nutzerkontext → Blockiert-/`IsHidden`-Regeln greifen automatisch.
- **Kosten**: Tagesquote (§6.6), Doppel-Start-Guard, Job-Retries begrenzt
  (30/120/300 wie Drafts). Mock-Provider als Dev-Default hält lokale Läufe kostenlos.
- **Beobachtbarkeit**: Telemetrie-Spans um Generierung + Widget-Resolve;
  `RawOutputExcerpt` in Revisionen für Prompt-Tuning (gekappt, z. B. 8000 Zeichen).
  Achtung: Prod loggt Info-Level nicht — Fehlerpfade auf Warning loggen.

---

## 11. Teststrategie

- **Unit**: `DashboardOutputParser` (Fences, Umgebungstext, kaputtes JSON),
  `DashboardDefinitionValidator` (fail-closed: unbekannte Kinds fliegen, Limits
  kappen, 0-Widget-Fall), Ortsauflösung (Bezirk vs. Gemeinde, unauflösbar),
  je Resolver die Query-Übersetzung.
- **Integration** (`Api.IntegrationTests`): kompletter Lebenszyklus mit Mock-Provider
  (Generate → Queued → Job → Finished → Data), Quoten, Ownership-Verweigerung,
  Fail-soft der Daten-Ebene (ein Resolver wirft → Rest liefert).
- **E2E Web**: Playwright gegen lokale Wegwerf-API
  (SQLite, `-p:OpenApiGenerateDocuments=false`, `Database__Provider=Sqlite` explizit) —
  Mock-Definition deckt alle sechs Widget-Kinds ab; Light/Dark, Mobile 375px.
- **E2E MAUI** (Phase 2): DevFlow-Durchlauf Windows/Emulator gegen Test-API.
- Der `MockDashboardDesigner` liefert deterministisch die kanonische
  Beispiel-Definition (§4.1) mit konfigurierbarem Delay — damit ist der gesamte
  Async-Flow ohne AiConnector testbar (bewährtes `MockDelaySeconds`-Muster).

---

## 12. Umsetzungsphasen

| Phase | Inhalt | Ergebnis |
|---|---|---|
| **0 — Fundament** | Contracts + Entities + Migrationen, Validator, Resolver-Registry mit den 6 v1-Widgets, `GetPropertyStatsRequest`, Endpoints, TickerQ-Job, Mock-Provider, Unit-/Integrationstests | API end-to-end lauffähig ohne KI |
| **1 — Web** | Slice + Seite + Templates + Polling + Refine, Navigation/Profil, i18n; danach `sections/dashboard/` im Workspace anlegen, `AiConnectorDashboardDesigner` gegen Test-API scharf schalten, Prompt-Tuning über `examples.md` | Feature live auf test.heimatplatz.at |
| **2 — MAUI** | Feature-Ordner, TemplateSelector, Offline-Allowlist, Polling; Release-Zug | Parität in der App |
| **3 — Ausbau** | Widgets nach `unsupportedWishes`-Auswertung; Feldauswahl (`fields`); manuelles Umsortieren/Entfernen einzelner Widgets ohne KI; optional: Dashboard-Query als Push-Quelle (Verwandtschaft zu `SameAsSearch`) | wächst mit echten Wünschen |

---

## 13. Offene Entscheidungen (mit Empfehlung)

1. **Öffentlicher Name/Route** — Empfehlung „Meine Übersicht" / `/meine-uebersicht/`;
   Alternativen: „Mein Heimatplatz", „Für mich". (Kundenwunsch-Regel: kein Jargon.)
2. **Kennzahlen-Set der `stat-row`** — Empfehlung: Total, NewLast7Days, Min/Median/Max-Preis
   reichen für v1; Preis/m² wäre der nächste Kandidat.
3. **`DataTemplateSelector` in MAUI einführen** (erster im Repo) vs. DataTrigger-Weg —
   Empfehlung: Selector, sauber dokumentiert im Feature-README.
4. **Tagesquote** (20/Tag) und Dashboard-Limit (5) — Bauchgefühl-Werte, per Options
   ohne Deploy nachschärfbar.
