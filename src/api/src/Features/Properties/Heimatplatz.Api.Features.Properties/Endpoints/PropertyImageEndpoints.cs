using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Endpoints;

/// <summary>
/// Manueller Minimal API Endpoint fuer Bild-Uploads.
/// Notwendig weil Shiny Mediator auto-generierte Endpoints kein multipart/form-data unterstuetzen.
/// Der Request wird trotzdem durch die Mediator-Pipeline verarbeitet.
/// </summary>
public static class PropertyImageEndpoints
{
    public static IEndpointRouteBuilder MapPropertyImageEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/properties/images", async (IMediator mediator, CancellationToken ct) =>
        {
            var (_, result) = await mediator.Request(new UploadPropertyImagesRequest(), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization(AuthorizationPolicies.RequireSeller)
        .DisableAntiforgery()
        .WithName("UploadPropertyImages")
        .WithTags("Properties")
        .Produces<UploadPropertyImagesResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        return builder;
    }
}
