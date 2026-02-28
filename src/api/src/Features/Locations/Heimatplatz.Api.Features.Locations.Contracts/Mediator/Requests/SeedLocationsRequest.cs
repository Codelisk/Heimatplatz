using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Locations.Contracts.Mediator.Requests;

public record SeedLocationsRequest : IRequest<SeedLocationsResponse>;

public record SeedLocationsResponse(
    int FederalProvinces,
    int Districts,
    int Municipalities,
    string? Error = null
);
