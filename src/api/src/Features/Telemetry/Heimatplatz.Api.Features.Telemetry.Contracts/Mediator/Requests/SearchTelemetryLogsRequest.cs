using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Requests;

/// <summary>
/// Durchsucht persistierte Log-Eintraege (paged, neueste zuerst).
/// Zeitfilter als ISO-8601-Strings (DateTimeOffset-Query-Parameter binden nicht).
/// </summary>
/// <param name="MinLevel">Minimales Log-Level als Name (z.B. "Warning"); ohne = alle</param>
/// <param name="TraceId">Nur Eintraege dieses Traces</param>
/// <param name="Search">Volltext-Filter auf Message/Category/ExceptionType</param>
/// <param name="Source">Filter auf Herkunft (Api/Maui/Web)</param>
/// <param name="ErrorGroupId">Nur Eintraege dieser Fehlergruppe</param>
/// <param name="From">Ab Zeitpunkt (ISO-8601)</param>
/// <param name="To">Bis Zeitpunkt (ISO-8601)</param>
public record SearchTelemetryLogsRequest(
    string? MinLevel = null,
    string? TraceId = null,
    string? Search = null,
    TelemetrySource? Source = null,
    Guid? ErrorGroupId = null,
    string? From = null,
    string? To = null,
    int Page = 1,
    int PageSize = 100
) : IRequest<SearchTelemetryLogsResponse>;

/// <summary>
/// Log-Seite, neueste Eintraege zuerst.
/// </summary>
public record SearchTelemetryLogsResponse(
    List<TelemetryLogEntryDto> Logs,
    int TotalCount,
    int Page,
    int PageSize
);
