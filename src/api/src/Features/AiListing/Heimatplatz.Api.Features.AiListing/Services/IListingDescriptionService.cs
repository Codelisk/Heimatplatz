using Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;

namespace Heimatplatz.Api.Features.AiListing.Services;

/// <summary>
/// Generiert eine Inserats-Beschreibung aus Eckdaten, Stichwoertern und Foto-URLs.
/// Implementierungen: AiConnectorListingDescriptionService (externer KI-Backend-Service),
/// MockListingDescriptionService (Dev-Template ohne KI).
/// Der Wortbereich kommt aus AiListingOptions.Description (fix im Backend definiert).
/// </summary>
public interface IListingDescriptionService
{
    Task<string> GenerateAsync(GenerateListingDescriptionRequest input, CancellationToken ct = default);
}
