using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Heimatplatz.Api.Features.Legal.Data.Entities;
using Heimatplatz.Api.Features.Legal.Services;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Legal.Handlers;

/// <summary>
/// Handler fuer GetPrivacyPolicyRequest - gibt die aktive Datenschutzerklaerung zurueck.
///
/// Der Verantwortliche wird aus dem IMPRESSUM abgeleitet, nicht aus dem gespeicherten
/// PrivacyPolicy-Datensatz: es ist dieselbe Firma, und zwei Kopien liefen zwangslaeufig
/// auseinander (eine Adressaenderung im Impressum liess /datenschutz auf dem alten Stand).
/// Aus dem eigenen Datensatz kommen nur noch die Rechtstext-Abschnitte und der
/// Datenschutzbeauftragte - Angaben, die es im Impressum nicht gibt.
///
/// Der frueher hier eingebaute On-Demand-Seed ist entfernt: er war eine dritte Kopie der
/// Firmendaten (mit ASCII-Umlauten) und hat je nach Aufrufreihenfolge andere Werte in die
/// DB geschrieben als der LegalSettingsSeeder. Geseedet wird ausschliesslich beim Start.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/legal")]
public class GetPrivacyPolicyHandler(AppDbContext dbContext) : IRequestHandler<GetPrivacyPolicyRequest, GetPrivacyPolicyResponse>
{
    [MediatorHttpGet("/privacy-policy", OperationId = "GetPrivacyPolicy")]
    public async Task<GetPrivacyPolicyResponse> Handle(GetPrivacyPolicyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var records = await dbContext.Set<LegalSettings>()
            .Where(x => x.IsActive && (x.SettingType == LegalSettingTypes.PrivacyPolicy || x.SettingType == LegalSettingTypes.Imprint))
            .ToListAsync(cancellationToken);

        var settings = records.FirstOrDefault(x => x.SettingType == LegalSettingTypes.PrivacyPolicy);

        if (settings == null)
            return new GetPrivacyPolicyResponse(null);

        var stored = LegalJson.Deserialize<ResponsiblePartyDto>(settings.ResponsiblePartyJson);
        var imprintJson = records.FirstOrDefault(x => x.SettingType == LegalSettingTypes.Imprint)?.ResponsiblePartyJson;
        var imprint = LegalJson.Deserialize<ImprintPartyDto>(imprintJson);

        // Ohne Impressum bleibt der gespeicherte Datensatz die Rueckfallebene
        var responsibleParty = imprint == null
            ? stored
            : new ResponsiblePartyDto(
                CompanyName: imprint.CompanyName,
                Street: imprint.Street,
                PostalCode: imprint.PostalCode,
                City: imprint.City,
                Country: imprint.Country,
                Email: imprint.Email,
                Phone: imprint.Phone,
                DataProtectionOfficer: stored?.DataProtectionOfficer);

        if (responsibleParty == null)
            return new GetPrivacyPolicyResponse(null);

        var sections = LegalJson.Deserialize<List<LegalSectionDto>>(settings.SectionsJson) ?? [];

        var privacyPolicy = new PrivacyPolicyDto(
            responsibleParty,
            PhoneNumberFormatter.ToTelLink(responsibleParty.Phone),
            sections,
            settings.Version,
            settings.EffectiveDate,
            settings.UpdatedAt ?? settings.CreatedAt
        );

        return new GetPrivacyPolicyResponse(privacyPolicy);
    }
}
