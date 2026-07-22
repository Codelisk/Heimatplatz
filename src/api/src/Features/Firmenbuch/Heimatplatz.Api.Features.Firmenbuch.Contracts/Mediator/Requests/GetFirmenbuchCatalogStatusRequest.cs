using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Firmenbuch.Contracts.Mediator.Requests;

/// <summary>Bestandszaehler des Firmenbuch-Katalogs.</summary>
public record GetFirmenbuchCatalogStatusRequest : IRequest<GetFirmenbuchCatalogStatusResponse>;

public record GetFirmenbuchCatalogStatusResponse
{
    public int TotalCompanies { get; init; }

    /// <summary>Aufrechte Firmen (Status leer laut Schnittstelle)</summary>
    public int ActiveCompanies { get; init; }

    /// <summary>Zaehler je Status-Auspraegung ("historisch", "geloescht", ...)</summary>
    public Dictionary<string, int> ByStatus { get; init; } = [];

    public DateTimeOffset? LastSyncAt { get; init; }
}
