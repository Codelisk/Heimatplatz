namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// Definiert den Anzeigemodus der PropertyCard
/// </summary>
public enum CardMode
{
    /// <summary>
    /// Standard-Modus: Zeigt 3-Punkt-Menu mit Favorisieren/Blockieren
    /// </summary>
    Default,

    /// <summary>
    /// Favoriten-Modus: Zeigt Herz-Icon zum Entfavorisieren
    /// </summary>
    Favorite,

    /// <summary>
    /// Blockiert-Modus: Zeigt X-Icon zum Entblockieren
    /// </summary>
    Blocked,

    /// <summary>
    /// Owner-Modus: Zeigt X-Icon zum Löschen der eigenen Immobilie
    /// </summary>
    Owner
}
