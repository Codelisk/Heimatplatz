using System.Diagnostics;
using System.Text.Json;
using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;
using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace Heimatplatz.Api.Features.Telemetry.Infrastructure;

/// <summary>
/// Wandelt Activity/LogRecord sofort in Entities um. Muss synchron in OnEnd passieren:
/// LogRecord-Instanzen sind im SDK gepoolt und werden nach der Prozessor-Kette recycelt.
/// </summary>
public static class TelemetryMaterializer
{
    private const int MaxAttributeValueLength = 4000;

    /// <summary>Span-Tags, die in eigene Spalten wandern und nicht ins Attribute-JSON</summary>
    private static readonly string[] ExtractedSpanTags =
    [
        "http.request.method",
        "http.route",
        "http.response.status_code",
        "user.id",
        "client.app"
    ];

    public static TelemetrySpan Materialize(Activity activity)
    {
        string? httpMethod = null;
        string? httpRoute = null;
        int? httpStatusCode = null;
        string? userId = null;
        string? clientApp = null;
        Dictionary<string, string?>? attributes = null;

        foreach (var tag in activity.TagObjects)
        {
            switch (tag.Key)
            {
                case "http.request.method":
                    httpMethod = tag.Value?.ToString();
                    break;
                case "http.route":
                    httpRoute = tag.Value?.ToString();
                    break;
                case "http.response.status_code":
                    if (int.TryParse(tag.Value?.ToString(), out var status))
                        httpStatusCode = status;
                    break;
                case "user.id":
                    userId = tag.Value?.ToString();
                    break;
                case "client.app":
                    clientApp = tag.Value?.ToString();
                    break;
                default:
                    attributes ??= [];
                    attributes[tag.Key] = Truncate(tag.Value?.ToString());
                    break;
            }
        }

        return new TelemetrySpan
        {
            Id = Guid.CreateVersion7(),
            TraceId = activity.TraceId.ToString(),
            SpanId = activity.SpanId.ToString(),
            // ParentId deckt In-Process- UND Remote-Parents (traceparent vom Client) ab
            ParentSpanId = activity.ParentId != null ? activity.ParentSpanId.ToString() : null,
            Name = Truncate(activity.DisplayName, 512) ?? "",
            Kind = activity.Kind.ToString(),
            StartTimeUtc = new DateTimeOffset(activity.StartTimeUtc, TimeSpan.Zero),
            DurationMs = activity.Duration.TotalMilliseconds,
            StatusCode = activity.Status.ToString(),
            StatusDescription = Truncate(activity.StatusDescription, 2000),
            HttpMethod = Truncate(httpMethod, 16),
            HttpRoute = Truncate(httpRoute, 512),
            HttpStatusCode = httpStatusCode,
            UserId = Truncate(userId, 64),
            ClientApp = Truncate(clientApp, 128),
            AttributesJson = attributes is { Count: > 0 } ? JsonSerializer.Serialize(attributes) : null
        };
    }

    public static TelemetryLog Materialize(LogRecord record, string levelName)
    {
        Dictionary<string, string?>? attributes = null;
        if (record.Attributes != null)
        {
            foreach (var attribute in record.Attributes)
            {
                // Das Template steckt bereits in MessageTemplate
                if (attribute.Key == "{OriginalFormat}")
                    continue;

                attributes ??= [];
                attributes[attribute.Key] = Truncate(attribute.Value?.ToString());
            }
        }

        var (userId, clientApp) = ResolveEnrichmentTags();

        return new TelemetryLog
        {
            Id = Guid.CreateVersion7(),
            TimestampUtc = new DateTimeOffset(record.Timestamp, TimeSpan.Zero),
            TraceId = record.TraceId == default ? null : record.TraceId.ToString(),
            SpanId = record.SpanId == default ? null : record.SpanId.ToString(),
            Level = levelName,
            Category = Truncate(record.CategoryName, 256) ?? "",
            EventId = record.EventId.Id,
            MessageTemplate = record.Body,
            Message = record.FormattedMessage ?? record.Body ?? "",
            ExceptionType = Truncate(record.Exception?.GetType().FullName, 512),
            ExceptionMessage = record.Exception?.Message,
            ExceptionStackTrace = record.Exception?.ToString(),
            UserId = Truncate(userId, 64),
            ClientApp = Truncate(clientApp, 128),
            Source = TelemetrySource.Api,
            AttributesJson = attributes is { Count: > 0 } ? JsonSerializer.Serialize(attributes) : null
        };
    }

    /// <summary>
    /// LogLevel -&gt; Level-Name + Rang (0=Trace .. 5=Critical).
    /// (LogRecord.Severity/LogRecordSeverity ist im stabilen SDK noch experimental/internal.)
    /// </summary>
    public static (string Name, int Rank) MapLevel(LogLevel level)
        => level switch
        {
            LogLevel.Critical => ("Critical", 5),
            LogLevel.Error => ("Error", 4),
            LogLevel.Warning => ("Warning", 3),
            LogLevel.Information => ("Information", 2),
            LogLevel.Debug => ("Debug", 1),
            _ => ("Trace", 0)
        };

    /// <summary>
    /// Liest user.id/client.app aus der aktuellen Activity-Kette (die Enrichment-
    /// Middleware setzt beide auf den Request-Root; Logs entstehen oft in Child-Spans).
    /// </summary>
    private static (string? UserId, string? ClientApp) ResolveEnrichmentTags()
    {
        string? userId = null;
        string? clientApp = null;

        for (var activity = Activity.Current; activity != null; activity = activity.Parent)
        {
            userId ??= activity.GetTagItem("user.id")?.ToString();
            clientApp ??= activity.GetTagItem("client.app")?.ToString();

            if (userId != null && clientApp != null)
                break;
        }

        return (userId, clientApp);
    }

    private static string? Truncate(string? value, int maxLength = MaxAttributeValueLength)
        => value == null || value.Length <= maxLength ? value : value[..maxLength];
}
