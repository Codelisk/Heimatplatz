using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;

namespace Heimatplatz.Api.Features.Auth.Data.Entities;

/// <summary>
/// Benutzer-Entity
/// </summary>
public class User : BaseEntity
{
    /// <summary>Vorname des Benutzers</summary>
    public required string Vorname { get; set; }

    /// <summary>Nachname des Benutzers</summary>
    public required string Nachname { get; set; }

    /// <summary>E-Mail-Adresse (eindeutig)</summary>
    public required string Email { get; set; }

    /// <summary>Gehashtes Passwort</summary>
    public required string PasswordHash { get; set; }

    /// <summary>Vollstaendiger Name</summary>
    public string FullName => $"{Vorname} {Nachname}";

    /// <summary>Rollen des Benutzers (Kaeufer, Verkaeufer)</summary>
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();

    /// <summary>Verkaeufertyp (nur bei Seller-Rolle)</summary>
    public SellerType? SellerType { get; set; }

    /// <summary>Firmenname (nur bei Broker)</summary>
    public string? CompanyName { get; set; }
}
