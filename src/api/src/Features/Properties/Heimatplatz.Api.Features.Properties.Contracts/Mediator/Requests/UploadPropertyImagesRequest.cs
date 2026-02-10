using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Hochladen von Immobilien-Bildern als Base64-kodierte Daten.
/// Ermoeglicht die Verwendung des automatisch generierten Shiny Mediator HTTP-Requests.
/// </summary>
public record UploadPropertyImagesRequest(
    List<Base64ImageData> Images
) : IRequest<UploadPropertyImagesResponse>;

/// <summary>
/// Base64-kodierte Bilddaten fuer den Upload.
/// </summary>
public record Base64ImageData(
    string FileName,
    string ContentType,
    string Base64Data
);

/// <summary>
/// Response mit den URLs der hochgeladenen Bilder.
/// </summary>
public record UploadPropertyImagesResponse(
    List<string> ImageUrls
);
