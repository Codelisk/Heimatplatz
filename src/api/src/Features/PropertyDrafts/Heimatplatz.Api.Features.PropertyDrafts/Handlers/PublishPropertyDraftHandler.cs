using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Models;
using Heimatplatz.Api.Features.PropertyDrafts.Data.Entities;
using Heimatplatz.Api.Features.PropertyDrafts.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyDrafts.Handlers;

/// <summary>
/// Veroeffentlicht einen Entwurf serverseitig ueber den bestehenden CreateProperty-Flow:
/// volle Validierung, SellerInfoResolver, PropertyChange-Journal und PropertyCreatedEvent
/// feuern dadurch normal. Bei Erfolg wird die Entwurfs-Zeile geloescht, die Medien-Dateien
/// bleiben (die neue Immobilie referenziert sie).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/property-drafts")]
public class PublishPropertyDraftHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IMediator mediator
) : IRequestHandler<PublishPropertyDraftRequest, PublishPropertyDraftResponse>
{
    [MediatorHttpPost("/publish", OperationId = "PublishPropertyDraft", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<PublishPropertyDraftResponse> Handle(
        PublishPropertyDraftRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var draft = await dbContext.Set<PropertyDraft>()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Entwurf mit ID {request.Id} nicht gefunden.");

        if (draft.UserId != userId)
            throw new UnauthorizedAccessException("Sie haben keine Berechtigung fuer diesen Entwurf.");

        var data = DraftPayloadSerializer.Deserialize(draft.PayloadJson);

        // Generierte Beschreibung uebernehmen, falls der Nutzer sie nie in den Payload
        // uebernommen hat (z.B. veroeffentlicht, waehrend/kurz nachdem der Job fertig wurde)
        if (string.IsNullOrWhiteSpace(data.Description) && !string.IsNullOrWhiteSpace(draft.GeneratedDescription))
            data.Description = draft.GeneratedDescription;

        var createRequest = MapToCreateRequest(data);

        var createResult = await mediator.Request(createRequest, cancellationToken);
        var response = createResult.Result;

        // Nur die Entwurfs-Zeile entfernen - KEINE Medien loeschen, die neue
        // Immobilie referenziert die hochgeladenen Bild-URLs.
        dbContext.Set<PropertyDraft>().Remove(draft);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PublishPropertyDraftResponse(response.PropertyId, response.Title, response.CreatedAt);
    }

    /// <summary>
    /// Pflichtfelder klar benannt pruefen (der Payload ist komplett nullable),
    /// alles Weitere (Laengen, Preis-Range, Gemeinde-FK) validiert CreatePropertyHandler.
    /// </summary>
    static CreatePropertyRequest MapToCreateRequest(PropertyDraftData data)
    {
        if (string.IsNullOrWhiteSpace(data.Title))
            throw new ArgumentException("Der Entwurf hat noch keinen Titel.");
        if (string.IsNullOrWhiteSpace(data.Address))
            throw new ArgumentException("Der Entwurf hat noch keine Adresse.");
        if (data.MunicipalityId is not { } municipalityId)
            throw new ArgumentException("Der Entwurf hat noch keine Gemeinde.");
        if (data.Price is not { } price)
            throw new ArgumentException("Der Entwurf hat noch keinen Preis.");
        if (data.Type is not { } type)
            throw new ArgumentException("Der Entwurf hat noch keinen Immobilientyp.");
        if (data.ImageUrls is not { Count: > 0 })
            throw new ArgumentException("Der Entwurf hat noch keine Fotos.");

        // Bei Grundstuecken sind Haus-Felder nicht sinnvoll (gleiches Verhalten wie das
        // bisherige Formular, das sie beim Typwechsel genullt hat)
        var isHouse = type == PropertyType.House;

        return new CreatePropertyRequest(
            Title: data.Title.Trim(),
            Address: data.Address.Trim(),
            MunicipalityId: municipalityId,
            Price: price,
            Type: type,
            Description: data.Description,
            LivingAreaSquareMeters: isHouse ? data.LivingAreaSquareMeters : null,
            PlotAreaSquareMeters: data.PlotAreaSquareMeters,
            Rooms: isHouse ? data.Rooms : null,
            YearBuilt: isHouse ? data.YearBuilt : null,
            Features: data.Features,
            ImageUrls: data.ImageUrls
        );
    }
}
