using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Voller Firmendatensatz fuer die Firmenpool-Detailansicht, live aus der Firmenpool-API
/// (Auszugsdaten, Funktionaere, GISA-Gewerbe, Abschluss-Anzahl) plus lokalem
/// Uebernahme-Status. <c>Company == null</c> heisst: FNR dort unbekannt.
/// </summary>
public record GetMarketingLeadCompanyRequest(string Fnr) : IRequest<GetMarketingLeadCompanyResponse>;

public record GetMarketingLeadCompanyResponse(
    MarketingLeadCompanyDto? Company,
    Guid? ContactId,
    MarketingContactStatus? ContactStatus
);

public record MarketingLeadCompanyDto(
    string Fnr,
    string Name,
    string? Status,
    string? Euid,
    DateOnly? Gegruendet,
    string? Strasse,
    string? Hausnummer,
    string? Plz,
    string? Ort,
    string? Staat,
    string? Sitz,
    string? RechtsformCode,
    string? RechtsformText,
    string? GerichtText,
    string? Handelsregisternummer,
    DateTimeOffset? AuszugStand,
    int AbschluesseVorhanden,
    List<MarketingLeadOfficerDto> Funktionaere,
    List<MarketingLeadTradeDto> Gewerbe
);

public record MarketingLeadOfficerDto(
    string Name,
    string? FunktionText,
    DateOnly? Seit,
    bool Aktiv
);

public record MarketingLeadTradeDto(
    long GisaZahl,
    string? Wortlaut,
    string? Plz,
    string? Ort,
    List<string> WeitereStandorte,
    bool Aktiv
);
