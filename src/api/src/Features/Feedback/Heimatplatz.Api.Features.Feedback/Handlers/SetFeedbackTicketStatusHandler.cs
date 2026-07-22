using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Handlers;

/// <summary>Intern: Status einer Anfrage manuell setzen (z.B. InProgress oder Closed).</summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/feedback")]
public class SetFeedbackTicketStatusHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<SetFeedbackTicketStatusRequest, SetFeedbackTicketStatusResponse>
{
    [MediatorHttpPost("/status", OperationId = "SetFeedbackTicketStatus")]
    public async Task<SetFeedbackTicketStatusResponse> Handle(
        SetFeedbackTicketStatusRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var ticket = await dbContext.Set<FeedbackTicket>()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
            return new SetFeedbackTicketStatusResponse(false, "Anfrage nicht gefunden.");

        ticket.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SetFeedbackTicketStatusResponse(true, null);
    }
}
