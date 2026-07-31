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
/// Kontakt-Detail fuer die Detailseite: Stammdaten + komplette Timeline
/// (gesendete Mails, Rueckmeldungen und Historien-Eintraege, jeweils neueste zuerst).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class GetMarketingContactDetailHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetMarketingContactDetailRequest, GetMarketingContactDetailResponse>
{
    [MediatorHttpGet("/contacts/detail", OperationId = "GetMarketingContactDetail")]
    public async Task<GetMarketingContactDetailResponse> Handle(GetMarketingContactDetailRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var contact = await dbContext.Set<MarketingContact>()
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new MarketingContactDto(
                c.Id, c.Email, c.Name, c.Salutation, c.Title, c.FirstName, c.LastName,
                c.Company, c.Phone, c.City, c.ContactType, c.Status,
                c.Notes, c.Source, c.FirmenbuchFnr, c.NextFollowUpAt,
                c.LastContactedAt, c.LastReplyAt, c.CreatedAt,
                c.Emails.Count, c.InboundEmails.Count))
            .FirstOrDefaultAsync(cancellationToken);

        if (contact is null)
            return new GetMarketingContactDetailResponse(null, [], [], [], []);

        var emails = await dbContext.Set<MarketingEmail>()
            .AsNoTracking()
            .Where(e => e.ContactId == request.Id)
            .OrderByDescending(e => e.SentAt)
            .Select(e => new MarketingEmailDto(
                e.Id, e.ContactId, e.Contact.Email, e.Contact.Name, e.Subject, e.Body,
                e.Keywords, e.Status, e.SentAt, e.Replies.Count))
            .ToListAsync(cancellationToken);

        var replies = await dbContext.Set<MarketingInboundEmail>()
            .AsNoTracking()
            .Where(i => i.ContactId == request.Id)
            .OrderByDescending(i => i.ReceivedAt)
            .Select(i => new MarketingInboundEmailDto(
                i.Id, i.ContactId, i.Contact != null ? i.Contact.Name : null,
                i.FromAddress, i.FromName, i.Subject, i.BodyText, i.ReceivedAt, i.IsRead,
                i.MarketingEmailId, i.RepliedToEmail != null ? i.RepliedToEmail.Subject : null,
                i.IsBounce))
            .ToListAsync(cancellationToken);

        var activities = await dbContext.Set<MarketingActivity>()
            .AsNoTracking()
            .Where(a => a.ContactId == request.Id)
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => new MarketingActivityDto(
                a.Id, a.ContactId, a.Type, a.Notes,
                a.StatusFrom, a.StatusTo, a.FollowUpAt, a.OccurredAt))
            .ToListAsync(cancellationToken);

        var additionalEmails = await dbContext.Set<MarketingContactEmail>()
            .AsNoTracking()
            .Where(a => a.ContactId == request.Id)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new MarketingContactEmailDto(a.Id, a.Email, a.Source, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return new GetMarketingContactDetailResponse(contact, emails, replies, activities, additionalEmails);
    }
}
