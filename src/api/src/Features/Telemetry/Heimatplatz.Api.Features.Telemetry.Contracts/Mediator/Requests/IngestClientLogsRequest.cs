using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Requests;

/// <summary>
/// Nimmt einen Batch Client-Logs/Fehler entgegen (anonym, rate-limitiert).
/// Groessen-Caps werden serverseitig erzwungen (Anzahl Eintraege, Stringlaengen).
/// </summary>
/// <param name="Source">Herkunft (Maui/Web)</param>
/// <param name="AppVersion">Client-App-Version (z.B. "1.76.0")</param>
/// <param name="Platform">Plattform (z.B. "Android", "iOS", "Windows")</param>
/// <param name="Entries">Die Log-/Fehler-Eintraege</param>
public record IngestClientLogsRequest(
    TelemetrySource Source,
    string? AppVersion,
    string? Platform,
    List<ClientLogEntryDto> Entries
) : IRequest<IngestClientLogsResponse>;

/// <summary>
/// Anzahl der uebernommenen Eintraege (nach Cap-Kuerzung).
/// </summary>
public record IngestClientLogsResponse(
    int Accepted
);
