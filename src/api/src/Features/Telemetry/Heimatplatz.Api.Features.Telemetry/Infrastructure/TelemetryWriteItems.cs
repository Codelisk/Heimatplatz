using Heimatplatz.Api.Features.Telemetry.Data.Entities;

namespace Heimatplatz.Api.Features.Telemetry.Infrastructure;

/// <summary>
/// Ein Element der Writer-Queue (Span oder Log, ggf. mit Fingerprint fuer den
/// Fehlergruppen-Upsert).
/// </summary>
public abstract record TelemetryWriteItem;

public sealed record SpanWriteItem(TelemetrySpan Span) : TelemetryWriteItem;

public sealed record LogWriteItem(TelemetryLog Log, ErrorFingerprint? Fingerprint) : TelemetryWriteItem;

/// <summary>
/// Fingerprint-Daten eines Fehler-Logs fuer den Upsert der zugehoerigen
/// <see cref="TelemetryErrorGroup"/>.
/// </summary>
public sealed record ErrorFingerprint(
    string Hash,
    string ExceptionType,
    string Title,
    string SampleMessage,
    string? SampleStackTrace
);
