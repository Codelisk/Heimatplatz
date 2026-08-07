using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;

namespace Heimatplatz.Api.Features.Dashboards.Data.Entities;

/// <summary>
/// Eine persoenliche Uebersicht ("Meine Uebersicht") eines Nutzers. Die validierte
/// KI-Definition liegt als JSON in <see cref="DefinitionJson"/> (Schema-Aenderungen
/// brauchen keine Migration). Die Generation*-Spalten gehoeren dem Generierungs-Job
/// und liegen bewusst NICHT im JSON - Definition und Job-Fortschritt koennen so nie
/// kollidieren (gleiches Muster wie die Description*-Spalten am PropertyDraft).
/// </summary>
public class UserDashboard : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>Titel (von der KI vorgeschlagen, vom Nutzer umbenennbar)</summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Ansichts-Typ aus DashboardViewTypes: "dashboard" (Widget-Komposition)
    /// oder "list" (seitenfuellende Immobilienliste). Steuert KI-Prompt,
    /// Validierungsregeln und Rendering; bleibt ueber Verfeinerungsrunden erhalten.
    /// </summary>
    public string ViewType { get; set; } = DashboardViewTypes.Dashboard;

    /// <summary>Serialisierte, validierte DashboardDefinition (null bis zur ersten erfolgreichen Generierung)</summary>
    public string? DefinitionJson { get; set; }

    /// <summary>Schema-Version der gespeicherten Definition (tolerantes Laden aelterer Staende)</summary>
    public int SchemaVersion { get; set; } = DashboardDefinition.CurrentSchemaVersion;

    /// <summary>Zustand der asynchronen KI-Generierung</summary>
    public DashboardGenerationStatus GenerationStatus { get; set; } = DashboardGenerationStatus.None;

    /// <summary>Nutzerfreundliche Fehlermeldung (nur bei Status Failed)</summary>
    public string? GenerationError { get; set; }

    public DateTimeOffset? GenerationRequestedAt { get; set; }

    public DateTimeOffset? GenerationCompletedAt { get; set; }
}
