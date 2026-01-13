using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Immobilien.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Immobilien.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Immobilien.Handlers;

/// <summary>
/// Handler fuer paginierte Immobilien-Liste mit Filtern
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/immobilien")]
public class GetImmobilienHandler(AppDbContext db)
    : IRequestHandler<GetImmobilienRequest, GetImmobilienResponse>
{
    [MediatorHttpGet("")]
    public async Task<GetImmobilienResponse> Handle(
        GetImmobilienRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var query = db.Set<Immobilie>()
            .Where(i => i.Status == ImmobilienStatus.Aktiv)
            .AsQueryable();

        // Filter anwenden
        if (request.Typ.HasValue)
            query = query.Where(i => i.Typ == request.Typ.Value);

        if (request.MinPreis.HasValue)
            query = query.Where(i => i.Preis >= request.MinPreis.Value);

        if (request.MaxPreis.HasValue)
            query = query.Where(i => i.Preis <= request.MaxPreis.Value);

        if (request.MinWohnflaeche.HasValue)
            query = query.Where(i => i.Wohnflaeche >= request.MinWohnflaeche.Value);

        if (!string.IsNullOrWhiteSpace(request.OrtSuche))
        {
            var suche = request.OrtSuche.ToLower();
            query = query.Where(i =>
                i.Ort.ToLower().Contains(suche) ||
                (i.Bezirk != null && i.Bezirk.ToLower().Contains(suche)) ||
                (i.Region != null && i.Region.ToLower().Contains(suche)));
        }

        // Gesamtanzahl ermitteln
        var gesamtAnzahl = await query.CountAsync(cancellationToken);

        // Sortierung anwenden
        query = request.Sortierung switch
        {
            ImmobilienSortierung.Preis => request.Richtung == SortierRichtung.Aufsteigend
                ? query.OrderBy(i => i.Preis)
                : query.OrderByDescending(i => i.Preis),
            ImmobilienSortierung.Wohnflaeche => request.Richtung == SortierRichtung.Aufsteigend
                ? query.OrderBy(i => i.Wohnflaeche)
                : query.OrderByDescending(i => i.Wohnflaeche),
            ImmobilienSortierung.Ort => request.Richtung == SortierRichtung.Aufsteigend
                ? query.OrderBy(i => i.Ort).ThenBy(i => i.Bezirk)
                : query.OrderByDescending(i => i.Ort).ThenByDescending(i => i.Bezirk),
            // Default: sort by Id (stable for SQLite compatibility)
            _ => request.Richtung == SortierRichtung.Aufsteigend
                ? query.OrderBy(i => i.Id)
                : query.OrderByDescending(i => i.Id)
        };

        // Pagination
        var skip = (request.Seite - 1) * request.SeitenGroesse;
        var eintraege = await query
            .Skip(skip)
            .Take(request.SeitenGroesse)
            .Select(i => new ImmobilieListeDto(
                i.Id,
                i.Titel,
                i.Typ,
                i.Preis,
                i.Waehrung,
                i.Wohnflaeche,
                i.Grundstuecksflaeche,
                i.Ort,
                i.Bezirk,
                i.Bilder
                    .Where(b => b.IstHauptbild)
                    .Select(b => b.Url)
                    .FirstOrDefault(),
                i.ZusatzInfo
            ))
            .ToListAsync(cancellationToken);

        var gesamtSeiten = (int)Math.Ceiling((double)gesamtAnzahl / request.SeitenGroesse);

        return new GetImmobilienResponse(
            eintraege,
            gesamtAnzahl,
            request.Seite,
            request.SeitenGroesse,
            gesamtSeiten
        );
    }
}
