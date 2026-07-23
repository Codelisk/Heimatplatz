using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Alle E-Mail-Vorlagen, sortiert nach DisplayOrder/Name.
/// IncludeInactive=false (Default) liefert nur aktive - so bekommt die Schreiben-Seite
/// ausschliesslich Vorlagen, die auch verwendet werden sollen.
/// </summary>
public record GetMarketingTemplatesRequest(
    bool IncludeInactive = false
) : IRequest<GetMarketingTemplatesResponse>;

public record GetMarketingTemplatesResponse(List<MarketingTemplateDto> Templates);
