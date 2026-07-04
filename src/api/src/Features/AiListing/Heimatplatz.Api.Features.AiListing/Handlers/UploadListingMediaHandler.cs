using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Features.AiListing.Configuration;
using Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.AiListing.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.AiListing.Handlers;

/// <summary>
/// Handler zum Hochladen von Inserats-Medien (Fotos und Videos) als Base64.
/// Der Client laedt idealerweise eine Datei pro Request hoch (kleine Request-Bodies).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/ai-listings")]
public class UploadListingMediaHandler(
    IListingMediaService mediaService,
    IOptions<AiListingOptions> options,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<UploadListingMediaRequest, UploadListingMediaResponse>
{
    [MediatorHttpPost("/media", OperationId = "UploadListingMedia", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<UploadListingMediaResponse> Handle(
        UploadListingMediaRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        if (request.Media.Count == 0)
            throw new ArgumentException("Keine Medien hochgeladen.");

        var opts = options.Value;
        if (request.Media.Count > opts.MaxImages + opts.MaxVideos)
            throw new ArgumentException($"Zu viele Dateien in einem Request (max. {opts.MaxImages + opts.MaxVideos}).");

        var (imageUrls, videoUrls) = await mediaService.SaveMediaAsync(request.Media, cancellationToken);

        // Relative Pfade in absolute URLs umwandeln (gleiches Muster wie UploadPropertyImagesHandler)
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar.");

        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        return new UploadListingMediaResponse(
            imageUrls.Select(p => $"{baseUrl}{p}").ToList(),
            videoUrls.Select(p => $"{baseUrl}{p}").ToList()
        );
    }
}
