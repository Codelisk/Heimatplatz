namespace Heimatplatz.Maui.Offline;

/// <summary>
/// Offline-faehiger Read-Request ohne Internet UND ohne lokal gespeicherte Antwort.
/// Erbt von HttpRequestException, damit bestehende Fehlerpfade (GetErrorHint etc.)
/// den Fall als Verbindungsproblem einordnen.
/// </summary>
public sealed class OfflineDataUnavailableException()
    : HttpRequestException("Keine Internetverbindung und keine lokal gespeicherten Daten für diese Anfrage.");
