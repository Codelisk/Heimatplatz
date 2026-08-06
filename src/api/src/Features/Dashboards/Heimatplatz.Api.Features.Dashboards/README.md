# Heimatplatz.Api.Features.Dashboards

KI-Dashboard-Feature ("Meine Uebersicht"): Der Nutzer beschreibt in Freitext,
wonach er sucht und wie er es sehen moechte - die KI entwirft daraus eine
persoenliche Uebersicht aus Bausteinen eines festen Widget-Katalogs.
Konzept: `docs/ki-dashboard-konzept.md` im Repo-Root.

## Architektur (zwei Ebenen)

**Definitions-Ebene** (KI, asynchron): `GenerateDashboardHandler`/`RefineDashboardHandler`
legen eine `UserDashboardRevision` an und planen den TickerQ-Job
(`DashboardGenerationJob`, Payload = Revision-Id). Der `DashboardGenerationProcessor`
laesst den `IDashboardDesigner` (Provider-Switch `Dashboards:Provider`:
`MockDashboardDesigner` deterministisch ohne KI, `AiConnectorDashboardDesigner`
via `RunPromptHttpRequest` im Workspace `projects/heimatplatz`) die Definition
entwerfen und schickt sie durch `DashboardOutputParser` +
`DashboardDefinitionValidator` (fail-closed: unbekannte Kinds/Werte werden
VERWORFEN, Orte serverseitig ueber das Locations-Feature aufgeloest, Limits
gekappt). Clients pollen `GetDashboard` bis Finished/Failed.

**Daten-Ebene** (keine KI, synchron): `GetDashboardDataHandler` loest die Queries
aller Widgets in-process ueber die bestehenden Properties-Requests auf
(`GetPropertiesRequest`, `GetPropertyMapPinsRequest`, `GetPropertyStatsRequest`) -
Blockiert-Ausschluss, IsHidden-Moderation und Bild-Regeln greifen automatisch.
Fail-soft je Widget.

## Widget-Katalog erweitern

1. Resolver-Klasse in `Services/Widgets/` (implementiert `IDashboardWidgetResolver`:
   `Kind`, `Descriptor` fuer den Prompt-Katalog, `Sanitize` fail-closed, `ResolveAsync`).
2. Eine `AddScoped`-Zeile in `Configuration/ServiceCollectionExtensions.cs`.
3. Kind-Konstante in `DashboardWidgetKinds` (Contracts) + Payload-Feld in
   `WidgetDataDto`, falls ein neuer Payload-Typ noetig ist (additiv!).
4. Renderer im Web (`components/dashboard/`) bzw. spaeter MAUI.

Der KI-Prompt-Katalog wird zur Laufzeit vom `DashboardCatalogPromptBuilder` aus den
`Descriptor`n generiert - Prompt und Validator koennen nie auseinanderlaufen.

## Konfiguration (Section "Dashboards")

| Schluessel | Default | Bedeutung |
|---|---|---|
| `Provider` | `Mock` | `Mock` oder `AiConnector` (Prod-Compose setzt `Dashboards__Provider`) |
| `AiConnector:WorkspaceId` | `projects/heimatplatz` | Workspace des Prompts |
| `AiConnector:SectionPath` | `sections/dashboard` | Section mit Rolle/Ton (AGENTS.md am AiConnector-Server) |
| `AiConnector:Model` | leer | optionales Claude-Modell |
| `Limits:MaxPerUser` | 5 | Uebersichten pro Nutzer |
| `Limits:MaxGenerationsPerDay` | 20 | KI-Runden pro Nutzer und 24h (Kostenschutz) |
| `Limits:MaxWidgets` | 8 | Widgets pro Definition |
| `Limits:MaxListItems` | 24 | Treffer pro Listen-Widget |
| `Limits:MaxPromptChars` | 1000 | Laenge des Freitext-Wunschs |
| `MockDelaySeconds` | 8 | kuenstliche Verzoegerung des Mock-Designers |

API-Key/Basis-URL des AiConnectors: zentral im `Heimatplatz.Api.Core.AiConnectorClient`
(`AiConnector:ApiKey`, `Mediator:Http:...`).

## Endpoints

Siehe README des Contracts-Projekts. Alle unter `/api/dashboards`, Bearer-Auth,
Ownership-Check in jedem Handler.

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Dashboards.Contracts`
- `Heimatplatz.Api.Features.Properties.Contracts` (in-process Daten-Requests)
- `Heimatplatz.Api.Features.Locations.Contracts` (Orts-Aufloesung im Validator)
- `Heimatplatz.Api.Core.AiConnectorClient` (KI-Provider)
- `Heimatplatz.Api.Core.Data`, `Heimatplatz.Api.Shared`, TickerQ
