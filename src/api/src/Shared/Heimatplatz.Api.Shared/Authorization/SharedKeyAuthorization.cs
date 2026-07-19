using System.Security.Cryptography;
using System.Text;

namespace Heimatplatz.Api.Authorization;

/// <summary>
/// Timing-sicherer Shared-Key-Vergleich fuer administrative Endpoints ohne echten
/// Admin-Account (Prod hat keinen) - genutzt vom Edikte-Sync-Trigger (X-Sync-Key,
/// TriggerForeclosureAuctionSyncHandler) und den /api/admin-Endpoints (X-Admin-Key,
/// AdminAccessGuard).
/// </summary>
public static class SharedKeyAuthorization
{
    /// <summary>
    /// Ohne konfigurierten <paramref name="expectedKey"/> ist der Zugang ausserhalb von
    /// Development gesperrt (fail-closed). FixedTimeEquals verhindert Timing-Angriffe auf
    /// den Key-Vergleich.
    /// </summary>
    public static bool IsAuthorized(string? expectedKey, string? providedKey, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(expectedKey))
            return isDevelopment;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedKey ?? string.Empty),
            Encoding.UTF8.GetBytes(expectedKey));
    }
}
