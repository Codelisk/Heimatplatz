namespace Heimatplatz.Api.Authorization;

/// <summary>
/// Zentrale Definition aller Authorization Policies.
///
/// Rollenmodell: Jeder authentifizierte Benutzer ist implizit Kaeufer (kann suchen,
/// favorisieren, Filter speichern) - dafuer reicht RequiresAuthorization ohne Policy.
/// Nur "darf inserieren" (Seller) und "System-/Import-Faehigkeiten" (Admin) sind
/// eigene Policies.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Policy: Nur Verkaeufer (User mit gesetztem SellerType)</summary>
    public const string RequireSeller = nameof(RequireSeller);

    /// <summary>Policy: Nur Administratoren (z.B. Batch-Import, Sync-Trigger)</summary>
    public const string RequireAdmin = nameof(RequireAdmin);
}
