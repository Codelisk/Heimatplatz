namespace Heimatplatz.Api.Features.SrealListings.Services;

public interface ISrealSyncService
{
    Task<SrealSyncResult> SyncAllAsync(CancellationToken ct = default);
}

public record SrealSyncResult(
    int Created,
    int Updated,
    int Removed,
    int Unchanged,
    int Errors,
    List<string> ErrorMessages
);
