using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Handlers;

/// <summary>
/// Nutzer benennt den Auto-Titel einer eigenen Anfrage um (Liste "Meine Anfragen").
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/feedback")]
public class RenameFeedbackTicketHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<RenameFeedbackTicketRequest, RenameFeedbackTicketResponse>
{
    [MediatorHttpPost("/subject", OperationId = "RenameFeedbackTicket", RequiresAuthorization = true)]
    public async Task<RenameFeedbackTicketResponse> Handle(
        RenameFeedbackTicketRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var ticket = await dbContext.Set<FeedbackTicket>()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.UserId == userId, cancellationToken);

        if (ticket == null)
            return new RenameFeedbackTicketResponse(false, "Anfrage nicht gefunden.");

        var subject = request.Subject?.Trim();
        if (string.IsNullOrEmpty(subject))
            return new RenameFeedbackTicketResponse(false, "Titel darf nicht leer sein.");

        ticket.Subject = FeedbackMapping.Truncate(subject, 200);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RenameFeedbackTicketResponse(true, null);
    }
}
