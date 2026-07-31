namespace Heimatplatz.Api.Features.OpenImmoImport.Services;

public static class OpenImmoImportConstants
{
    /// <summary>
    /// Deterministische System-User GUID fuer importierte Properties. BEWUSST dieselbe
    /// GUID wie ForeclosureAuctionConstants.SystemUserId ("System Heimatplatz",
    /// system@heimatplatz.at): ein gemeinsamer System-User fuer alle automatischen
    /// Importe, ohne Projekt-Referenz auf ForeclosureAuctions.
    /// </summary>
    public static readonly Guid SystemUserId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
}
