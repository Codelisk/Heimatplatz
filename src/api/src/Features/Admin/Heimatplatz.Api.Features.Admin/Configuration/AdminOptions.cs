namespace Heimatplatz.Api.Features.Admin.Configuration;

/// <summary>
/// Konfiguration des Admin-Features (Section "Admin").
/// </summary>
public class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>
    /// Shared-Key fuer die /api/admin-Endpoints (Header X-Admin-Key). Ohne gesetzten
    /// Key sind die Endpoints ausserhalb von Development gesperrt (fail-closed).
    /// </summary>
    public string? ApiKey { get; set; }
}
