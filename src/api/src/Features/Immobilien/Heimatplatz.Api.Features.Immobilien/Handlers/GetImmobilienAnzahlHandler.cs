using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Immobilien.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Immobilien.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Immobilien.Handlers;

/// <summary>
/// Handler fuer Immobilien-Anzahl (optimierte COUNT-Query)
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/immobilien")]
public class GetImmobilienAnzahlHandler(AppDbContext db)
    : IRequestHandler<GetImmobilienAnzahlRequest, GetImmobilienAnzahlResponse>
{
    [MediatorHttpGet("anzahl")]
    public async Task<GetImmobilienAnzahlResponse> Handle(
        GetImmobilienAnzahlRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var query = db.Set<Immobilie>()
            .Where(i => i.Status == request.Status)
            .AsQueryable();

        if (request.Typ.HasValue)
            query = query.Where(i => i.Typ == request.Typ.Value);

        var anzahl = await query.CountAsync(cancellationToken);

        return new GetImmobilienAnzahlResponse(anzahl);
    }
}
