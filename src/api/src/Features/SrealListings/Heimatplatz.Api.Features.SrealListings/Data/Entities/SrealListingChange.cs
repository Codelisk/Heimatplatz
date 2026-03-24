using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.SrealListings.Data.Entities;

/// <summary>
/// Aenderungs-Log fuer SrealListings (Change-Detection)
/// </summary>
public class SrealListingChange : BaseEntity
{
    /// <summary>FK zum SrealListing</summary>
    public required Guid SrealListingId { get; set; }

    /// <summary>Art der Aenderung: Created, Updated, Removed, Reappeared</summary>
    public required string ChangeType { get; set; }

    /// <summary>JSON-Diff der geaenderten Felder (bei Updates)</summary>
    public string? ChangedFields { get; set; }

    /// <summary>Content-Hash vor der Aenderung</summary>
    public string? OldContentHash { get; set; }

    /// <summary>Content-Hash nach der Aenderung</summary>
    public string? NewContentHash { get; set; }

    // === Navigation Properties ===
    public SrealListing SrealListing { get; set; } = null!;
}
