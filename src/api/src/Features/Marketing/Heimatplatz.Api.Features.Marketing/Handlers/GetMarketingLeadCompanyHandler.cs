using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Firmenbuch.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Firmenpool-Detailansicht: voller Firmendatensatz live aus der Firmenpool-API
/// (Auszug, Funktionaere, GISA-Gewerbe, Abschluss-Anzahl) plus lokalem Uebernahme-Status.
/// Company == null signalisiert der Intern-Seite "FNR unbekannt" - bewusst kein 404,
/// damit die Seite den Fall selbst huebsch darstellen kann.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class GetMarketingLeadCompanyHandler(
    AppDbContext dbContext,
    IFirmenpoolApiClient firmenpool,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetMarketingLeadCompanyRequest, GetMarketingLeadCompanyResponse>
{
    [MediatorHttpGet("/lead-pool/company/{Fnr}", OperationId = "GetMarketingLeadCompany")]
    public async Task<GetMarketingLeadCompanyResponse> Handle(GetMarketingLeadCompanyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var company = await firmenpool.GetCompanyDetailAsync(request.Fnr, cancellationToken);
        if (company is null)
            return new GetMarketingLeadCompanyResponse(null, null, null);

        var kontakt = await dbContext.Set<MarketingContact>()
            .AsNoTracking()
            .Where(x => x.FirmenbuchFnr == company.Fnr)
            .Select(x => new { x.Id, x.Status })
            .FirstOrDefaultAsync(cancellationToken);

        var dto = new MarketingLeadCompanyDto(
            company.Fnr,
            company.Name,
            company.Status,
            company.Euid,
            company.Gegruendet,
            company.Strasse,
            company.Hausnummer,
            company.Plz,
            company.Ort,
            company.Staat,
            company.Sitz,
            company.RechtsformCode,
            company.RechtsformText,
            company.GerichtText,
            company.Handelsregisternummer,
            company.AuszugStand,
            company.AbschluesseVorhanden,
            [.. company.Funktionaere.Select(f => new MarketingLeadOfficerDto(f.Name, f.FunktionText, f.Seit, f.Aktiv))],
            [.. company.Gewerbe.Select(g => new MarketingLeadTradeDto(g.GisaZahl, g.Wortlaut, g.Plz, g.Ort, g.WeitereStandorte, g.Aktiv))]);

        return new GetMarketingLeadCompanyResponse(dto, kontakt?.Id, kontakt?.Status);
    }
}
