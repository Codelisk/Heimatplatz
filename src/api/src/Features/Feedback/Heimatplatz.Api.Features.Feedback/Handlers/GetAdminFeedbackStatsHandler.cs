using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Handlers;

/// <summary>Intern: Kennzahlen fuer die Dashboard-Karte des Feedback-Bereichs.</summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/feedback")]
public class GetAdminFeedbackStatsHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetAdminFeedbackStatsRequest, GetAdminFeedbackStatsResponse>
{
    [MediatorHttpGet("/stats", OperationId = "GetAdminFeedbackStats")]
    public async Task<GetAdminFeedbackStatsResponse> Handle(
        GetAdminFeedbackStatsRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var tickets = dbContext.Set<FeedbackTicket>().AsNoTracking();

        var total = await tickets.CountAsync(cancellationToken);
        var open = await tickets.CountAsync(t => t.Status == FeedbackTicketStatus.Open, cancellationToken);
        var inProgress = await tickets.CountAsync(t => t.Status == FeedbackTicketStatus.InProgress, cancellationToken);
        var unread = await tickets.CountAsync(t => t.HasUnreadForTeam, cancellationToken);

        return new GetAdminFeedbackStatsResponse(total, open, inProgress, unread);
    }
}
