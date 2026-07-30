using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Firmenbuch.Services;
using Heimatplatz.Api.Features.Marketing.Configuration;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Firmenpool: aufrechte Firmen, deren Name auf die Immobilienbranche hindeutet - live aus
/// der Firmenpool-API, Heimatplatz haelt keinen eigenen Firmenkatalog mehr. Schlagwort-,
/// Orts- und Ausschlussfilter laufen serverseitig in der Quelle, damit Trefferzahl und
/// Seitengrenzen exakt bleiben; nur der Uebernahme-Status (Kontakt-Verknuepfung ueber die
/// Firmenbuchnummer) kommt aus der eigenen Datenbank dazu.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class GetMarketingLeadPoolHandler(
    AppDbContext dbContext,
    IFirmenpoolApiClient firmenpool,
    IOptions<MarketingOptions> options,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetMarketingLeadPoolRequest, GetMarketingLeadPoolResponse>
{
    [MediatorHttpGet("/lead-pool", OperationId = "GetMarketingLeadPool")]
    public async Task<GetMarketingLeadPoolResponse> Handle(GetMarketingLeadPoolRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        // Uebernommene Firmen samt Kontakt-Status - nur Pool-Uebernahmen tragen eine FNR,
        // die Menge bleibt also klein. Sie dient beidem: dem Ausschlussfilter (OnlyOpen)
        // und der Status-Anzeige je Treffer.
        var uebernommen = await dbContext.Set<MarketingContact>()
            .AsNoTracking()
            .Where(x => x.FirmenbuchFnr != null)
            .Select(x => new { Fnr = x.FirmenbuchFnr!, x.Id, x.Status })
            .ToListAsync(cancellationToken);

        var keywords = options.Value.LeadPool.NameKeywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var page = Math.Max(request.Page, 0);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var result = await firmenpool.GetCompaniesAsync(new FirmenpoolCompanyQuery
        {
            // Firmenpool zaehlt Seiten 1-basiert, dieser Endpoint historisch 0-basiert
            Page = page + 1,
            PageSize = pageSize,
            SearchText = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            Sitz = request.Sitz,
            Status = "aufrecht",
            NameContainsAny = string.Join(',', keywords),
            ExcludeFnrs = request.OnlyOpen && uebernommen.Count > 0
                ? string.Join(',', uebernommen.Select(u => u.Fnr))
                : null
        }, cancellationToken);

        var kontaktByFnr = uebernommen
            .GroupBy(u => u.Fnr, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var leads = result.Items
            .Select(item =>
            {
                kontaktByFnr.TryGetValue(item.Fnr, out var kontakt);
                return new MarketingLeadDto(
                    item.Fnr, item.Name, item.Sitz, item.RechtsformText,
                    kontakt?.Id, kontakt?.Status);
            })
            .ToList();

        return new GetMarketingLeadPoolResponse(
            leads, result.TotalCount, pageSize, page,
            HasMore: (page + 1) * pageSize < result.TotalCount);
    }
}
