using Heimatplatz.Api.Features.Immobilien.Contracts.Mediator.Requests;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Immobilien.Handlers;

/// <summary>
/// Handler fuer verfuegbare Immobilientypen
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/immobilien")]
public class GetImmobilienTypenHandler
    : IRequestHandler<GetImmobilienTypenRequest, GetImmobilienTypenResponse>
{
    private static readonly IReadOnlyList<ImmobilienTypDto> Typen =
    [
        new(1, nameof(ImmobilienTyp.Haus), "Haus"),
        new(2, nameof(ImmobilienTyp.Grundstueck), "Grundstück"),
        new(3, nameof(ImmobilienTyp.Wohnung), "Wohnung")
    ];

    [MediatorHttpGet("typen")]
    public Task<GetImmobilienTypenResponse> Handle(
        GetImmobilienTypenRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new GetImmobilienTypenResponse(Typen));
    }
}
