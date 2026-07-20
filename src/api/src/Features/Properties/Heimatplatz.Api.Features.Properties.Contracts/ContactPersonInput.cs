namespace Heimatplatz.Api.Features.Properties.Contracts;

/// <summary>
/// Optionaler zusaetzlicher Ansprechpartner eines Inserats (Kontakt mit DisplayOrder 1).
/// Der Anbieter-Kontakt (DisplayOrder 0) wird weiterhin serverseitig aus dem Profil
/// abgeleitet und kann vom Client nicht beeinflusst werden.
/// Note: Class mit Default-Properties (kein record) fuer den Shiny Mediator OpenAPI-Generator.
/// </summary>
public class ContactPersonInput
{
    /// <summary>Name des Ansprechpartners (Pflicht)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>E-Mail-Adresse (mindestens E-Mail oder Telefon erforderlich)</summary>
    public string? Email { get; set; }

    /// <summary>Telefonnummer (mindestens E-Mail oder Telefon erforderlich)</summary>
    public string? Phone { get; set; }
}
