using Heimatplatz.Api.Core.Data.Entities;

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
}
