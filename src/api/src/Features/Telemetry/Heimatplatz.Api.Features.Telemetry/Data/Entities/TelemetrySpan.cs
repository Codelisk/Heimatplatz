using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Telemetry.Data.Entities;

/// <summary>
/// Ein persistierter Span eines Traces. Es landen nur Traces in der DB, die die
/// Tail-Sampling-Entscheidung ueberleben (Fehler, langsame Requests, Stichprobe).
/// </summary>
public class TelemetrySpan : BaseEntity
{
    /// <summary>W3C-Trace-Id (32 Hex-Zeichen)</summary>
    public string TraceId { get; set; } = null!;

    /// <summary>W3C-Span-Id (16 Hex-Zeichen)</summary>
    public string SpanId { get; set; } = null!;

    /// <summary>Span-Id des Parents (null beim lokalen Root ohne Remote-Parent)</summary>
    public string? ParentSpanId { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>ActivityKind als Name (Server/Client/Internal/...)</summary>
    public string Kind { get; set; } = null!;

    public DateTimeOffset StartTimeUtc { get; set; }

    public double DurationMs { get; set; }

    /// <summary>ActivityStatusCode als Name (Unset/Ok/Error)</summary>
    public string StatusCode { get; set; } = null!;

    public string? StatusDescription { get; set; }

    public string? HttpMethod { get; set; }

    public string? HttpRoute { get; set; }

    public int? HttpStatusCode { get; set; }

    /// <summary>User-Id (sub-Claim) des Requests, falls authentifiziert</summary>
    public string? UserId { get; set; }

    /// <summary>Client-App aus dem X-Client-App-Header (z.B. "Maui/1.76.0")</summary>
    public string? ClientApp { get; set; }

    /// <summary>Restliche Span-Attribute als JSON-Objekt (Text-Spalte)</summary>
    public string? AttributesJson { get; set; }
}
