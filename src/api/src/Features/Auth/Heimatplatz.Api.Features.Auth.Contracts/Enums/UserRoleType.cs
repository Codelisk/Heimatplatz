namespace Heimatplatz.Api.Features.Auth.Contracts.Enums;

/// <summary>
/// Typen von Benutzerrollen im System
/// </summary>
public enum UserRoleType
{
    /// <summary>Kaeufer - kann Immobilien suchen und Anfragen stellen</summary>
    Buyer = 1,

    /// <summary>Verkaeufer - kann Immobilien anbieten und verwalten</summary>
    Seller = 2
}
