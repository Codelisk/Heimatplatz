using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Heimatplatz.Api.Features.Legal.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;

namespace Heimatplatz.Api.Features.Legal.Services;

/// <inheritdoc cref="IContactInfoProvider" />
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class ContactInfoProvider(AppDbContext dbContext) : IContactInfoProvider
{
    public async Task<LegalContactInfoDto?> GetAsync(CancellationToken cancellationToken = default)
    {
        // Beide Datensaetze in einem Roundtrip - Contact ist optional und fehlt auf
        // Datenbanken, die vor dem Contact-Seeder aufgesetzt wurden
        var settings = await dbContext.Set<LegalSettings>()
            .Where(x => x.IsActive && (x.SettingType == LegalSettingTypes.Imprint || x.SettingType == LegalSettingTypes.Contact))
            .ToListAsync(cancellationToken);

        var imprintJson = settings.FirstOrDefault(x => x.SettingType == LegalSettingTypes.Imprint)?.ResponsiblePartyJson;
        var imprint = LegalJson.Deserialize<ImprintPartyDto>(imprintJson);

        if (imprint == null)
            return null;

        var contactJson = settings.FirstOrDefault(x => x.SettingType == LegalSettingTypes.Contact)?.ResponsiblePartyJson;
        var contact = LegalJson.Deserialize<ContactSettingsDto>(contactJson);

        return ContactInfoFactory.Create(imprint, contact);
    }
}
