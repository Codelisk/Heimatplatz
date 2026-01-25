using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Speichern der Filtereinstellungen fuer den authentifizierten Benutzer
/// </summary>
/// <param name="SelectedOrtes">Liste der ausgewaehlten Orte</param>
/// <param name="SelectedAgeFilter">Ausgewaehlter Zeitraum (0=Alle, 1=Heute, etc.)</param>
/// <param name="IsHausSelected">Ob Haeuser selektiert sind</param>
/// <param name="IsGrundstueckSelected">Ob Grundstuecke selektiert sind</param>
/// <param name="IsZwangsversteigerungSelected">Ob Zwangsversteigerungen selektiert sind</param>
public record SaveUserFilterPreferencesRequest(
    List<string> SelectedOrtes,
    int SelectedAgeFilter,
    bool IsHausSelected,
    bool IsGrundstueckSelected,
    bool IsZwangsversteigerungSelected
) : IRequest<SaveUserFilterPreferencesResponse>;

/// <summary>
/// Response nach dem Speichern der Filtereinstellungen
/// </summary>
/// <param name="Success">Ob das Speichern erfolgreich war</param>
public record SaveUserFilterPreferencesResponse(bool Success);
