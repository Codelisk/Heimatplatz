using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;

/// <summary>
/// Kompletter Verlauf einer eigenen Anfrage. Das Abrufen markiert ungelesene
/// Team-Antworten als gelesen (HasUnread der Liste erlischt damit).
/// </summary>
public record GetFeedbackTicketRequest(Guid TicketId) : IRequest<GetFeedbackTicketResponse>;

/// <summary>Ticket ist null, wenn es nicht existiert oder nicht dem Nutzer gehoert.</summary>
public record GetFeedbackTicketResponse(FeedbackTicketDetailDto? Ticket);
