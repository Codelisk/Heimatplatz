using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Partners.Data.Entities;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Partners.Services;

/// <summary>
/// Live-Inseratszahl je Partner ueber Property.SourceName (z.B. "immobaer.at").
/// Zaehlt nur sichtbare Inserate - gleiche Sichtbarkeitsregel (!IsHidden) wie
/// GetPropertiesHandler, damit die Zahl zur oeffentlichen Suche passt.
/// </summary>
public static class PartnerListingCounts
{
    public static async Task<Dictionary<string, int>> LoadAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<Partner> partners,
        CancellationToken cancellationToken)
    {
        var sourceNames = partners
            .Select(p => p.SourceName)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct()
            .ToList();

        if (sourceNames.Count == 0)
            return new Dictionary<string, int>();

        return await dbContext.Set<Property>()
            .Where(p => !p.IsHidden && p.SourceName != null && sourceNames.Contains(p.SourceName))
            .GroupBy(p => p.SourceName!)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
    }
}
