using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Kontaktliste mit Suche (E-Mail/Name/Firma), Filtern und Paging.
/// Status/ContactType kommen als Enum-Namen-Strings aus den Query-Parametern
/// (leer = kein Filter) - bewusst string statt Enum, damit das Query-Binding des
/// generierten Endpoints robust bleibt.
/// </summary>
public record GetMarketingContactsRequest(
    string? Search,
    string? Status,
    string? ContactType,
    int Page = 0,
    int PageSize = 50
) : IRequest<GetMarketingContactsResponse>;

public record GetMarketingContactsResponse(
    List<MarketingContactDto> Contacts,
    int Total,
    int PageSize,
    int CurrentPage,
    bool HasMore
);
