using System.Linq.Expressions;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Firmenbuch.Data.Entities;
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
/// Firmenpool: aufrechte Firmen aus dem Firmenbuch-Katalog, deren Name auf die
/// Immobilienbranche hindeutet. Zeigt pro Firma, ob sie bereits als Kontakt uebernommen
/// wurde (Verknuepfung ueber die Firmenbuchnummer).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class GetMarketingLeadPoolHandler(
    AppDbContext dbContext,
    IOptions<MarketingOptions> options,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetMarketingLeadPoolRequest, GetMarketingLeadPoolResponse>
{
    [MediatorHttpGet("/lead-pool", OperationId = "GetMarketingLeadPool")]
    public async Task<GetMarketingLeadPoolResponse> Handle(GetMarketingLeadPoolRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        // Status == null bedeutet im Katalog "aufrecht" - geloeschte Firmen sind als
        // Kontakt wertlos und bleiben komplett aussen vor.
        var query = dbContext.Set<FirmenbuchCompany>()
            .AsNoTracking()
            .Where(c => c.Status == null)
            .Where(BranchNameFilter());

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(c => c.Name.ToLower().Contains(search) || c.Fnr == search);
        }

        if (!string.IsNullOrWhiteSpace(request.Sitz))
        {
            query = query.Where(SitzFilter(request.Sitz));
        }

        // Uebernommene Firmen haengen ueber die Firmenbuchnummer am Kontakt
        var taken = dbContext.Set<MarketingContact>()
            .AsNoTracking()
            .Where(x => x.FirmenbuchFnr != null);

        if (request.OnlyOpen)
            query = query.Where(c => !taken.Any(x => x.FirmenbuchFnr == c.Fnr));

        var total = await query.CountAsync(cancellationToken);

        var page = Math.Max(request.Page, 0);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        // Die Uebernahme-Info (Kontakt-Id + Status) in EINER korrelierten Unterabfrage holen,
        // nicht je Feld eine eigene
        var rows = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Fnr,
                c.Name,
                c.Sitz,
                c.RechtsformText,
                Taken = taken
                    .Where(x => x.FirmenbuchFnr == c.Fnr)
                    .Select(x => new { x.Id, x.Status })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var leads = rows
            .Select(r => new MarketingLeadDto(
                r.Id, r.Fnr, r.Name, r.Sitz, r.RechtsformText,
                r.Taken?.Id,
                r.Taken is null ? null : r.Taken.Status))
            .ToList();

        return new GetMarketingLeadPoolResponse(
            leads, total, pageSize, page, HasMore: (page + 1) * pageSize < total);
    }

    /// <summary>
    /// Namensfilter auf die Immobilienbranche: "Name enthaelt eines der Schlagworte".
    /// Das Firmenbuch fuehrt keine Branche, deshalb ist der Name die einzige Handhabe.
    /// Die OR-Kette wird als Ausdrucksbaum gebaut, weil EF eine lokale Liste in
    /// keywords.Any(k => name.Contains(k)) nicht nach SQL uebersetzt.
    /// Leere Schlagwortliste = kein Filter.
    /// </summary>
    private Expression<Func<FirmenbuchCompany, bool>> BranchNameFilter()
    {
        var keywords = options.Value.LeadPool.NameKeywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (keywords.Count == 0)
            return _ => true;

        var company = Expression.Parameter(typeof(FirmenbuchCompany), "c");
        var lowerName = Expression.Call(
            Expression.Property(company, nameof(FirmenbuchCompany.Name)),
            typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!);
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

        var body = keywords
            .Select(k => (Expression)Expression.Call(lowerName, contains, Expression.Constant(k)))
            .Aggregate(Expression.OrElse);

        return Expression.Lambda<Func<FirmenbuchCompany, bool>>(body, company);
    }

    /// <summary>
    /// Der Astro-OrtPicker kann wie auf der Startseite mehrere Gemeinden liefern.
    /// Die Werte kommen kommasepariert an und werden als ODER-Filter auf den
    /// Firmenbuch-Sitz angewendet. Ein einzelner Wert bleibt abwaertskompatibel.
    /// </summary>
    private static Expression<Func<FirmenbuchCompany, bool>> SitzFilter(string rawSitz)
    {
        var sitze = rawSitz
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .Distinct()
            .ToList();

        if (sitze.Count == 0)
            return _ => true;

        var company = Expression.Parameter(typeof(FirmenbuchCompany), "c");
        var sitzProperty = Expression.Property(company, nameof(FirmenbuchCompany.Sitz));
        var hasSitz = Expression.NotEqual(sitzProperty, Expression.Constant(null, typeof(string)));
        var lowerSitz = Expression.Call(
            sitzProperty,
            typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!);
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

        var matchesAnySitz = sitze
            .Select(sitz => (Expression)Expression.Call(lowerSitz, contains, Expression.Constant(sitz)))
            .Aggregate(Expression.OrElse);

        return Expression.Lambda<Func<FirmenbuchCompany, bool>>(
            Expression.AndAlso(hasSitz, matchesAnySitz),
            company);
    }
}
