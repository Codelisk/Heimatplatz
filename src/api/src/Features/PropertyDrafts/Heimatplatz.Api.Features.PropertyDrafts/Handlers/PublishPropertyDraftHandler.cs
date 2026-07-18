using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
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

        // Laeuft der Beschreibungs-Job noch, blockiert das die Veroeffentlichung NICHT:
        // die Immobilie geht mit Platzhalter-Text live, der Job liefert den fertigen
        // Text nach (PublishedPropertyId + DraftDescriptionProcessor).
        var descriptionPending = string.IsNullOrWhiteSpace(data.Description)
            && draft.DescriptionStatus is DraftDescriptionStatus.Queued or DraftDescriptionStatus.InProgress;
        if (descriptionPending)
            data.Description = DraftDescriptionPlaceholder.Text;

        var createRequest = MapToCreateRequest(data);

        var createResult = await mediator.Request(createRequest, cancellationToken);
        var response = createResult.Result;

        if (descriptionPending)
        {
            // Entwurf behalten, damit der laufende Job sein Ziel findet - er ist ueber
            // PublishedPropertyId aus der Entwurfs-Liste ausgeblendet und wird nach
            // der Text-Nachlieferung geloescht.
            draft.PublishedPropertyId = response.PropertyId;
            await dbContext.SaveChangesAsync(cancellationToken);

            // Der Job kann GENAU waehrend des Publish fertig geworden sein - seine
            // Nachlieferungs-Pruefung sah PublishedPropertyId dann noch nicht. Nach dem
            // eigenen Commit frisch nachsehen (Spiegelbild zur Pruefung im Processor):
            await dbContext.Entry(draft).ReloadAsync(cancellationToken);
            if (draft.DescriptionStatus == DraftDescriptionStatus.Finished
                && !string.IsNullOrWhiteSpace(draft.GeneratedDescription))
            {
                var property = await dbContext.Set<Property>()
                    .FirstOrDefaultAsync(p => p.Id == response.PropertyId, cancellationToken);
                if (property is not null && property.Description == DraftDescriptionPlaceholder.Text)
                {
                    var text = draft.GeneratedDescription;
                    property.Description = text.Length > 2000 ? text[..2000] : text;
                }

                dbContext.Set<PropertyDraft>().Remove(draft);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            // Nur die Entwurfs-Zeile entfernen - KEINE Medien loeschen, die neue
            // Immobilie referenziert die hochgeladenen Bild-URLs.
            dbContext.Set<PropertyDraft>().Remove(draft);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

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
            ImageUrls: data.ImageUrls,
            OriginalListingUrl: data.OriginalListingUrl
        );
    }
}
