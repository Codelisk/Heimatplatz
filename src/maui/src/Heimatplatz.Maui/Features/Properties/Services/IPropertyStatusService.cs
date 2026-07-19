namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Service zur Verwaltung von Favoriten- und Blockiert-Status der Immobilien des Benutzers.
/// Cached den Status lokal und synchronisiert mit der API.
/// </summary>
public interface IPropertyStatusService
{
    /// <summary>
    /// Event wenn sich Favoriten- oder Blockiert-Status aendert
    /// </summary>
    event EventHandler? StatusChanged;

    /// <summary>
    /// Prueft ob eine Immobilie vom aktuellen Benutzer favorisiert ist
    /// </summary>
    bool IsFavorite(Guid propertyId);

    /// <summary>
    /// Prueft ob eine Immobilie vom aktuellen Benutzer blockiert ist
    /// </summary>
    bool IsBlocked(Guid propertyId);

    /// <summary>
    /// Wechselt den Favoriten-Status einer Immobilie
    /// </summary>
    /// <returns>True wenn jetzt favorisiert, false wenn entfavorisiert</returns>
    Task<bool> ToggleFavoriteAsync(Guid propertyId);

    /// <summary>
    /// Wechselt den Blockiert-Status einer Immobilie
    /// </summary>
    /// <returns>True wenn jetzt blockiert, false wenn entblockt</returns>
    Task<bool> ToggleBlockedAsync(Guid propertyId);

    /// <summary>
    /// Zieht den lokalen Status nach, wenn eine Favoriten-Seite den API-Remove selbst
    /// ausgefuehrt hat. Verhindert, dass Detailseiten danach mit einem alten Cache arbeiten.
    /// </summary>
    void NotifyFavoriteRemoved(Guid propertyId);

    /// <summary>
    /// Zieht den lokalen Status nach, wenn eine Blockiert-Seite den API-Remove selbst
    /// ausgefuehrt hat. Verhindert, dass die Startseite danach den falschen Toggle sendet.
    /// </summary>
    void NotifyBlockedRemoved(Guid propertyId);

    /// <summary>
    /// Laedt die Favoriten- und Blockiert-Listen des Benutzers von der API neu
    /// </summary>
    Task RefreshStatusAsync();

    /// <summary>
    /// Stellt sicher, dass der Status geladen ist (ruft RefreshStatusAsync auf falls noch nicht geladen)
    /// </summary>
    Task EnsureLoadedAsync();

    /// <summary>
    /// Loescht den gecachten Status (z.B. bei Logout)
    /// </summary>
    void ClearCache();
}
