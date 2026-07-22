using System.Net.Mail;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Heimatplatz.Api.Features.Legal.Data.Entities;
using Heimatplatz.Api.Features.Legal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Legal.Handlers;

/// <summary>
/// Speichert die Firmendaten des Impressums (ECG §5 / UGB §14). Die Rechtstext-Abschnitte
/// bleiben unveraendert - hier geht es um Stammdaten, nicht um die Textbausteine.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/legal")]
public class UpdateImprintPartyHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard,
    ILogger<UpdateImprintPartyHandler> logger
) : IRequestHandler<UpdateImprintPartyRequest, UpdateImprintPartyResponse>
{
    [MediatorHttpPost("/imprint", OperationId = "UpdateImprintParty")]
    public async Task<UpdateImprintPartyResponse> Handle(UpdateImprintPartyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        // Pflichtangaben: ein leeres Impressum waere ein Rechtsverstoss, deshalb hier
        // strenger als beim Contact-Datensatz
        foreach (var (label, value) in RequiredFields(request))
        {
            if (string.IsNullOrWhiteSpace(value))
                return Failed($"Pflichtangabe fehlt: {label}.");
        }

        if (!MailAddress.TryCreate(request.Email.Trim(), out _))
            return Failed("Die E-Mail-Adresse ist ungültig.");

        if (InvalidUrl(request.Website))
            return Failed("Die Website muss mit http:// oder https:// beginnen.");

        var entity = await dbContext.Set<LegalSettings>()
            .FirstOrDefaultAsync(x => x.SettingType == LegalSettingTypes.Imprint && x.IsActive, cancellationToken);

        if (entity == null)
            return Failed("Es ist kein aktives Impressum vorhanden. Bitte zuerst die API neu starten, damit der Seeder es anlegt.");

        var party = new ImprintPartyDto(
            CompanyName: request.CompanyName.Trim(),
            LegalForm: request.LegalForm.Trim(),
            Owner: request.Owner.Trim(),
            Street: request.Street.Trim(),
            PostalCode: request.PostalCode.Trim(),
            City: request.City.Trim(),
            Country: request.Country.Trim(),
            Email: request.Email.Trim(),
            Phone: Normalize(request.Phone),
            Website: Normalize(request.Website),
            UidNumber: request.UidNumber.Trim(),
            TaxNumber: request.TaxNumber.Trim(),
            DunsNumber: Normalize(request.DunsNumber),
            Gln: Normalize(request.Gln),
            GisaNumber: Normalize(request.GisaNumber),
            Trade: request.Trade.Trim(),
            TradeAuthority: request.TradeAuthority.Trim(),
            ProfessionalLaw: request.ProfessionalLaw.Trim(),
            ChamberMembership: Normalize(request.ChamberMembership),
            TradeGroup: Normalize(request.TradeGroup)
        );

        entity.ResponsiblePartyJson = LegalJson.Serialize(party);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[Admin] Impressum-Stammdaten aktualisiert ({CompanyName})", party.CompanyName);

        var sections = LegalJson.Deserialize<List<LegalSectionDto>>(entity.SectionsJson) ?? [];

        var imprint = new ImprintDto(
            party.CompanyName, party.LegalForm, party.Owner,
            party.Street, party.PostalCode, party.City, party.Country,
            party.Email, party.Phone, PhoneNumberFormatter.ToTelLink(party.Phone), party.Website,
            party.UidNumber, party.TaxNumber, party.DunsNumber, party.Gln, party.GisaNumber,
            party.Trade, party.TradeAuthority, party.ProfessionalLaw,
            party.ChamberMembership, party.TradeGroup,
            sections,
            entity.Version,
            entity.EffectiveDate,
            entity.UpdatedAt ?? entity.CreatedAt
        );

        return new UpdateImprintPartyResponse(true, null, imprint);
    }

    private static IEnumerable<(string Label, string? Value)> RequiredFields(UpdateImprintPartyRequest request)
    {
        yield return ("Firmenname", request.CompanyName);
        yield return ("Rechtsform", request.LegalForm);
        yield return ("Inhaber", request.Owner);
        yield return ("Straße", request.Street);
        yield return ("PLZ", request.PostalCode);
        yield return ("Ort", request.City);
        yield return ("Land", request.Country);
        yield return ("E-Mail", request.Email);
        yield return ("UID-Nummer", request.UidNumber);
        yield return ("Steuernummer", request.TaxNumber);
        yield return ("Gewerbe", request.Trade);
        yield return ("Gewerbebehörde", request.TradeAuthority);
        yield return ("Berufsrecht", request.ProfessionalLaw);
    }

    private static UpdateImprintPartyResponse Failed(string error)
        => new(false, error, null);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool InvalidUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps);
    }
}
