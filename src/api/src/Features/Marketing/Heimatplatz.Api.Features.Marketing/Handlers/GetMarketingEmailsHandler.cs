using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>Versand-Historie (neueste zuerst), optional auf einen Kontakt gefiltert.</summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class GetMarketingEmailsHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetMarketingEmailsRequest, GetMarketingEmailsResponse>
{
    [MediatorHttpGet("/emails", OperationId = "GetMarketingEmails")]
    public async Task<GetMarketingEmailsResponse> Handle(GetMarketingEmailsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var query = dbContext.Set<MarketingEmail>().AsNoTracking();

        if (request.ContactId is { } contactId)
            query = query.Where(e => e.ContactId == contactId);

        var total = await query.CountAsync(cancellationToken);

        var page = Math.Max(request.Page, 0);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var emails = await query
            .OrderByDescending(e => e.SentAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(e => new MarketingEmailDto(
                e.Id, e.ContactId, e.Contact.Email, e.Contact.Name, e.Subject, e.Body,
                e.Keywords, e.Status, e.SentAt, e.Replies.Count))
            .ToListAsync(cancellationToken);

        return new GetMarketingEmailsResponse(
            emails, total, pageSize, page, HasMore: (page + 1) * pageSize < total);
    }
}
