using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Services;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>Manueller Posteingang-Abruf ("Jetzt abrufen") - umgeht die Auto-Sync-Drossel.</summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class SyncMarketingInboxHandler(
    IMarketingInboxSync inboxSync,
    IAdminAccessGuard accessGuard
) : IRequestHandler<SyncMarketingInboxRequest, SyncMarketingInboxResponse>
{
    [MediatorHttpPost("/inbox/sync", OperationId = "SyncMarketingInbox")]
    public async Task<SyncMarketingInboxResponse> Handle(SyncMarketingInboxRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var result = await inboxSync.SyncAsync(force: true, cancellationToken);
        return new SyncMarketingInboxResponse(result.Success, result.Added, result.Error);
    }
}
