namespace Heimatplatz.Core.DeepLink.Services;

/// <summary>
/// Service fuer das Handling von Deep Links
/// </summary>
public interface IDeepLinkService
{
    /// <summary>
    /// Verarbeitet einen Deep Link und navigiert zur entsprechenden Seite
    /// </summary>
    /// <param name="uri">Die Deep Link URI</param>
    /// <returns>True wenn der Link erfolgreich verarbeitet wurde</returns>
    Task<bool> HandleDeepLinkAsync(Uri uri);

    /// <summary>
    /// Prueft ob die URI vom DeepLinkService verarbeitet werden kann
    /// </summary>
    /// <param name="uri">Die zu pruefende URI</param>
    /// <returns>True wenn die URI verarbeitet werden kann</returns>
    bool CanHandleUri(Uri uri);
}
