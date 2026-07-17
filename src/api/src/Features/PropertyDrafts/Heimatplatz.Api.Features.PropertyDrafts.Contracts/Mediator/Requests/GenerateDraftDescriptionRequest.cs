using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyDrafts.Contracts.Mediator.Requests;

/// <summary>
/// Fordert die asynchrone KI-Generierung der Inserats-Beschreibung fuer einen Entwurf an.
/// Der Server plant einen Hintergrund-Job (TickerQ) ein, der aus den Eckdaten des Entwurfs,
/// den Stichwoertern und den Fotos eine Beschreibung im konfigurierten Wortbereich erstellt.
/// Der Client pollt den Fortschritt ueber GetDraftDescription und kann sofort weiterarbeiten.
/// Id im Body statt in der Route - Shiny-Mediator-OpenAPI-Generator-Kompatibilitaet
/// (gleiche Praezedenz wie SavePropertyDraftRequest).
/// </summary>
public class GenerateDraftDescriptionRequest : IRequest<GenerateDraftDescriptionResponse>
{
    public Guid Id { get; set; }

    /// <summary>Stichwoerter bzw. frei diktierter/getippter Text des Verkaeufers</summary>
    public string Keywords { get; set; } = "";
}

/// <summary>
/// Response mit dem neuen Generierungs-Status (Queued).
/// </summary>
public record GenerateDraftDescriptionResponse(
    DraftDescriptionStatus Status
);
