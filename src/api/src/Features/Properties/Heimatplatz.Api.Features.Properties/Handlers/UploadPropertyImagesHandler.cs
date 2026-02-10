using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Services;
using Microsoft.AspNetCore.Http;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler zum Hochladen von Immobilien-Bildern.
/// Akzeptiert Base64-kodierte Bilder im JSON-Body.
/// Der Endpoint wird automatisch via [MediatorHttpPost] generiert.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class UploadPropertyImagesHandler(
    IPropertyImageService imageService,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<UploadPropertyImagesRequest, UploadPropertyImagesResponse>
{
    [MediatorHttpPost("/images", OperationId = "UploadPropertyImages", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<UploadPropertyImagesResponse> Handle(
        UploadPropertyImagesRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        if (request.Images.Count == 0)
            throw new ArgumentException("Keine Bilder hochgeladen.");

        var relativePaths = await imageService.SaveBase64ImagesAsync(request.Images, cancellationToken);

        // Convert relative paths to absolute URLs so frontend Image controls can load them
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar.");

        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
        var absoluteUrls = relativePaths.Select(p => $"{baseUrl}{p}").ToList();

        return new UploadPropertyImagesResponse(absoluteUrls);
    }
}
