using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Handlers;

/// <summary>
/// Intern: Anfragen-Liste mit Nutzer-Infos, Filtern (Status/Kategorie/Suche) und Paging,
/// neueste Aktivitaet zuerst.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/feedback")]
public class GetAdminFeedbackTicketsHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetAdminFeedbackTicketsRequest, GetAdminFeedbackTicketsResponse>
{
    [MediatorHttpGet("", OperationId = "GetAdminFeedbackTickets")]
    public async Task<GetAdminFeedbackTicketsResponse> Handle(
        GetAdminFeedbackTicketsRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var tickets = dbContext.Set<FeedbackTicket>().AsNoTracking();

        if (Enum.TryParse<FeedbackTicketStatus>(request.Status, ignoreCase: true, out var status))
            tickets = tickets.Where(t => t.Status == status);

        if (Enum.TryParse<FeedbackCategory>(request.Category, ignoreCase: true, out var category))
            tickets = tickets.Where(t => t.Category == category);

        var query =
            from t in tickets
            join user in dbContext.Set<User>().AsNoTracking() on t.UserId equals user.Id into users
            from u in users.DefaultIfEmpty()
            select new { Ticket = t, User = u };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // ToLower() uebersetzt EF auf beiden Providern zu LOWER() (E-Mail ist bereits
            // normalisiert gespeichert) - gleiches Muster wie die Marketing-Kontaktliste
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Ticket.Subject.ToLower().Contains(search) ||
                (x.User != null && (
                    x.User.Email.Contains(search) ||
                    (x.User.FirstName + " " + x.User.LastName).ToLower().Contains(search))));
        }

        var total = await query.CountAsync(cancellationToken);

        var page = Math.Max(request.Page, 0);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var rows = await query
            .OrderByDescending(x => x.Ticket.LastMessageAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Ticket.Id, x.Ticket.Category, x.Ticket.Subject, x.Ticket.Status,
                x.Ticket.CreatedAt, x.Ticket.LastMessageAt, x.Ticket.HasUnreadForTeam,
                x.Ticket.UserId, x.Ticket.Source, x.Ticket.AppVersion,
                MessageCount = x.Ticket.Messages.Count,
                LastBody = x.Ticket.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Body)
                    .FirstOrDefault(),
                LastHasAttachments = x.Ticket.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Attachments.Count > 0)
                    .FirstOrDefault(),
                UserName = x.User != null ? x.User.FirstName + " " + x.User.LastName : null,
                UserEmail = x.User != null ? x.User.Email : null
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new AdminFeedbackTicketSummaryDto(
                r.Id, r.Category, r.Subject, r.Status, r.CreatedAt, r.LastMessageAt,
                r.HasUnreadForTeam, r.MessageCount,
                FeedbackMapping.BuildPreview(r.LastBody ?? string.Empty, r.LastHasAttachments),
                r.UserId, r.UserName, r.UserEmail, r.Source, r.AppVersion))
            .ToList();

        return new GetAdminFeedbackTicketsResponse(
            items, total, pageSize, page, HasMore: (page + 1) * pageSize < total);
    }
}
