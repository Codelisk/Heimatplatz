using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>Kennzahlen fuer das Marketing-Dashboard (Auswertung).</summary>
public record GetMarketingStatsRequest : IRequest<GetMarketingStatsResponse>;

/// <summary>
/// ReplyRatePercent = Anteil versendeter Mails mit mindestens einer zugeordneten
/// Antwort (0-100, gerundet); null solange noch nichts versendet wurde.
/// </summary>
public record GetMarketingStatsResponse(
    int TotalContacts,
    int Leads,
    int Contacted,
    int Replied,
    int Interested,
    int Customers,
    int NotInterested,
    int EmailsSentTotal,
    int EmailsSent30Days,
    int RepliesTotal,
    int Replies30Days,
    int UnreadReplies,
    int? ReplyRatePercent
);
