using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;

namespace Heimatplatz.Api.Features.Properties.Data.Entities;

/// <summary>
/// Represents a known seller source (broker company or portal platform).
/// Used as a lookup table for sub-filtering by specific broker/portal names.
/// </summary>
public class SellerSource : BaseEntity
{
    /// <summary>Display name of the seller source (e.g. "RE/MAX", "Willhaben")</summary>
    public required string Name { get; set; }

    /// <summary>Whether this is a Broker or Portal source</summary>
    public SellerType SellerType { get; set; }

    /// <summary>Whether this source is included by default in filter results</summary>
    public bool IsDefault { get; set; } = true;
}
