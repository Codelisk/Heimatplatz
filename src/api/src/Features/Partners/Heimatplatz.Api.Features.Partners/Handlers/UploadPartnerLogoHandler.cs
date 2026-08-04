using Heimatplatz.Api;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Partners.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Services;
using Microsoft.AspNetCore.Http;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Partners.Handlers;

/// <summary>
/// Laedt ein Partner-Logo ueber die bestehende Bild-Pipeline hoch (Original +
/// Display-Variante wie Inserats-Fotos). Liefert die absolute URL fuer das
/// LogoUrl-Feld des Save-Formulars.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/partners")]
public class UploadPartnerLogoHandler(
    IAdminAccessGuard accessGuard,
    IPropertyImageService imageService,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<UploadPartnerLogoRequest, UploadPartnerLogoResponse>
{
    [MediatorHttpPost("/logo", OperationId = "UploadPartnerLogo")]
    public async Task<UploadPartnerLogoResponse> Handle(UploadPartnerLogoRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        if (string.IsNullOrWhiteSpace(request.Image.Base64Data))
            return new UploadPartnerLogoResponse(false, "Es wurde kein Bild übermittelt.", null);

        List<string> relativePaths;
        try
        {
            relativePaths = await imageService.SaveBase64ImagesAsync([request.Image], cancellationToken);
        }
        catch (ArgumentException ex)
        {
            // Ungueltiges Format/zu gross - fachlicher Fehler, kein 500
            return new UploadPartnerLogoResponse(false, ex.Message, null);
        }

        if (relativePaths.Count == 0)
            return new UploadPartnerLogoResponse(false, "Das Bild konnte nicht gespeichert werden.", null);

        // Absolute URL wie UploadPropertyImagesHandler, damit das Logo direkt anzeigbar ist
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar.");

        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
        return new UploadPartnerLogoResponse(true, null, $"{baseUrl}{relativePaths[0]}");
    }
}
