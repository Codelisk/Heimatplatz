using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Hochladen von Immobilien-Bildern.
/// Die Dateien werden vom Handler aus dem HttpContext gelesen (multipart/form-data).
/// Der Endpoint wird manuell via PropertyImageEndpoints registriert.
/// </summary>
public record UploadPropertyImagesRequest : IRequest<UploadPropertyImagesResponse>;

/// <summary>
/// Response mit den URLs der hochgeladenen Bilder.
/// </summary>
public record UploadPropertyImagesResponse(
    List<string> ImageUrls
);
