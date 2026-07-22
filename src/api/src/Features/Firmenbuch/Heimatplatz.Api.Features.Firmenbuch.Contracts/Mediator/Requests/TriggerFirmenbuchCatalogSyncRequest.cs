using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Firmenbuch.Contracts.Mediator.Requests;

/// <summary>
/// Loest den Firmenbuch-Katalog-Sync aus (laeuft fire-and-forget im Hintergrund).
/// <paramref name="OrtNr"/>: 1-stellig Bundesland (z.B. "4" = Oberoesterreich),
/// 3-stellig Bezirk, 5-stellig Gemeinde, leer = ganz Oesterreich.
/// </summary>
public record TriggerFirmenbuchCatalogSyncRequest(string OrtNr = "4")
    : IRequest<TriggerFirmenbuchCatalogSyncResponse>;

public record TriggerFirmenbuchCatalogSyncResponse
{
    public List<string> Messages { get; init; } = [];
}
