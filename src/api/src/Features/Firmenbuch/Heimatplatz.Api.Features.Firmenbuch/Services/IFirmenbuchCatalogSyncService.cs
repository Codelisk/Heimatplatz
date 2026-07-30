namespace Heimatplatz.Api.Features.Firmenbuch.Services;

public interface IFirmenbuchCatalogSyncService
{
    /// <summary>
    /// Spiegelt den kompletten Firmenpool-Katalog in die lokale FirmenbuchCompanies-Tabelle
    /// (Upsert per FNR, loescht nie). Der raeumliche Umfang ergibt sich aus der Quelle -
    /// der Firmenpool fuehrt derzeit Oberoesterreich.
    /// </summary>
    Task<FirmenbuchCatalogSyncResult> SyncAsync(CancellationToken ct = default);
}

public record FirmenbuchCatalogSyncResult(
    int Created,
    int Updated,
    int Unchanged,
    int PagesFetched,
    int Errors,
    List<string> ErrorMessages);
