using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Firmenpool: aufrechte Firmen aus dem Firmenbuch-Katalog, deren Name auf die
/// Immobilienbranche hindeutet (Schlagwortliste in Marketing:LeadPool:NameKeywords).
/// Das Firmenbuch fuehrt keine Branche und keine Kontaktdaten - der Namensfilter ist
/// die einzige verfuegbare Eingrenzung.
/// OnlyOpen=true blendet Firmen aus, die bereits als Kontakt uebernommen wurden.
/// </summary>
public record GetMarketingLeadPoolRequest(
    string? Search,
    string? Sitz,
    bool OnlyOpen = true,
    int Page = 0,
    int PageSize = 50
) : IRequest<GetMarketingLeadPoolResponse>;

public record GetMarketingLeadPoolResponse(
    List<MarketingLeadDto> Leads,
    int Total,
    int PageSize,
    int CurrentPage,
    bool HasMore
);
