namespace Heimatplatz.Api.Features.Marketing.Contracts.Models;

/// <summary>Versendete Marketing-E-Mail (Versand-Historie/Kontakt-Timeline).</summary>
public record MarketingEmailDto(
    Guid Id,
    Guid ContactId,
    string? ContactEmail,
    string? ContactName,
    string Subject,
    string Body,
    string? Keywords,
    MarketingEmailStatus Status,
    DateTimeOffset SentAt,
    int ReplyCount
);
