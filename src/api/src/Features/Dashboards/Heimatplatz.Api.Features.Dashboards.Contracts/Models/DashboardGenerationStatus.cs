namespace Heimatplatz.Api.Features.Dashboards.Contracts.Models;

/// <summary>
/// Zustand der asynchronen KI-Generierung einer Dashboard-Definition.
/// Wird in eigenen Spalten am UserDashboard gehalten (NICHT im DefinitionJson),
/// damit der Job-Fortschritt nie mit der Definition selbst kollidiert
/// (gleiches Muster wie DraftDescriptionStatus im PropertyDrafts-Feature).
/// </summary>
public enum DashboardGenerationStatus
{
    /// <summary>Keine Generierung angefordert (kommt praktisch nicht vor)</summary>
    None = 0,

    /// <summary>Job eingeplant, wartet auf Ausfuehrung</summary>
    Queued = 1,

    /// <summary>Job laeuft (inkl. Wartezeit zwischen Retries)</summary>
    InProgress = 2,

    /// <summary>Definition liegt validiert vor</summary>
    Finished = 3,

    /// <summary>Alle Versuche fehlgeschlagen (Details in GenerationError)</summary>
    Failed = 4
}
