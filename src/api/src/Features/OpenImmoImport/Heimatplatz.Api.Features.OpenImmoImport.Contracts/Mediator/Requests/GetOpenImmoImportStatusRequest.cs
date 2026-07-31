using Shiny.Mediator;

namespace Heimatplatz.Api.Features.OpenImmoImport.Contracts.Mediator.Requests;

/// <summary>
/// Liefert den Import-Status je konfiguriertem Feed (letzter Lauf aus dem Marker-File
/// plus aktueller Property-Bestand der Quelle).
/// </summary>
public record GetOpenImmoImportStatusRequest : IRequest<GetOpenImmoImportStatusResponse>;

public record GetOpenImmoImportStatusResponse
{
    /// <summary>True solange ein Import-Lauf (Worker oder Trigger) aktiv ist.</summary>
    public bool IsRunning { get; init; }

    public List<OpenImmoFeedStatus> Feeds { get; init; } = [];
}

public record OpenImmoFeedStatus
{
    public required string FeedKey { get; init; }
    public required string SourceName { get; init; }

    /// <summary>Zeitpunkt des letzten erfolgreichen Imports (null = noch nie importiert).</summary>
    public DateTimeOffset? LastImportAt { get; init; }

    /// <summary>Dateiname der zuletzt importierten Feed-Datei.</summary>
    public string? LastFileName { get; init; }

    /// <summary>Schreibzeitpunkt der zuletzt importierten Feed-Datei.</summary>
    public DateTimeOffset? LastFileWriteTime { get; init; }

    /// <summary>Ergebnis-Zusammenfassung des letzten Laufs (Zaehler + Fehler).</summary>
    public string? LastResultSummary { get; init; }

    /// <summary>Aktueller Property-Bestand dieser Quelle in der Datenbank.</summary>
    public int PropertyCount { get; init; }
}
