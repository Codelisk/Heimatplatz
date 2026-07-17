namespace Heimatplatz.Api.Features.AiListing.Configuration;

/// <summary>
/// Konfiguration fuer die KI-Unterstuetzung bei der Inseratserstellung
/// (Medien-Upload-Limits + Beschreibungs-Generierung).
/// </summary>
public class AiListingOptions
{
    public const string SectionName = "AiListing";

    /// <summary>
    /// Welcher KI-Provider verwendet wird:
    /// "Mock" (Dev, Template-Beschreibung ohne KI) oder
    /// "AiConnector" (externer AiConnector-Backend-Service, Workspace-basiert).
    /// </summary>
    public string Provider { get; set; } = "Mock";

    /// <summary>Maximale Anzahl Fotos pro Inserat/Upload</summary>
    public int MaxImages { get; set; } = 20;

    /// <summary>Maximale Anzahl Videos pro Upload (nur noch Altbestand, der Wizard laedt keine Videos mehr hoch)</summary>
    public int MaxVideos { get; set; } = 3;

    /// <summary>Maximale Groesse eines Videos in MB</summary>
    public int MaxVideoSizeMb { get; set; } = 60;

    /// <summary>Einstellungen fuer den "AiConnector"-Provider</summary>
    public AiConnectorOptions AiConnector { get; set; } = new();

    /// <summary>Einstellungen fuer die Beschreibungs-Generierung</summary>
    public ListingDescriptionOptions Description { get; set; } = new();
}

/// <summary>
/// Feature-spezifische Einstellungen fuer den "AiConnector"-Provider.
/// Basis-URL und API-Key des AiConnector-Backends selbst werden zentral im
/// Heimatplatz.Api.Core.AiConnectorClient konfiguriert (Mediator:Http:... bzw. AiConnector:ApiKey).
/// </summary>
public class AiConnectorOptions
{
    /// <summary>Workspace, in dem der Prompt ausgefuehrt wird</summary>
    public string WorkspaceId { get; set; } = "projects/heimatplatz";

    /// <summary>Optionales Claude-Modell (leer = Default des AiConnectors)</summary>
    public string? Model { get; set; }
}

/// <summary>
/// Fix im Backend definierter Rahmen fuer generierte Inserats-Beschreibungen.
/// </summary>
public class ListingDescriptionOptions
{
    /// <summary>Untergrenze des Wortbereichs der generierten Beschreibung</summary>
    public int MinWords { get; set; } = 100;

    /// <summary>Obergrenze des Wortbereichs der generierten Beschreibung</summary>
    public int MaxWords { get; set; } = 160;

    /// <summary>Kuenstliche Dauer des Mock-Providers in Sekunden (macht den asynchronen Flow sichtbar)</summary>
    public int MockDelaySeconds { get; set; } = 8;
}
