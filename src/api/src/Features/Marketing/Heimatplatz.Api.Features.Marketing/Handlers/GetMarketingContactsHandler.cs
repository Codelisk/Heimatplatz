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
/// Kontaktliste der Marketing-Kontaktdatenbank: Suche ueber E-Mail/Name/Firma,
/// Filter auf Status/Typ, Paging, neueste zuerst.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class GetMarketingContactsHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetMarketingContactsRequest, GetMarketingContactsResponse>
{
    [MediatorHttpGet("/contacts", OperationId = "GetMarketingContacts")]
    public async Task<GetMarketingContactsResponse> Handle(GetMarketingContactsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var query = dbContext.Set<MarketingContact>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // ToLower() uebersetzt EF auf beiden Providern zu LOWER() - Postgres-LIKE
            // waere sonst case-sensitiv (E-Mail ist bereits normalisiert gespeichert)
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.Email.Contains(search) ||
                (c.Name != null && c.Name.ToLower().Contains(search)) ||
                (c.Company != null && c.Company.ToLower().Contains(search)));
        }

        if (Enum.TryParse<MarketingContactStatus>(request.Status, ignoreCase: true, out var status))
            query = query.Where(c => c.Status == status);

        if (Enum.TryParse<MarketingContactType>(request.ContactType, ignoreCase: true, out var type))
            query = query.Where(c => c.ContactType == type);

        var total = await query.CountAsync(cancellationToken);

        var page = Math.Max(request.Page, 0);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var contacts = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(c => new MarketingContactDto(
                c.Id, c.Email, c.Name, c.Company, c.Phone, c.ContactType, c.Status,
                c.Notes, c.Source, c.LastContactedAt, c.LastReplyAt, c.CreatedAt,
                c.Emails.Count, c.InboundEmails.Count))
            .ToListAsync(cancellationToken);

        return new GetMarketingContactsResponse(
            contacts, total, pageSize, page, HasMore: (page + 1) * pageSize < total);
    }
}
