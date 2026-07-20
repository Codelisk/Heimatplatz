namespace Heimatplatz.Api.Features.Marketing.Contracts.Models;

/// <summary>Kontakt der Marketing-Kontaktdatenbank fuer Listen und Detail-Ansicht.</summary>
public record MarketingContactDto(
    Guid Id,
    string Email,
    string? Name,
    string? Company,
    string? Phone,
    MarketingContactType ContactType,
    MarketingContactStatus Status,
    string? Notes,
    string? Source,
    DateTimeOffset? LastContactedAt,
    DateTimeOffset? LastReplyAt,
    DateTimeOffset CreatedAt,
    int EmailCount,
    int ReplyCount
);
