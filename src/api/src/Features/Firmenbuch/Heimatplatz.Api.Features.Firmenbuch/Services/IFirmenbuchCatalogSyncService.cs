namespace Heimatplatz.Api.Features.Firmenbuch.Services;

public interface IFirmenbuchCatalogSyncService
{
    /// <summary>
    /// Baut den Firmenbuch-Katalog fuer den angegebenen ORTNR-Bereich auf bzw. aktualisiert ihn
    /// (1-stellig Bundesland, 3-stellig Bezirk, 5-stellig Gemeinde, leer = ganz Oesterreich).
    /// </summary>
    Task<FirmenbuchCatalogSyncResult> SyncAsync(string ortNr, CancellationToken ct = default);
}

public record FirmenbuchCatalogSyncResult(
    int Created,
    int Updated,
    int Unchanged,
    int QueriesExecuted,
    int CappedPrefixes,
    int Errors,
    List<string> ErrorMessages);
