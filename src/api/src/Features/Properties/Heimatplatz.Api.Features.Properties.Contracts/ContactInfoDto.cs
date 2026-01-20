using Heimatplatz.Api.Features.Properties.Contracts.Enums;

namespace Heimatplatz.Api.Features.Properties.Contracts;

/// <summary>
/// Kontaktinformationen zu einer Immobilie
/// </summary>
public record ContactInfoDto(
    Guid Id,
    ContactType Type,
    ContactSource Source,
    string? Name,
    string? Email,
    string? Phone,
    string? OriginalListingUrl,
    string? SourceName,
    int DisplayOrder
);
