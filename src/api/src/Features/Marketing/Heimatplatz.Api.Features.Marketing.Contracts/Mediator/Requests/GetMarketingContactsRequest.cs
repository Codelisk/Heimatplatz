using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Kontaktliste mit Suche (E-Mail/Name/Firma), Filtern und Paging.
/// Status/ContactType kommen als Enum-Namen-Strings aus den Query-Parametern
/// (leer = kein Filter) - bewusst string statt Enum, damit das Query-Binding des
/// generierten Endpoints robust bleibt.
/// DueOnly=true liefert nur faellige Wiedervorlagen (NextFollowUpAt &lt;= jetzt).
/// </summary>
public record GetMarketingContactsRequest(
    string? Search,
    string? Status,
    string? ContactType,
    bool DueOnly = false,
    int Page = 0,
    int PageSize = 50
) : IRequest<GetMarketingContactsResponse>;

public record GetMarketingContactsResponse(
    List<MarketingContactDto> Contacts,
    int Total,
    int PageSize,
    int CurrentPage,
    bool HasMore,
    List<MarketingStatusCountDto> StatusCounts,
    int DueCount
);

/// <summary>
/// Anzahl Kontakte je Funnel-Status fuer die Pipeline-Chips der Kontaktliste.
/// Suche/Typ-Filter wirken auf die Zaehler, der Status-Filter bewusst nicht -
/// die Chips bleiben beim Umschalten stabil. Status ohne Kontakte fehlen in der Liste.
/// </summary>
public record MarketingStatusCountDto(MarketingContactStatus Status, int Count);
