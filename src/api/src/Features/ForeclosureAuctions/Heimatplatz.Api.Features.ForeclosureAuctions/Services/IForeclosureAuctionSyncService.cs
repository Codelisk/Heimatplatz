namespace Heimatplatz.Api.Features.ForeclosureAuctions.Services;

public interface IForeclosureAuctionSyncService
{
    Task<SyncResult> SyncAllAsync(CancellationToken ct = default);
}

public record SyncResult(
    int Created,
    int Updated,
    int Removed,
    int Unchanged,
    int Errors,
    List<string> ErrorMessages
);
