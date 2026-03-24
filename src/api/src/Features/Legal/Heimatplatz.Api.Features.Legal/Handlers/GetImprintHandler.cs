using System.Text.Json;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Heimatplatz.Api.Features.Legal.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Legal.Handlers;

/// <summary>
/// Handler fuer GetImprintRequest - gibt das aktive Impressum zurueck
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/legal")]
public class GetImprintHandler(AppDbContext dbContext) : IRequestHandler<GetImprintRequest, GetImprintResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [MediatorHttpGet("/imprint", OperationId = "GetImprint")]
    public async Task<GetImprintResponse> Handle(GetImprintRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var settings = await dbContext.Set<LegalSettings>()
            .Where(x => x.SettingType == "Imprint" && x.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings == null)
        {
            return new GetImprintResponse(null);
        }

        var party = settings.ResponsiblePartyJson != null
            ? JsonSerializer.Deserialize<ImprintPartyDto>(settings.ResponsiblePartyJson, JsonOptions)
            : null;

        if (party == null)
        {
            return new GetImprintResponse(null);
        }

        var sections = settings.SectionsJson != null
            ? JsonSerializer.Deserialize<List<LegalSectionDto>>(settings.SectionsJson, JsonOptions)
            : [];

        var imprint = new ImprintDto(
            party.CompanyName,
            party.LegalForm,
            party.Owner,
            party.Street,
            party.PostalCode,
            party.City,
            party.Country,
            party.Email,
            party.Phone,
            party.Website,
            party.UidNumber,
            party.TaxNumber,
            party.DunsNumber,
            party.Gln,
            party.GisaNumber,
            party.Trade,
            party.TradeAuthority,
            party.ProfessionalLaw,
            party.ChamberMembership,
            party.TradeGroup,
            sections ?? [],
            settings.Version,
            settings.EffectiveDate,
            settings.UpdatedAt ?? settings.CreatedAt
        );

        return new GetImprintResponse(imprint);
    }
}
