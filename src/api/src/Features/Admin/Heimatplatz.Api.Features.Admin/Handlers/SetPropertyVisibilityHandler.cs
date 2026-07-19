using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Admin.Handlers;

/// <summary>
/// Blendet ein Inserat aus bzw. wieder ein (Property.IsHidden). Der
/// PropertyChangeInterceptor bildet den Uebergang im Delta-Sync-Journal als
/// Deleted- bzw. Created-Eintrag ab, damit MAUI-Clients ihren Cache aufraeumen.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin")]
public class SetPropertyVisibilityHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard,
    ILogger<SetPropertyVisibilityHandler> logger
) : IRequestHandler<SetPropertyVisibilityRequest, SetPropertyVisibilityResponse>
{
    [MediatorHttpPost("/properties/visibility", OperationId = "SetAdminPropertyVisibility")]
    public async Task<SetPropertyVisibilityResponse> Handle(SetPropertyVisibilityRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var property = await dbContext.Set<Property>()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Property mit ID {request.Id} nicht gefunden");

        if (property.IsHidden != request.Hidden)
        {
            property.IsHidden = request.Hidden;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "[Admin] Inserat {PropertyId} ({Title}) {Action}",
                property.Id, property.Title, request.Hidden ? "ausgeblendet" : "wieder eingeblendet");
        }

        return new SetPropertyVisibilityResponse(Success: true, IsHidden: property.IsHidden);
    }
}
