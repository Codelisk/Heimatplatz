using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Immobilien.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Immobilien.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Immobilien.Handlers;

/// <summary>
/// Handler fuer einzelne Immobilie mit Details
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class GetImmobilieByIdHandler(AppDbContext db)
    : IRequestHandler<GetImmobilieByIdRequest, GetImmobilieByIdResponse>
{
    public async Task<GetImmobilieByIdResponse> Handle(
        GetImmobilieByIdRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var immobilie = await db.Set<Immobilie>()
            .Include(i => i.Bilder.OrderBy(b => b.Reihenfolge))
            .Where(i => i.Id == request.Id)
            .Select(i => new ImmobilieDetailDto(
                i.Id,
                i.Titel,
                i.Beschreibung,
                i.Typ,
                i.Status,
                i.Preis,
                i.Waehrung,
                i.Wohnflaeche,
                i.Grundstuecksflaeche,
                i.Ort,
                i.Bezirk,
                i.Region,
                i.Land,
                i.Breitengrad,
                i.Laengengrad,
                i.Zimmer,
                i.Schlafzimmer,
                i.Badezimmer,
                i.Baujahr,
                i.Bilder
                    .OrderBy(b => b.Reihenfolge)
                    .Select(b => new ImmobilieBildDto(
                        b.Id,
                        b.Url,
                        b.AltText,
                        b.IstHauptbild
                    ))
                    .ToList(),
                i.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);

        return new GetImmobilieByIdResponse(immobilie);
    }
}
