using Heimatplatz.Api;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler for GetPropertyTypesRequest - returns the available property types with
/// German display labels. Single source of truth for type pickers in Web/MAUI:
/// the clients must not hardcode the type list (Backend-First).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class GetPropertyTypesHandler : IRequestHandler<GetPropertyTypesRequest, GetPropertyTypesResponse>
{
    [MediatorHttpGet("/types", OperationId = "GetPropertyTypes")]
    public Task<GetPropertyTypesResponse> Handle(GetPropertyTypesRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var types = Enum.GetValues<PropertyType>()
            .Select(type => new PropertyTypeOptionDto(type.ToString(), GetLabel(type)))
            .ToList();

        return Task.FromResult(new GetPropertyTypesResponse(types));
    }

    private static string GetLabel(PropertyType type) => type switch
    {
        PropertyType.House => "Haus",
        PropertyType.Land => "Grundstück",
        PropertyType.Foreclosure => "Zwangsversteigerung",
        _ => type.ToString()
    };
}
