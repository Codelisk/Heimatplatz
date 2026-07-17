using System.Collections.Concurrent;
using System.Globalization;

namespace Heimatplatz.Maui.Offline;

/// <summary>
/// Merkt sich pro Request-Typ, seit wann lokal gecachte Antworten als veraltet gelten.
/// Der PropertySyncService setzt die Markierung, wenn der Delta-Sync neue Immobilien
/// meldet, die nicht client-seitig in gefilterte/paginierte Listen eingefuegt werden
/// koennen. <see cref="LocalFirstRequestMiddleware{TRequest,TResult}"/> liefert markierte
/// Eintraege weiterhin sofort aus, stoesst aber beim naechsten Zugriff einen
/// Hintergrund-Refresh an. Ueberlebt App-Neustarts (Preferences).
/// </summary>
public sealed class CacheStalenessRegistry
{
    private const string PreferenceKeyPrefix = "cache.stale-since:";

    private readonly ConcurrentDictionary<string, DateTimeOffset?> _staleSince = new(StringComparer.Ordinal);

    /// <summary>Markiert alle Cache-Eintraege der Request-Typen als veraltet (ab jetzt)</summary>
    public void MarkStale(IEnumerable<string> requestTypeFullNames)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var typeName in requestTypeFullNames)
        {
            _staleSince[typeName] = now;
            Preferences.Default.Set(
                PreferenceKeyPrefix + typeName,
                now.ToString("O", CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// True wenn ein Cache-Eintrag dieses Request-Typs vor der Stale-Markierung
    /// geschrieben wurde und deshalb im Hintergrund aktualisiert werden soll
    /// </summary>
    public bool IsStale(string requestTypeFullName, DateTimeOffset entryCreatedAt)
    {
        var staleSince = _staleSince.GetOrAdd(requestTypeFullName, LoadFromPreferences);
        return staleSince is { } marker && entryCreatedAt < marker;
    }

    private static DateTimeOffset? LoadFromPreferences(string typeName)
    {
        var raw = Preferences.Default.Get<string?>(PreferenceKeyPrefix + typeName, null);
        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value)
            ? value
            : null;
    }
}
