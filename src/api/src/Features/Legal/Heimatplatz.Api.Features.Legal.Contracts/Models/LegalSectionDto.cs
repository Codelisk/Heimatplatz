namespace Heimatplatz.Api.Features.Legal.Contracts.Models;

/// <summary>
/// Ein Abschnitt in einem rechtlichen Dokument (z.B. Datenschutzerklaerung)
/// </summary>
public record LegalSectionDto(
    int SortOrder,
    string Title,
    string Content,
    bool IsVisible = true
);
