using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.Properties.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Admin.Handlers;

/// <summary>
/// Endgueltiges Loeschen eines Inserats durch den Admin - wie DeletePropertyHandler,
/// aber ohne Ownership-Pruefung (Moderation). Kontakte/Favoriten/Blockierungen haengen
/// per Cascade an der Property; hochgeladene Bilddateien werden mitentfernt.
/// Zwangsversteigerungen besser nur ausblenden: der naechste Edikte-Sync wuerde ein
/// geloeschtes, noch aktives Edikt wieder anlegen (SetPropertyVisibilityRequest).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin")]
public class AdminDeletePropertyHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard,
    IPropertyImageService imageService,
    ILogger<AdminDeletePropertyHandler> logger
) : IRequestHandler<AdminDeletePropertyRequest, AdminDeletePropertyResponse>
{
    [MediatorHttpDelete("/properties/{Id}", OperationId = "AdminDeleteProperty")]
    public async Task<AdminDeletePropertyResponse> Handle(AdminDeletePropertyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var property = await dbContext.Set<Property>()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Property mit ID {request.Id} nicht gefunden");

        dbContext.Set<Property>().Remove(property);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Hochgeladene Bilddateien mitentfernen - sonst bleiben sie unter
        // wwwroot/uploads fuer immer oeffentlich abrufbar liegen (DSGVO).
        // Externe/gescrapte URLs ignoriert DeleteImageAsync selbst.
        foreach (var imageUrl in property.ImageUrls)
        {
            await imageService.DeleteImageAsync(imageUrl, cancellationToken);
        }

        logger.LogInformation(
            "[Admin] Inserat {PropertyId} ({Title}) endgueltig geloescht", property.Id, property.Title);

        return new AdminDeletePropertyResponse(
            Success: true,
            Message: "Immobilie erfolgreich geloescht"
        );
    }
}
