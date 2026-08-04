using Heimatplatz.Api.Features.Partners.Contracts.Models;
using Heimatplatz.Api.Features.Partners.Data.Entities;

namespace Heimatplatz.Api.Features.Partners.Services;

public static class PartnerMapper
{
    public static PartnerDto ToDto(Partner partner, IReadOnlyDictionary<string, int> listingCounts)
        => new(
            partner.Id,
            partner.Name,
            partner.Category,
            partner.Description,
            partner.WebsiteUrl,
            partner.LogoUrl,
            partner.Region,
            partner.PartnerSinceYear,
            partner.SourceName,
            partner.SellerName,
            partner.DisplayOrder,
            partner.IsVisible,
            partner.SourceName != null && listingCounts.TryGetValue(partner.SourceName, out var count) ? count : 0
        );
}
