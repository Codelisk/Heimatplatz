using Shiny.Mediator;

namespace Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Hochladen von Medien (Fotos und Videos) fuer die KI-gestuetzte Inseratserstellung.
/// Der Client laedt idealerweise eine Datei pro Request hoch, um die Request-Groesse klein zu halten.
/// </summary>
public record UploadListingMediaRequest(
    List<Base64MediaData> Media
) : IRequest<UploadListingMediaResponse>;

/// <summary>
/// Base64-kodierte Mediendaten (Foto oder Video) fuer den Upload.
/// </summary>
public record Base64MediaData(
    string FileName,
    string ContentType,
    string Base64Data
);

/// <summary>
/// Response mit den URLs der hochgeladenen Medien, getrennt nach Bildern und Videos.
/// </summary>
public record UploadListingMediaResponse(
    List<string> ImageUrls,
    List<string> VideoUrls
);
