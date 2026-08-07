namespace Heimatplatz.Api.Features.Dashboards.Services;

/// <summary>
/// Entwirft die Dashboard-Definition zu einem Nutzerwunsch. Liefert die ROHE
/// Ausgabe (JSON-Text) - Parsen und fail-closed-Validieren uebernimmt die
/// Pipeline im DashboardGenerationProcessor, damit Mock und AiConnector durch
/// exakt denselben Pruefpfad laufen.
/// Provider-Auswahl in AddDashboardsFeature: "Mock" (Dev-Default,
/// deterministische Beispiel-Definition) oder "AiConnector".
/// </summary>
public interface IDashboardDesigner
{
    /// <summary>
    /// <paramref name="request"/> = Freitext-Wunsch (Erstellung) bzw.
    /// Verfeinerungs-Anweisung; <paramref name="viewType"/> = Ansichts-Typ aus
    /// DashboardViewTypes (steuert Aufgabe und Katalog-Umfang des Prompts);
    /// <paramref name="currentDefinitionJson"/> ist bei Verfeinerungsrunden die
    /// bestehende Definition (sonst null).
    /// </summary>
    Task<string> DesignAsync(string request, string viewType, string? currentDefinitionJson, CancellationToken cancellationToken = default);
}
