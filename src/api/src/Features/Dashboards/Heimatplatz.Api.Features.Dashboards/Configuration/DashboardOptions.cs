namespace Heimatplatz.Api.Features.Dashboards.Configuration;

/// <summary>
/// Konfiguration des Dashboards-Features (Section "Dashboards").
/// </summary>
public class DashboardOptions
{
    public const string SectionName = "Dashboards";

    /// <summary>
    /// Welcher KI-Provider die Dashboard-Definitionen entwirft:
    /// "Mock" (Dev, deterministische Beispiel-Definition ohne KI) oder
    /// "AiConnector" (externer AiConnector-Backend-Service, Workspace-basiert).
    /// </summary>
    public string Provider { get; set; } = "Mock";

    /// <summary>Einstellungen fuer den "AiConnector"-Provider</summary>
    public DashboardAiConnectorOptions AiConnector { get; set; } = new();

    /// <summary>Mengen-Limits (schuetzen KI-Kosten und Antwortzeiten)</summary>
    public DashboardLimitOptions Limits { get; set; } = new();

    /// <summary>Kuenstliche Verzoegerung des Mock-Designers, damit der Async-Flow testbar bleibt</summary>
    public int MockDelaySeconds { get; set; } = 8;
}

/// <summary>
/// AiConnector-Einstellungen des Dashboards-Features. Basis-URL und API-Key des
/// AiConnector-Backends werden zentral im Heimatplatz.Api.Core.AiConnectorClient
/// konfiguriert (Mediator:Http:... bzw. AiConnector:ApiKey).
/// </summary>
public class DashboardAiConnectorOptions
{
    /// <summary>Workspace, in dem der Prompt ausgefuehrt wird</summary>
    public string WorkspaceId { get; set; } = "projects/heimatplatz";

    /// <summary>
    /// Section-Verzeichnis innerhalb des Workspaces mit Rolle/Ton/Gestaltungsprinzipien
    /// des Dashboard-Designers (AGENTS.md). Der Widget-Katalog steht bewusst NICHT dort,
    /// sondern wird zur Laufzeit aus den Resolver-Selbstbeschreibungen in den Prompt
    /// generiert - Katalog und Validator koennen so nie auseinanderlaufen.
    /// </summary>
    public string SectionPath { get; set; } = "sections/dashboard";

    /// <summary>Optionales Claude-Modell (leer = Default des AiConnectors)</summary>
    public string? Model { get; set; }
}

/// <summary>
/// Mengen-Limits. Ohne Deploy nachschaerfbar (Options).
/// </summary>
public class DashboardLimitOptions
{
    /// <summary>Maximale Anzahl Uebersichten pro Nutzer</summary>
    public int MaxPerUser { get; set; } = 5;

    /// <summary>KI-Generierungen (Erstellen + Verfeinern) pro Nutzer im rollierenden 24h-Fenster</summary>
    public int MaxGenerationsPerDay { get; set; } = 20;

    /// <summary>Maximale Widgets pro Definition (Validator kappt)</summary>
    public int MaxWidgets { get; set; } = 8;

    /// <summary>Maximale Treffer pro Listen-Widget (Validator kappt query.limit)</summary>
    public int MaxListItems { get; set; } = 24;

    /// <summary>Maximale Laenge des Freitext-Wunschs bzw. der Verfeinerungs-Anweisung</summary>
    public int MaxPromptChars { get; set; } = 1000;
}
