using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Kennzahlen fuer das Marketing-Dashboard: Kontakt-Funnel, Versand- und
/// Antwort-Volumen, Antwortquote. X-Admin-Key-Schutz wie alle /api/admin-Endpoints.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class GetMarketingStatsHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetMarketingStatsRequest, GetMarketingStatsResponse>
{
    [MediatorHttpGet("/stats", OperationId = "GetMarketingStats")]
    public async Task<GetMarketingStatsResponse> Handle(GetMarketingStatsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var since30Days = DateTimeOffset.UtcNow.AddDays(-30);

        var contacts = dbContext.Set<MarketingContact>().AsNoTracking();
        var emails = dbContext.Set<MarketingEmail>().AsNoTracking();
        var inbound = dbContext.Set<MarketingInboundEmail>().AsNoTracking();

        var byStatus = await contacts
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        int CountOf(MarketingContactStatus status) =>
            byStatus.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

        var emailsSentTotal = await emails.CountAsync(cancellationToken);
        // Bounces sind keine Antworten - weder fuer die Quote noch fuer die Zaehler
        var emailsWithReply = await emails.CountAsync(e => e.Replies.Any(r => !r.IsBounce), cancellationToken);

        var now = DateTimeOffset.UtcNow;

        return new GetMarketingStatsResponse(
            TotalContacts: byStatus.Sum(x => x.Count),
            Leads: CountOf(MarketingContactStatus.Lead),
            ToContact: CountOf(MarketingContactStatus.ToContact),
            // Faellig heisst: Termin erreicht - unabhaengig vom Status, damit auch ein
            // Kontakt mit Status "Interessiert" und offenem Termin auftaucht
            FollowUpDue: await contacts.CountAsync(
                c => c.NextFollowUpAt != null && c.NextFollowUpAt <= now,
                cancellationToken),
            Contacted: CountOf(MarketingContactStatus.Contacted),
            Replied: CountOf(MarketingContactStatus.Replied),
            Interested: CountOf(MarketingContactStatus.Interested),
            Customers: CountOf(MarketingContactStatus.Customer),
            NotInterested: CountOf(MarketingContactStatus.NotInterested),
            EmailsSentTotal: emailsSentTotal,
            EmailsSent30Days: await emails.CountAsync(e => e.SentAt >= since30Days, cancellationToken),
            RepliesTotal: await inbound.CountAsync(i => !i.IsBounce, cancellationToken),
            Replies30Days: await inbound.CountAsync(i => i.ReceivedAt >= since30Days && !i.IsBounce, cancellationToken),
            // Ungelesen inkl. Bounces - auch die sollen auffallen
            UnreadReplies: await inbound.CountAsync(i => !i.IsRead, cancellationToken),
            ReplyRatePercent: emailsSentTotal > 0
                ? (int)Math.Round(100.0 * emailsWithReply / emailsSentTotal)
                : null
        );
    }
}
