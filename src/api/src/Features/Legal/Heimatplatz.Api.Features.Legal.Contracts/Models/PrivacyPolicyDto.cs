namespace Heimatplatz.Api.Features.Legal.Contracts.Models;

/// <summary>
/// Vollstaendige Datenschutzerklaerung mit allen Daten
/// </summary>
public record PrivacyPolicyDto(
    ResponsiblePartyDto ResponsibleParty,
    List<LegalSectionDto> Sections,
    string Version,
    DateTimeOffset EffectiveDate,
    DateTimeOffset LastUpdated
);
