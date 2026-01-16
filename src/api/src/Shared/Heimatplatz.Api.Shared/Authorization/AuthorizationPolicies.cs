namespace Heimatplatz.Api.Authorization;

/// <summary>
/// Zentrale Definition aller Authorization Policies
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Policy: Nur Käufer</summary>
    public const string RequireBuyer = nameof(RequireBuyer);

    /// <summary>Policy: Nur Verkäufer</summary>
    public const string RequireSeller = nameof(RequireSeller);

    /// <summary>Policy: Käufer ODER Verkäufer (mindestens eine Rolle)</summary>
    public const string RequireAnyRole = nameof(RequireAnyRole);

    /// <summary>Policy: Käufer UND Verkäufer (beide Rollen)</summary>
    public const string RequireBuyerAndSeller = nameof(RequireBuyerAndSeller);
}
