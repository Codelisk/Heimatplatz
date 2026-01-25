using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Auth.Data.Entities;

/// <summary>
/// Entity fuer die Filtereinstellungen eines Benutzers.
/// Speichert die Default-Filter die auf der HomePage angewendet werden.
/// </summary>
public class UserFilterPreferences : BaseEntity
{
    /// <summary>
    /// Referenz zum User
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Navigation Property zum User
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Ausgewaehlte Orte als JSON-Array (z.B. ["Linz", "Traun", "Leonding"])
    /// </summary>
    public string SelectedOrtesJson { get; set; } = "[]";

    /// <summary>
    /// Ausgewaehlter Zeitraum-Filter (0=Alle, 1=Heute, 2=Diese Woche, etc.)
    /// </summary>
    public int SelectedAgeFilter { get; set; } = 0;

    /// <summary>
    /// Ob Haeuser im Filter selektiert sind
    /// </summary>
    public bool IsHausSelected { get; set; } = true;

    /// <summary>
    /// Ob Grundstuecke im Filter selektiert sind
    /// </summary>
    public bool IsGrundstueckSelected { get; set; } = true;

    /// <summary>
    /// Ob Zwangsversteigerungen im Filter selektiert sind
    /// </summary>
    public bool IsZwangsversteigerungSelected { get; set; } = true;
}
