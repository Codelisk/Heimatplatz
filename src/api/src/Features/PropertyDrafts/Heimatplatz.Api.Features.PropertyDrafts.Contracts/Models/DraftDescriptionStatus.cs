namespace Heimatplatz.Api.Features.PropertyDrafts.Contracts.Models;

/// <summary>
/// Zustand der asynchronen KI-Beschreibungs-Generierung eines Inserat-Entwurfs.
/// Wird in eigenen Spalten am Entwurf gehalten (NICHT im PayloadJson), damit
/// Client-Auto-Saves des Payloads den Job-Fortschritt nie ueberschreiben.
/// </summary>
public enum DraftDescriptionStatus
{
    /// <summary>Keine Generierung angefordert</summary>
    None = 0,

    /// <summary>Job eingeplant, wartet auf Ausfuehrung</summary>
    Queued = 1,

    /// <summary>Job laeuft (inkl. Wartezeit zwischen Retries)</summary>
    InProgress = 2,

    /// <summary>Beschreibung liegt in GeneratedDescription vor</summary>
    Finished = 3,

    /// <summary>Alle Versuche fehlgeschlagen (Details in DescriptionError)</summary>
    Failed = 4
}
