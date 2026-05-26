namespace Heimatplatz.Features.Properties.Contracts.Interfaces;

/// <summary>
/// Ergebnis einer Share-Operation
/// </summary>
public enum ShareResult
{
    /// <summary>
    /// Inhalt wurde ueber den nativen Share-Dialog geteilt
    /// </summary>
    SharedNatively,

    /// <summary>
    /// Nativer Share war nicht verfuegbar, Inhalt wurde in die Zwischenablage kopiert
    /// </summary>
    CopiedToClipboard,

    /// <summary>
    /// Share ist fehlgeschlagen
    /// </summary>
    Failed
}

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
    /// <returns>Ergebnis der Share-Operation</returns>
    Task<ShareResult> ShareTextAsync(string title, string text);

    /// <summary>
    /// Teilt einen Link ueber den nativen Share-Dialog
    /// </summary>
    /// <param name="title">Titel fuer den Share-Dialog</param>
    /// <param name="uri">Die zu teilende URL</param>
    /// <param name="description">Optionale Beschreibung</param>
    /// <returns>Ergebnis der Share-Operation</returns>
    Task<ShareResult> ShareLinkAsync(string title, Uri uri, string? description = null);
}
