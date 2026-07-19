using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;

namespace Heimatplatz.Api.Features.Telemetry.Data.Entities;

/// <summary>
/// Fingerprint-deduplizierte Fehlergruppe: gleicher Exception-Typ + normalisierte
/// Top-Stackframes + Message-Template ergeben denselben Hash. Gruppen werden nie
/// von der Retention geloescht (nur die zugehoerigen Log-Eintraege).
/// </summary>
public class TelemetryErrorGroup : BaseEntity
{
    /// <summary>SHA-256-Hex des normalisierten Fingerprints (eindeutig)</summary>
    public string FingerprintHash { get; set; } = null!;

    public string ExceptionType { get; set; } = null!;

    /// <summary>Kurzbezeichnung fuer Listen (Exception-Typ + Message-Anfang)</summary>
    public string Title { get; set; } = null!;

    public string SampleMessage { get; set; } = null!;

    public string? SampleStackTrace { get; set; }

    public DateTimeOffset FirstSeenUtc { get; set; }

    public DateTimeOffset LastSeenUtc { get; set; }

    public long OccurrenceCount { get; set; }

    /// <summary>Trace-Id des letzten Auftretens (Einstieg fuer die Waterfall-Ansicht)</summary>
    public string? LastTraceId { get; set; }

    public ErrorGroupStatus Status { get; set; }
}
