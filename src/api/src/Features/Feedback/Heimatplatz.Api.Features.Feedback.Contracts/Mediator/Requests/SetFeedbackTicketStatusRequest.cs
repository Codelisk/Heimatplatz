using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;

/// <summary>Intern: Status einer Anfrage manuell setzen (z.B. InProgress oder Closed).</summary>
public record SetFeedbackTicketStatusRequest(
    Guid TicketId,
    FeedbackTicketStatus Status
) : IRequest<SetFeedbackTicketStatusResponse>;

public record SetFeedbackTicketStatusResponse(bool Success, string? Error);
