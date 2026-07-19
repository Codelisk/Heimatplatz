using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;

namespace Heimatplatz.Api.Features.Telemetry.Data.Entities;

/// <summary>
/// Ein persistierter Log-Eintrag: API-Logs ab Warning immer, Info/Debug nur als
/// nachgereichter Kontext von Fehler-Traces, Client-Reports via Ingestion-Endpoint.
/// </summary>
public class TelemetryLog : BaseEntity
{
    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>W3C-Trace-Id, falls der Log innerhalb eines Traces entstand</summary>
    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    /// <summary>LogLevel als Name (Trace/Debug/Information/Warning/Error/Critical)</summary>
    public string Level { get; set; } = null!;

    /// <summary>Logger-Kategorie bzw. "Client" bei Ingestion-Eintraegen</summary>
    public string Category { get; set; } = null!;

    public int EventId { get; set; }

    /// <summary>Original-Message-Template (strukturiertes Logging)</summary>
    public string? MessageTemplate { get; set; }

    /// <summary>Formatierte Nachricht</summary>
    public string Message { get; set; } = null!;

    public string? ExceptionType { get; set; }

    public string? ExceptionMessage { get; set; }

    public string? ExceptionStackTrace { get; set; }

    /// <summary>Fehlergruppe (Fingerprint), nur bei Eintraegen mit Exception</summary>
    public Guid? ErrorGroupId { get; set; }

    /// <summary>User-Id (sub-Claim) des Requests, falls authentifiziert</summary>
    public string? UserId { get; set; }

    /// <summary>Client-App aus dem X-Client-App-Header bzw. AppVersion der Ingestion</summary>
    public string? ClientApp { get; set; }

    /// <summary>Herkunft: Api (Server-Log) oder Maui/Web (Client-Report)</summary>
    public TelemetrySource Source { get; set; }

    /// <summary>Strukturierte Log-Attribute als JSON-Objekt (Text-Spalte)</summary>
    public string? AttributesJson { get; set; }
}
