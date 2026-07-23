namespace Heimatplatz.Api.Features.Marketing.Contracts.Models;

/// <summary>
/// Kontakt der Marketing-Kontaktdatenbank fuer Listen und Detail-Ansicht.
/// <see cref="Email"/> ist optional: Kontakte aus dem Firmenpool entstehen ohne Adresse
/// (Firmenbuch fuehrt keine Kontaktdaten) und werden erst beim Telefonat ergaenzt.
/// </summary>
public record MarketingContactDto(
    Guid Id,
    string? Email,
    string? Name,
    string? Company,
    string? Phone,
    string? City,
    MarketingContactType ContactType,
    MarketingContactStatus Status,
    string? Notes,
    string? Source,
    string? FirmenbuchFnr,
    DateTimeOffset? NextFollowUpAt,
    DateTimeOffset? LastContactedAt,
    DateTimeOffset? LastReplyAt,
    DateTimeOffset CreatedAt,
    int EmailCount,
    int ReplyCount
);
