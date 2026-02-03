using Shiny.Mediator;

namespace Heimatplatz.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Mediator Request zum Hochladen von Immobilien-Bildern.
/// Wird vom UploadPropertyImagesHandler verarbeitet, der den multipart/form-data Upload durchfuehrt.
/// </summary>
public record UploadPropertyImagesRequest(
    IReadOnlyList<ImageFileData> Files
) : IRequest<UploadPropertyImagesResponse>;

/// <summary>
/// Dateidaten fuer den Upload (plattformunabhaengig).
/// </summary>
public record ImageFileData(
    string FileName,
    string ContentType,
    Stream Stream
);

/// <summary>
/// Response mit den URLs der hochgeladenen Bilder.
/// </summary>
public record UploadPropertyImagesResponse(
    List<string> ImageUrls
);
