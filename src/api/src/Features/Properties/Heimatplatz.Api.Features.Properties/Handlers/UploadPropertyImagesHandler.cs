using Heimatplatz.Api;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Services;
using Microsoft.AspNetCore.Http;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler zum Hochladen von Immobilien-Bildern.
/// Liest die Dateien aus dem HttpContext (multipart/form-data).
/// Der HTTP-Endpoint wird manuell registriert (PropertyImageEndpoints),
/// da multipart/form-data nicht vom Shiny Mediator Endpoint Generator unterstuetzt wird.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class UploadPropertyImagesHandler(
    IPropertyImageService imageService,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<UploadPropertyImagesRequest, UploadPropertyImagesResponse>
{
    public async Task<UploadPropertyImagesResponse> Handle(
        UploadPropertyImagesRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar.");

        var files = httpContext.Request.Form.Files;
        if (files.Count == 0)
            throw new ArgumentException("Keine Dateien hochgeladen.");

        var relativePaths = await imageService.SaveImagesAsync(files.ToList(), cancellationToken);

        // Convert relative paths to absolute URLs so frontend Image controls can load them
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
        var absoluteUrls = relativePaths.Select(p => $"{baseUrl}{p}").ToList();

        return new UploadPropertyImagesResponse(absoluteUrls);
    }
}
