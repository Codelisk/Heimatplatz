namespace Heimatplatz.Api.Features.ForeclosureAuctions.Services;

public interface IForeclosurePropertySyncService
{
    Task<PropertySyncResult> SyncToPropertiesAsync(CancellationToken ct = default);
}

public record PropertySyncResult(
    int Created,
    int Updated,
    int Removed,
    int Skipped,
    int Errors,
    List<string> ErrorMessages
);
