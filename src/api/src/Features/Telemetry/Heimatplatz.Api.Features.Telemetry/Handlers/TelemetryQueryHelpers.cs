using System.Globalization;
using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;
using Heimatplatz.Api.Features.Telemetry.Data.Entities;

namespace Heimatplatz.Api.Features.Telemetry.Handlers;

/// <summary>
/// Gemeinsame Helfer der Auswertungs-Handler: ISO-8601-Zeitparsing (Query-Parameter
/// sind Strings), Level-Rangfolge und Entity-&gt;DTO-Mapping (bewusst in-memory,
/// EF kann statische Methodenaufrufe nicht in SQL projizieren).
/// </summary>
internal static class TelemetryQueryHelpers
{
    private static readonly string[] LevelNamesByRank =
        ["Trace", "Debug", "Information", "Warning", "Error", "Critical"];

    public static bool TryParseTime(string? value, out DateTimeOffset result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)
            || !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return false;
        }

        result = result.ToUniversalTime();
        return true;
    }

    /// <summary>-1 fuer unbekannte Level-Namen</summary>
    public static int RankOfLevel(string level)
        => Array.IndexOf(LevelNamesByRank, level);

    /// <summary>Alle Level-Namen ab dem angegebenen (fuer IN-Filter)</summary>
    public static string[] LevelsAtOrAbove(string level)
    {
        var rank = RankOfLevel(level);
        return rank < 0 ? LevelNamesByRank : LevelNamesByRank[rank..];
    }

    public static ErrorGroupSummaryDto Map(TelemetryErrorGroup group) => new(
        group.Id,
        group.ExceptionType,
        group.Title,
        group.SampleMessage,
        group.FirstSeenUtc,
        group.LastSeenUtc,
        group.OccurrenceCount,
        group.LastTraceId,
        group.Status);

    public static TelemetryLogEntryDto Map(TelemetryLog log) => new(
        log.Id,
        log.TimestampUtc,
        log.Level,
        log.Category,
        log.Message,
        log.TraceId,
        log.SpanId,
        log.ExceptionType,
        log.ExceptionMessage,
        log.ExceptionStackTrace,
        log.ErrorGroupId,
        log.UserId,
        log.ClientApp,
        log.Source,
        log.AttributesJson);

    public static TraceSpanDto Map(TelemetrySpan span) => new(
        span.Id,
        span.TraceId,
        span.SpanId,
        span.ParentSpanId,
        span.Name,
        span.Kind,
        span.StartTimeUtc,
        span.DurationMs,
        span.StatusCode,
        span.StatusDescription,
        span.HttpMethod,
        span.HttpRoute,
        span.HttpStatusCode,
        span.UserId,
        span.ClientApp,
        span.AttributesJson);
}
