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
/// Handler fuer GetPrivacyPolicyRequest - gibt die aktive Datenschutzerklaerung zurueck
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/legal")]
public class GetPrivacyPolicyHandler(AppDbContext dbContext) : IRequestHandler<GetPrivacyPolicyRequest, GetPrivacyPolicyResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [MediatorHttpGet("/privacy-policy", OperationId = "GetPrivacyPolicy")]
    public async Task<GetPrivacyPolicyResponse> Handle(GetPrivacyPolicyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var settings = await dbContext.Set<LegalSettings>()
            .Where(x => x.SettingType == "PrivacyPolicy" && x.IsActive)
            .OrderByDescending(x => x.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings == null)
        {
            return new GetPrivacyPolicyResponse(null);
        }

        var responsibleParty = settings.ResponsiblePartyJson != null
            ? JsonSerializer.Deserialize<ResponsiblePartyDto>(settings.ResponsiblePartyJson, JsonOptions)
            : null;

        var sections = settings.SectionsJson != null
            ? JsonSerializer.Deserialize<List<LegalSectionDto>>(settings.SectionsJson, JsonOptions)
            : new List<LegalSectionDto>();

        if (responsibleParty == null)
        {
            return new GetPrivacyPolicyResponse(null);
        }

        var privacyPolicy = new PrivacyPolicyDto(
            responsibleParty,
            sections ?? new List<LegalSectionDto>(),
            settings.Version,
            settings.EffectiveDate,
            settings.UpdatedAt ?? settings.CreatedAt
        );

        return new GetPrivacyPolicyResponse(privacyPolicy);
    }
}
