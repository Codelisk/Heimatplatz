namespace Heimatplatz.Maui.Features.Debug.Services;

/// <summary>
/// Debug-Umschalter zwischen lokaler Entwicklungs-API, Test-API und Produktions-API.
/// Wirkt sofort, da Shiny.Mediator die Base-URL pro Request aus der Konfiguration liest.
/// </summary>
public interface IApiEndpointService
{
    /// <summary>Lokale Entwicklungs-API (plattformabhaengig, siehe ApiEndpoints)</summary>
    string DevelopmentUrl { get; }

    /// <summary>Test-API (Testdatenbank am Hetzner-Server)</summary>
    string TestUrl { get; }

    /// <summary>Produktions-API</summary>
    string ProductionUrl { get; }

    /// <summary>Aktuell persistierte Auswahl</summary>
    ApiEndpointKind SelectedEndpoint { get; }

    /// <summary>Aktuell aktive Base-URL laut Konfiguration</summary>
    string CurrentUrl { get; }

    /// <summary>Schaltet den Endpunkt um und persistiert die Auswahl in den Preferences</summary>
    void SetEndpoint(ApiEndpointKind kind);
}
