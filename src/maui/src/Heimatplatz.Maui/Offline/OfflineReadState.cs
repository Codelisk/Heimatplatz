using Shiny;

namespace Heimatplatz.Maui.Offline;

/// <summary>
/// Prozessweiter Zustand fuer den letzten fehlgeschlagenen Backend-Abruf. So koennen
/// Listen- und Detailseiten einen lokalen Fallback ehrlich kennzeichnen.
/// </summary>
[Singleton]
public sealed class OfflineReadState
{
    private int _backendUnavailable;

    /// <summary>
    /// Feuert bei jedem Zustandswechsel (beliebiger Thread!). Sichtbare Seiten
    /// koennen so ihren "zwischengespeicherte Daten"-Hinweis nachziehen, wenn ein
    /// Hintergrund-Refresh erst nach dem synchronen Cache-Hit fehlschlaegt.
    /// </summary>
    public event EventHandler? Changed;

    public bool IsBackendUnavailable => Volatile.Read(ref _backendUnavailable) == 1;

    public void MarkUnavailable()
    {
        if (Interlocked.Exchange(ref _backendUnavailable, 1) != 1)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    public void MarkAvailable()
    {
        if (Interlocked.Exchange(ref _backendUnavailable, 0) != 0)
            Changed?.Invoke(this, EventArgs.Empty);
    }
}
