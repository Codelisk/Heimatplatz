namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Definiert den Anzeigemodus der PropertyCard
/// </summary>
public enum CardMode
{
    /// <summary>Standard-Modus: Zeigt Favorisieren/Blockieren-Buttons (nur angemeldet)</summary>
    Default,

    /// <summary>Favoriten-Modus: Zeigt Herz-Icon zum Entfavorisieren</summary>
    Favorite,

    /// <summary>Blockiert-Modus: Zeigt X-Icon zum Entblockieren</summary>
    Blocked,

    /// <summary>Owner-Modus: Zeigt X-Icon zum Loeschen der eigenen Immobilie</summary>
    Owner
}
