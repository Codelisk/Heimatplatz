using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;

/// <summary>
/// Intern: kompletter Verlauf einer Anfrage. Das Abrufen markiert neue
/// Nutzer-Nachrichten als vom Team gelesen.
/// </summary>
public record GetAdminFeedbackTicketDetailRequest(Guid Id) : IRequest<GetAdminFeedbackTicketDetailResponse>;

public record GetAdminFeedbackTicketDetailResponse(AdminFeedbackTicketDetailDto? Ticket);
