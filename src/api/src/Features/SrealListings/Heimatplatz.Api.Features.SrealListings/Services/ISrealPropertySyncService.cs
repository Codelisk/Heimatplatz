namespace Heimatplatz.Api.Features.SrealListings.Services;

public interface ISrealPropertySyncService
{
    Task<SrealPropertySyncResult> SyncToPropertiesAsync(CancellationToken ct = default);
}

public record SrealPropertySyncResult(
    int Created,
    int Updated,
    int Removed,
    int Skipped,
    int Errors,
    List<string> ErrorMessages
);
