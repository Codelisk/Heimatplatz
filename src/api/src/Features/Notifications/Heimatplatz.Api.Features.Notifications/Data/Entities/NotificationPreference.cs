using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Notifications.Contracts;

namespace Heimatplatz.Api.Features.Notifications.Data.Entities;

/// <summary>
/// Speichert die Notification-Einstellungen eines Benutzers.
/// Eine Zeile pro User mit FilterMode und optionalen Custom-Filter-Feldern.
/// </summary>
public class NotificationPreference : BaseEntity
{
    /// <summary>User ID this preference belongs to</summary>
    public required Guid UserId { get; set; }

    /// <summary>Der gewaehlte Filter-Modus (All, SameAsSearch, Custom)</summary>
    public NotificationFilterMode FilterMode { get; set; } = NotificationFilterMode.All;

    /// <summary>Ob Notifications generell aktiviert sind</summary>
    public bool IsEnabled { get; set; } = true;

    // --- Custom-Filter-Felder (nur relevant wenn FilterMode == Custom) ---

    /// <summary>Ausgewaehlte Orte als JSON-Array (z.B. ["Linz", "Traun"])</summary>
    public string SelectedLocationsJson { get; set; } = "[]";

    /// <summary>Ob Haeuser im Filter selektiert sind</summary>
    public bool IsHausSelected { get; set; } = true;

    /// <summary>Ob Grundstuecke im Filter selektiert sind</summary>
    public bool IsGrundstueckSelected { get; set; } = true;

    /// <summary>Ob Zwangsversteigerungen im Filter selektiert sind</summary>
    public bool IsZwangsversteigerungSelected { get; set; } = true;

    /// <summary>Ob private Anbieter im Filter selektiert sind</summary>
    public bool IsPrivateSelected { get; set; } = true;

    /// <summary>Ob Makler im Filter selektiert sind</summary>
    public bool IsBrokerSelected { get; set; } = true;

    /// <summary>Ob Portal-Quellen im Filter selektiert sind</summary>
    public bool IsPortalSelected { get; set; } = true;

    /// <summary>JSON-Array von ausgeschlossenen SellerSource-IDs</summary>
    public string ExcludedSellerSourceIdsJson { get; set; } = "[]";

    /// <summary>Navigation property to User</summary>
    public User User { get; set; } = null!;
}
