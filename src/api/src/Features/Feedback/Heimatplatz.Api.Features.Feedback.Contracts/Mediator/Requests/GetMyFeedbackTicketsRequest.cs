using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;

/// <summary>Alle Anfragen des angemeldeten Nutzers, neueste Aktivitaet zuerst.</summary>
public record GetMyFeedbackTicketsRequest : IRequest<GetMyFeedbackTicketsResponse>;

public record GetMyFeedbackTicketsResponse(
    List<FeedbackTicketSummaryDto> Tickets,
    int UnreadCount);
