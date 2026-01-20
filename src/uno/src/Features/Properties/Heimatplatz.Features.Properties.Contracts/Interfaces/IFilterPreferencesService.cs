using Heimatplatz.Features.Properties.Contracts.Models;

namespace Heimatplatz.Features.Properties.Contracts.Interfaces;

/// <summary>
/// Service fuer das Laden und Speichern von Benutzer-Filtereinstellungen
/// </summary>
public interface IFilterPreferencesService
{
    /// <summary>
    /// Laedt die Filtereinstellungen des aktuell angemeldeten Benutzers
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Die gespeicherten Filtereinstellungen oder null wenn keine vorhanden</returns>
    Task<FilterPreferencesDto?> GetPreferencesAsync(CancellationToken ct = default);

    /// <summary>
    /// Speichert die Filtereinstellungen fuer den aktuell angemeldeten Benutzer
    /// </summary>
    /// <param name="preferences">Die zu speichernden Einstellungen</param>
    /// <param name="ct">Cancellation Token</param>
    Task SavePreferencesAsync(FilterPreferencesDto preferences, CancellationToken ct = default);

    /// <summary>
    /// Loescht die gespeicherten Filtereinstellungen des aktuell angemeldeten Benutzers
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    Task ClearPreferencesAsync(CancellationToken ct = default);
}
