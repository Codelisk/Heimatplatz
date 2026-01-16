using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler fuer GetPropertiesRequest - gibt gefilterte Immobilien-Liste zurueck
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class GetPropertiesHandler(AppDbContext dbContext) : IRequestHandler<GetPropertiesRequest, GetPropertiesResponse>
{
    [MediatorHttpGet("/api/properties", OperationId = "GetProperties", AuthorizationPolicies = [AuthorizationPolicies.RequireAnyRole])]
    public async Task<GetPropertiesResponse> Handle(GetPropertiesRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Property>().AsQueryable();

        // Filter anwenden
        if (request.Typ.HasValue)
            query = query.Where(p => p.Typ == request.Typ.Value);

        if (request.AnbieterTyp.HasValue)
            query = query.Where(p => p.AnbieterTyp == request.AnbieterTyp.Value);

        if (request.PreisMin.HasValue)
            query = query.Where(p => p.Preis >= request.PreisMin.Value);

        if (request.PreisMax.HasValue)
            query = query.Where(p => p.Preis <= request.PreisMax.Value);

        if (request.FlaecheMin.HasValue)
            query = query.Where(p => (p.WohnflaecheM2 ?? p.GrundstuecksflaecheM2) >= request.FlaecheMin.Value);

        if (request.FlaecheMax.HasValue)
            query = query.Where(p => (p.WohnflaecheM2 ?? p.GrundstuecksflaecheM2) <= request.FlaecheMax.Value);

        if (request.ZimmerMin.HasValue)
            query = query.Where(p => p.Zimmer >= request.ZimmerMin.Value);

        if (!string.IsNullOrWhiteSpace(request.Ort))
            query = query.Where(p => p.Ort.Contains(request.Ort) || p.Plz.StartsWith(request.Ort));

        // Gesamtanzahl fuer Paging
        var gesamt = await query.CountAsync(cancellationToken);

        // Laden, Sortierung im Speicher (SQLite unterstuetzt DateTimeOffset nicht in ORDER BY), dann Paging
        var entities = await query.ToListAsync(cancellationToken);
        var immobilien = entities
            .OrderByDescending(p => p.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(p => new PropertyListItemDto(
                p.Id,
                p.Titel,
                p.Adresse,
                p.Ort,
                p.Preis,
                p.WohnflaecheM2,
                p.GrundstuecksflaecheM2,
                p.Zimmer,
                p.Typ,
                p.AnbieterTyp,
                p.AnbieterName,
                p.BildUrls
            ))
            .ToList();

        return new GetPropertiesResponse(immobilien, gesamt);
    }
}
