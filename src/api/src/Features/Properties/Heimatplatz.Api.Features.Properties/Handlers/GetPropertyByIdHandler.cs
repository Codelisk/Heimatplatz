using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler fuer GetPropertyByIdRequest - gibt einzelne Immobilie zurueck
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class GetPropertyByIdHandler(AppDbContext dbContext) : IRequestHandler<GetPropertyByIdRequest, GetPropertyByIdResponse>
{
    [MediatorHttpGet("/api/properties/{Id}", OperationId = "GetPropertyById")]
    public async Task<GetPropertyByIdResponse> Handle(GetPropertyByIdRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var property = await dbContext.Set<Property>()
            .Where(p => p.Id == request.Id)
            .Select(p => new PropertyDto(
                p.Id,
                p.Titel,
                p.Adresse,
                p.Ort,
                p.Plz,
                p.Preis,
                p.WohnflaecheM2,
                p.GrundstuecksflaecheM2,
                p.Zimmer,
                p.Baujahr,
                p.Typ,
                p.AnbieterTyp,
                p.AnbieterName,
                p.Beschreibung,
                p.Ausstattung,
                p.BildUrls,
                p.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);

        return new GetPropertyByIdResponse(property);
    }
}
