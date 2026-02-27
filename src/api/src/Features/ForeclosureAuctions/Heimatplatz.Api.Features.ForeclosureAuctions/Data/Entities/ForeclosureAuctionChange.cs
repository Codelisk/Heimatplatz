using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;

/// <summary>
/// Aenderungs-Log fuer Zwangsversteigerungen (Change-Detection)
/// </summary>
public class ForeclosureAuctionChange : BaseEntity
{
    /// <summary>FK zur Zwangsversteigerung</summary>
    public required Guid ForeclosureAuctionId { get; set; }

    /// <summary>Art der Aenderung: Created, Updated, Removed, Reappeared</summary>
    public required string ChangeType { get; set; }

    /// <summary>JSON-Diff der geaenderten Felder (bei Updates)</summary>
    public string? ChangedFields { get; set; }

    /// <summary>Content-Hash vor der Aenderung</summary>
    public string? OldContentHash { get; set; }

    /// <summary>Content-Hash nach der Aenderung</summary>
    public string? NewContentHash { get; set; }

    // === Navigation Properties ===
    public ForeclosureAuction ForeclosureAuction { get; set; } = null!;
}
