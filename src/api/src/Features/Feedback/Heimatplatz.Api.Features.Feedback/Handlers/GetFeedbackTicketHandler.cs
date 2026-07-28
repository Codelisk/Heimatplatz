using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Exceptions;
using Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Handlers;

/// <summary>
/// Kompletter Verlauf einer eigenen Anfrage (chronologisch). Das Abrufen markiert
/// ungelesene Team-Antworten als gelesen.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/feedback")]
public class GetFeedbackTicketHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IRequestHandler<GetFeedbackTicketRequest, GetFeedbackTicketResponse>
{
    [MediatorHttpGet("/{TicketId}", OperationId = "GetFeedbackTicketDetail", RequiresAuthorization = true)]
    public async Task<GetFeedbackTicketResponse> Handle(
        GetFeedbackTicketRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        // Tracked, weil das Abrufen das Unread-Flag zuruecksetzt
        var ticket = await dbContext.Set<FeedbackTicket>()
            .Include(t => t.Messages)
            .ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.UserId == userId, cancellationToken);

        // 404 statt 200+null (WEB-027): unbekannte oder fremde Tickets sind semantisch
        // "nicht gefunden" - die Clients behandeln den Status explizit
        if (ticket == null)
            throw new NotFoundException("Anfrage wurde nicht gefunden");

        var baseUrl = FeedbackMapping.GetBaseUrl(httpContextAccessor, configuration);
        var dto = new FeedbackTicketDetailDto(
            ticket.Id,
            ticket.Category,
            ticket.Subject,
            ticket.Status,
            ticket.CreatedAt,
            ticket.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => FeedbackMapping.ToDto(m, baseUrl))
                .ToList());

        if (ticket.HasUnreadForUser)
        {
            ticket.HasUnreadForUser = false;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new GetFeedbackTicketResponse(dto);
    }
}
