using System.Text.Json;

namespace Heimatplatz.Maui.Features.Telemetry.Services;

/// <summary>
/// Persistiert Crash-Reports in den Preferences, damit sie den Prozess-Tod
/// ueberleben und beim naechsten App-Start an die API gesendet werden koennen.
/// Bewusst statisch und synchron: muss aus Crash-Handlern heraus funktionieren,
/// wo weder DI noch async zuverlaessig verfuegbar sind. Alles fail-open.
/// </summary>
public static class CrashReportStore
{
    private const string PreferencesKey = "telemetry.pending-client-logs";

    // Entspricht dem Server-Cap MaxBatchEntries (aeltere Eintraege fallen raus)
    private const int MaxPending = 20;

    private static readonly object SyncRoot = new();

    public sealed record PendingEntry(
        DateTimeOffset TimestampUtc,
        string Level,
        string Message,
        string? ExceptionText,
        string? Screen
    );

    public static void Append(PendingEntry entry)
    {
        try
        {
            lock (SyncRoot)
            {
                var pending = Read();
                pending.Add(entry);
                if (pending.Count > MaxPending)
                {
                    pending.RemoveRange(0, pending.Count - MaxPending);
                }

                Preferences.Default.Set(PreferencesKey, JsonSerializer.Serialize(pending));
            }
        }
        catch
        {
            // Crash-Persistenz darf nie selbst crashen
        }
    }

    public static List<PendingEntry> Read()
    {
        try
        {
            var json = Preferences.Default.Get(PreferencesKey, string.Empty);
            return string.IsNullOrEmpty(json)
                ? []
                : JsonSerializer.Deserialize<List<PendingEntry>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void Clear()
    {
        try
        {
            Preferences.Default.Remove(PreferencesKey);
        }
        catch
        {
            // fail-open
        }
    }
}
