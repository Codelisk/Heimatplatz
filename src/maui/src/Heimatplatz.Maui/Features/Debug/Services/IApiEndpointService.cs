namespace Heimatplatz.Maui.Features.Debug.Services;

/// <summary>
/// Umschalter zwischen lokaler Entwicklungs-API, Test-API und Produktions-API.
/// Wirkt sofort, da Shiny.Mediator die Base-URL pro Request aus der Konfiguration liest.
/// Nur in Builds mit Entwicklerwerkzeugen erreichbar (siehe Core/Build/AppChannels.cs).
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

    /// <summary>
    /// Feuert nach jedem Endpunktwechsel. Die Shell haengt daran ihre Umgebungs-Pille -
    /// im fixierten Desktop-Flyout gibt es sonst kein Ereignis, an dem sie nachziehen koennte.
    /// </summary>
    event EventHandler? EndpointChanged;

    /// <summary>Schaltet den Endpunkt um und persistiert die Auswahl in den Preferences</summary>
    void SetEndpoint(ApiEndpointKind kind);

    /// <summary>
    /// Vollstaendiger Umgebungswechsel: Endpunkt umstellen, angemeldete Session beenden
    /// (Tokens gelten immer nur fuer eine Umgebung) und alle offenen Listen neu laden lassen.
    /// </summary>
    /// <returns>True, wenn dabei eine bestehende Anmeldung beendet wurde</returns>
    Task<bool> SwitchEndpointAsync(ApiEndpointKind kind);
}
