namespace Heimatplatz.Api.Features.ForeclosureAuctions.Services;

public static class ForeclosureAuctionConstants
{
    /// <summary>
    /// Deterministische System-User GUID fuer automatisch generierte Properties
    /// </summary>
    public static readonly Guid SystemUserId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

    /// <summary>
    /// Quellsystem-Name fuer Import-Tracking
    /// </summary>
    public const string SourceName = "edikte.justiz.gv.at";
}
