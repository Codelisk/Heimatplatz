using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Heimatplatz.Api.Features.Marketing.Services;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Posteingang (Rueckmeldungen). Vor dem Lesen laeuft ein gedrosselter IMAP-Auto-Sync
/// (max. alle 5 Minuten) - die Seite ist beim Oeffnen aktuell, ohne bei jedem Reload
/// das Postfach zu belasten. Sync-Fehler brechen das Lesen nicht ab, sondern kommen
/// als SyncError zur Anzeige mit.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class GetMarketingInboxHandler(
    AppDbContext dbContext,
    IMarketingInboxSync inboxSync,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetMarketingInboxRequest, GetMarketingInboxResponse>
{
    [MediatorHttpGet("/inbox", OperationId = "GetMarketingInbox")]
    public async Task<GetMarketingInboxResponse> Handle(GetMarketingInboxRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        string? syncError = null;
        if (inboxSync.IsConfigured)
        {
            var syncResult = await inboxSync.SyncAsync(force: false, cancellationToken);
            if (!syncResult.Success && !syncResult.Skipped)
                syncError = syncResult.Error;
        }

        var query = dbContext.Set<MarketingInboundEmail>().AsNoTracking();

        if (request.UnreadOnly)
            query = query.Where(i => !i.IsRead);

        var total = await query.CountAsync(cancellationToken);

        var page = Math.Max(request.Page, 0);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var items = await query
            .OrderByDescending(i => i.ReceivedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(i => new MarketingInboundEmailDto(
                i.Id, i.ContactId, i.Contact != null ? i.Contact.Name : null,
                i.FromAddress, i.FromName, i.Subject, i.BodyText, i.ReceivedAt, i.IsRead,
                i.MarketingEmailId, i.RepliedToEmail != null ? i.RepliedToEmail.Subject : null))
            .ToListAsync(cancellationToken);

        return new GetMarketingInboxResponse(
            items, total, pageSize, page, HasMore: (page + 1) * pageSize < total,
            ImapConfigured: inboxSync.IsConfigured,
            SyncError: syncError);
    }
}
