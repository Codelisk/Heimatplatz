using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Requests;

/// <summary>
/// Listet Fehlergruppen (Fingerprint-dedupliziert), sortiert nach letztem Auftreten
/// oder Haeufigkeit. Zeitfilter als ISO-8601-Strings (DateTimeOffset-Query-Parameter
/// binden nicht, siehe GetPropertyChangesRequest).
/// </summary>
/// <param name="Status">Filter auf Triage-Status (ohne = alle)</param>
/// <param name="Search">Volltext-Filter auf ExceptionType/Title/SampleMessage</param>
/// <param name="From">Nur Gruppen mit LastSeenUtc ab diesem Zeitpunkt (ISO-8601)</param>
/// <param name="To">Nur Gruppen mit LastSeenUtc bis zu diesem Zeitpunkt (ISO-8601)</param>
/// <param name="Sort">"lastseen" (Default) oder "count"</param>
public record ListErrorGroupsRequest(
    ErrorGroupStatus? Status = null,
    string? Search = null,
    string? From = null,
    string? To = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<ListErrorGroupsResponse>;

/// <summary>
/// Fehlergruppen-Seite, sortiert gemaess Request.
/// </summary>
public record ListErrorGroupsResponse(
    List<ErrorGroupSummaryDto> Groups,
    int TotalCount,
    int Page,
    int PageSize
);
