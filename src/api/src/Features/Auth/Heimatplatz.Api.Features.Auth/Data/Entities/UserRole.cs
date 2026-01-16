using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Auth.Contracts.Enums;

namespace Heimatplatz.Api.Features.Auth.Data.Entities;

/// <summary>
/// Benutzerrolle - definiert ob ein User Kaeufer, Verkaeufer oder beides ist
/// </summary>
public class UserRole : BaseEntity
{
    /// <summary>Referenz zum Benutzer</summary>
    public Guid UserId { get; set; }

    /// <summary>Navigation Property zum Benutzer</summary>
    public User User { get; set; } = null!;

    /// <summary>Typ der Rolle (Buyer/Seller)</summary>
    public UserRoleType RoleType { get; set; }
}
