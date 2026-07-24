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

    public bool IsBackendUnavailable => Volatile.Read(ref _backendUnavailable) == 1;

    public void MarkUnavailable() => Interlocked.Exchange(ref _backendUnavailable, 1);

    public void MarkAvailable() => Interlocked.Exchange(ref _backendUnavailable, 0);
}
