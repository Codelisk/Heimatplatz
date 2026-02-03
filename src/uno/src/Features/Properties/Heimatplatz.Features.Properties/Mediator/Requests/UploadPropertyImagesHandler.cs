using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Mediator.Requests;
using Shiny.Mediator;

namespace Heimatplatz.Features.Properties.Mediator.Requests;

/// <summary>
/// Mediator Handler fuer Bild-Uploads.
/// Delegiert an IImageUploadService fuer den eigentlichen HTTP-Upload.
/// </summary>
public class UploadPropertyImagesHandler(
    IImageUploadService imageUploadService
) : IRequestHandler<UploadPropertyImagesRequest, UploadPropertyImagesResponse>
{
    public async Task<UploadPropertyImagesResponse> Handle(
        UploadPropertyImagesRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var urls = await imageUploadService.UploadImagesAsync(request.Files, cancellationToken);
        return new UploadPropertyImagesResponse(urls);
    }
}
