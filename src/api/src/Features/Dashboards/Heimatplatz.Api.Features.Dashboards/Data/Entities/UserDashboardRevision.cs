using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Dashboards.Data.Entities;

/// <summary>
/// Eine Generierungsrunde einer Uebersicht: der Wunsch bzw. die Verfeinerungs-Anweisung
/// des Nutzers und das Ergebnis. Traegt den TickerQ-Job (Payload = Revision-Id),
/// ermoeglicht "Rueckgaengig" (RevertDashboard) und liefert mit dem Rohausgabe-Auszug
/// die Basis fuers Prompt-Tuning.
/// </summary>
public class UserDashboardRevision : BaseEntity
{
    public Guid DashboardId { get; set; }

    /// <summary>Freitext-Wunsch (Erstellung) bzw. Verfeinerungs-Anweisung des Nutzers</summary>
    public string UserPrompt { get; set; } = "";

    /// <summary>Validierte Definition dieser Runde (null solange der Job laeuft bzw. bei Fehlschlag)</summary>
    public string? DefinitionJson { get; set; }

    /// <summary>Gekappter Auszug der KI-Rohausgabe (Debugging/Prompt-Tuning)</summary>
    public string? RawOutputExcerpt { get; set; }
}
