namespace Heimatplatz.Api.Features.Marketing.Contracts.Models;

/// <summary>Eingegangene Rueckmeldung (Marketing-Posteingang/Kontakt-Timeline).</summary>
public record MarketingInboundEmailDto(
    Guid Id,
    Guid? ContactId,
    string? ContactName,
    string FromAddress,
    string? FromName,
    string? Subject,
    string? BodyText,
    DateTimeOffset ReceivedAt,
    bool IsRead,
    Guid? RepliedToEmailId,
    string? RepliedToSubject,
    bool IsBounce
);
