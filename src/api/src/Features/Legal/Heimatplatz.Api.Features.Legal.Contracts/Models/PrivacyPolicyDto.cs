namespace Heimatplatz.Api.Features.Legal.Contracts.Models;

/// <summary>
/// Vollstaendige Datenschutzerklaerung mit allen Daten
/// </summary>
public record PrivacyPolicyDto(
    ResponsiblePartyDto ResponsibleParty,
    // Telefon des Verantwortlichen normalisiert fuer href="tel:..." - berechnet, nicht
    // gespeichert (ResponsiblePartyDto ist die Storage-Form und bleibt frei von Ableitungen)
    string? PhoneLink,
    List<LegalSectionDto> Sections,
    string Version,
    DateTimeOffset EffectiveDate,
    DateTimeOffset LastUpdated
);
