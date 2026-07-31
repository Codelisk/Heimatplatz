using Shiny.Mediator;

namespace Heimatplatz.Api.Features.OpenImmoImport.Contracts.Mediator.Requests;

/// <summary>
/// Startet den OpenImmo-Import manuell (alle konfigurierten Feeds). Der Import laeuft
/// fire-and-forget im Hintergrund - Ergebnis-Zaehler stehen danach im Status-Endpoint
/// (GetOpenImmoImportStatusRequest) und im Log.
/// </summary>
public record TriggerOpenImmoImportRequest : IRequest<TriggerOpenImmoImportResponse>
{
    /// <summary>
    /// True = Marker-Kurzschluss umgehen und die aktuell liegende Feed-Datei auch dann
    /// erneut importieren, wenn sie seit dem letzten Lauf unveraendert ist.
    /// </summary>
    public bool Force { get; init; }
}

public record TriggerOpenImmoImportResponse
{
    public bool Started { get; init; }
    public string Message { get; init; } = "";
}
