using System.Collections.Concurrent;
using Heimatplatz.Maui.ApiClient.Generated;
using Shiny;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Uebergibt die bereits geladenen Listendaten einer angetippten Karte an die
/// Detailseite. Damit steht der Seitenkopf (Titel, Preis, Adresse, Typ, Kernfakten,
/// erstes Foto) sofort - ohne auf den Detail-Request zu warten. Die vollstaendigen
/// Daten ersetzen den Vorschau-Zustand, sobald sie eintreffen.
/// </summary>
[Singleton]
public sealed class PropertyHandoffCache
{
    // Nur die zuletzt angetippten Karten: die Detailseite holt den Eintrag direkt nach
    // der Navigation ab, die restlichen sind lediglich Reserve fuer Zurueck-Navigation.
    private const int MaxEntries = 16;

    private readonly ConcurrentDictionary<Guid, PropertyListItemDto> _items = new();

    public void Put(PropertyListItemDto item)
    {
        // Bewusst simpel: bei Ueberlauf komplett leeren statt LRU zu fuehren. Ein
        // verlorener Eintrag kostet nur den Vorschau-Zustand, nie Korrektheit.
        if (_items.Count >= MaxEntries)
            _items.Clear();

        _items[item.Id] = item;
    }

    public PropertyListItemDto? Get(Guid id)
        => _items.TryGetValue(id, out var item) ? item : null;
}
