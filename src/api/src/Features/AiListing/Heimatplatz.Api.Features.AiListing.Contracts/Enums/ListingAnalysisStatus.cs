using System.Text.Json.Serialization;

namespace Heimatplatz.Api.Features.AiListing.Contracts.Enums;

/// <summary>
/// Status einer KI-Inserat-Analyse (Job-Lebenszyklus).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ListingAnalysisStatus
{
    /// <summary>In der Warteschlange, noch nicht gestartet</summary>
    Queued = 1,

    /// <summary>Analyse laeuft gerade</summary>
    InProgress = 2,

    /// <summary>Analyse erfolgreich abgeschlossen, Ergebnis verfuegbar</summary>
    Finished = 3,

    /// <summary>Analyse fehlgeschlagen</summary>
    Failed = 4
}
