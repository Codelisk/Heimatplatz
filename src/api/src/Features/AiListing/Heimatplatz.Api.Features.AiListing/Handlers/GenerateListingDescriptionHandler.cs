using Heimatplatz.Api;
using Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.AiListing.Services;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.AiListing.Handlers;

/// <summary>
/// In-process Handler (bewusst OHNE MediatorHttp-Attribute, also kein HTTP-Endpoint):
/// delegiert die Beschreibungs-Generierung an den konfigurierten Provider
/// (Mock/AiConnector, siehe AddAiListingFeature).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class GenerateListingDescriptionHandler(
    IListingDescriptionService descriptionService
) : IRequestHandler<GenerateListingDescriptionRequest, GenerateListingDescriptionResponse>
{
    public async Task<GenerateListingDescriptionResponse> Handle(
        GenerateListingDescriptionRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var description = await descriptionService.GenerateAsync(request, cancellationToken);
        return new GenerateListingDescriptionResponse(description);
    }
}
