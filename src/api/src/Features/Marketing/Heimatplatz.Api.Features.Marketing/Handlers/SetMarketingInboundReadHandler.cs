using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>Rueckmeldung als gelesen/ungelesen markieren.</summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class SetMarketingInboundReadHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<SetMarketingInboundReadRequest, SetMarketingInboundReadResponse>
{
    [MediatorHttpPost("/inbox/read", OperationId = "SetMarketingInboundRead")]
    public async Task<SetMarketingInboundReadResponse> Handle(SetMarketingInboundReadRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var inbound = await dbContext.Set<MarketingInboundEmail>()
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);
        if (inbound is null)
            return new SetMarketingInboundReadResponse(false);

        inbound.IsRead = request.IsRead;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SetMarketingInboundReadResponse(true);
    }
}
