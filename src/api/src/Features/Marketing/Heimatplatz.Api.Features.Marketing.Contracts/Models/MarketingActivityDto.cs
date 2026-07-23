namespace Heimatplatz.Api.Features.Marketing.Contracts.Models;

/// <summary>Ein Eintrag der Kontakt-Historie (Anruf, Notiz, Statuswechsel, Wiedervorlage).</summary>
public record MarketingActivityDto(
    Guid Id,
    Guid ContactId,
    MarketingActivityType Type,
    string? Notes,
    MarketingContactStatus? StatusFrom,
    MarketingContactStatus? StatusTo,
    DateTimeOffset? FollowUpAt,
    DateTimeOffset OccurredAt
);
