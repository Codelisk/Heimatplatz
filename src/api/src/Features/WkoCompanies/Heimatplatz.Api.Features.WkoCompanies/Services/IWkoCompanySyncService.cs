namespace Heimatplatz.Api.Features.WkoCompanies.Services;

public interface IWkoCompanySyncService
{
    Task<WkoCompanySyncResult> SyncAllAsync(CancellationToken ct = default);
}

public record WkoCompanySyncResult(
    int Created,
    int Updated,
    int Removed,
    int Unchanged,
    int Errors,
    List<string> ErrorMessages
);
