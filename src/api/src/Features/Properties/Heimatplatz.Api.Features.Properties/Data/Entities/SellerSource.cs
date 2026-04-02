using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Properties.Data.Entities;

/// <summary>
/// Represents a known seller source (broker company).
/// Used as a lookup table for sub-filtering by specific broker names.
/// </summary>
public class SellerSource : BaseEntity
{
    /// <summary>Display name of the seller source (e.g. "RE/MAX")</summary>
    public required string Name { get; set; }

    /// <summary>Whether this source is included by default in filter results</summary>
    public bool IsDefault { get; set; } = true;
}
