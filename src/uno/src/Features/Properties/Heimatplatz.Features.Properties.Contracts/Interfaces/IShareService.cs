namespace Heimatplatz.Features.Properties.Contracts.Interfaces;

/// <summary>
/// Service fuer Share-Operationen (native Share-Dialog)
/// </summary>
public interface IShareService
{
    /// <summary>
    /// Teilt einen Text ueber den nativen Share-Dialog
    /// </summary>
    /// <param name="title">Titel fuer den Share-Dialog</param>
    /// <param name="text">Der zu teilende Text</param>
    /// <returns>True wenn Share-Dialog geoeffnet wurde</returns>
    Task<bool> ShareTextAsync(string title, string text);

    /// <summary>
    /// Teilt einen Link ueber den nativen Share-Dialog
    /// </summary>
    /// <param name="title">Titel fuer den Share-Dialog</param>
    /// <param name="uri">Die zu teilende URL</param>
    /// <param name="description">Optionale Beschreibung</param>
    /// <returns>True wenn Share-Dialog geoeffnet wurde</returns>
    Task<bool> ShareLinkAsync(string title, Uri uri, string? description = null);
}
