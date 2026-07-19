namespace Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;

/// <summary>
/// Triage-Status einer Fehlergruppe.
/// </summary>
public enum ErrorGroupStatus
{
    Open,
    Resolved,
    Ignored
}

/// <summary>
/// Herkunft eines Telemetrie-Eintrags.
/// </summary>
public enum TelemetrySource
{
    Api,
    Maui,
    Web
}

/// <summary>
/// Zusammenfassung einer Fehlergruppe (Fingerprint-dedupliziert) fuer Listen und Details.
/// </summary>
public record ErrorGroupSummaryDto(
    Guid Id,
    string ExceptionType,
    string Title,
    string SampleMessage,
    DateTimeOffset FirstSeenUtc,
    DateTimeOffset LastSeenUtc,
    long OccurrenceCount,
    string? LastTraceId,
    ErrorGroupStatus Status
);

/// <summary>
/// Ein persistierter Log-Eintrag (API-Log oder Client-Report).
/// </summary>
public record TelemetryLogEntryDto(
    Guid Id,
    DateTimeOffset TimestampUtc,
    string Level,
    string Category,
    string Message,
    string? TraceId,
    string? SpanId,
    string? ExceptionType,
    string? ExceptionMessage,
    string? ExceptionStackTrace,
    Guid? ErrorGroupId,
    string? UserId,
    string? ClientApp,
    TelemetrySource Source,
    string? AttributesJson
);

/// <summary>
/// Ein persistierter Span eines Traces (fuer die Waterfall-Ansicht).
/// </summary>
public record TraceSpanDto(
    Guid Id,
    string TraceId,
    string SpanId,
    string? ParentSpanId,
    string Name,
    string Kind,
    DateTimeOffset StartTimeUtc,
    double DurationMs,
    string StatusCode,
    string? StatusDescription,
    string? HttpMethod,
    string? HttpRoute,
    int? HttpStatusCode,
    string? UserId,
    string? ClientApp,
    string? AttributesJson
);

/// <summary>
/// Ein einzelner Log-/Fehler-Eintrag aus einem Client (MAUI-App).
/// </summary>
/// <param name="TimestampUtc">Zeitpunkt auf dem Client (UTC)</param>
/// <param name="Level">Log-Level als Name (z.B. "Error", "Warning")</param>
/// <param name="Message">Fehlermeldung bzw. Log-Text</param>
/// <param name="ExceptionText">Kompletter Exception-Text inkl. Stacktrace (ToString())</param>
/// <param name="Screen">Aktive Seite/Screen zum Fehlerzeitpunkt</param>
/// <param name="Traceparent">Optionaler W3C-traceparent des zugehoerigen API-Calls</param>
public record ClientLogEntryDto(
    DateTimeOffset TimestampUtc,
    string Level,
    string Message,
    string? ExceptionText = null,
    string? Screen = null,
    string? Traceparent = null
);

/// <summary>
/// Fehler-/Warnungs-Zaehler eines Kalendertages (UTC) fuer die Stats-Ansicht.
/// </summary>
public record DailyErrorCountDto(
    DateOnly Date,
    int ErrorCount,
    int WarningCount
);
