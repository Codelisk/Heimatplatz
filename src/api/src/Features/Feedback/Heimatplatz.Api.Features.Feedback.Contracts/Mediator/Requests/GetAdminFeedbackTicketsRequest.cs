using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;

/// <summary>
/// Intern: Anfragen-Liste mit Filtern und Paging. Status/Category als Enum-NAME-Strings
/// (bewusst string, leere Werte = kein Filter - gleiches Muster wie Marketing-Kontakte).
/// </summary>
public record GetAdminFeedbackTicketsRequest(
    string? Status = null,
    string? Category = null,
    string? Search = null,
    int Page = 0,
    int PageSize = 50
) : IRequest<GetAdminFeedbackTicketsResponse>;

public record GetAdminFeedbackTicketsResponse(
    List<AdminFeedbackTicketSummaryDto> Tickets,
    int Total,
    int PageSize,
    int Page,
    bool HasMore);
