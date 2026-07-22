using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Handlers;

/// <summary>
/// "Meine Anfragen" des angemeldeten Nutzers, neueste Aktivitaet zuerst.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/feedback")]
public class GetMyFeedbackTicketsHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetMyFeedbackTicketsRequest, GetMyFeedbackTicketsResponse>
{
    [MediatorHttpGet("", OperationId = "GetMyFeedbackTickets", RequiresAuthorization = true)]
    public async Task<GetMyFeedbackTicketsResponse> Handle(
        GetMyFeedbackTicketsRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var rows = await dbContext.Set<FeedbackTicket>()
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.LastMessageAt)
            .Take(200)
            .Select(t => new
            {
                t.Id, t.Category, t.Subject, t.Status, t.CreatedAt, t.LastMessageAt, t.HasUnreadForUser,
                MessageCount = t.Messages.Count,
                LastBody = t.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Body)
                    .FirstOrDefault(),
                LastHasAttachments = t.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Attachments.Count > 0)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var tickets = rows
            .Select(r => new FeedbackTicketSummaryDto(
                r.Id, r.Category, r.Subject, r.Status, r.CreatedAt, r.LastMessageAt,
                r.HasUnreadForUser, r.MessageCount,
                FeedbackMapping.BuildPreview(r.LastBody ?? string.Empty, r.LastHasAttachments)))
            .ToList();

        var unreadCount = await dbContext.Set<FeedbackTicket>()
            .CountAsync(t => t.UserId == userId && t.HasUnreadForUser, cancellationToken);

        return new GetMyFeedbackTicketsResponse(tickets, unreadCount);
    }
}
