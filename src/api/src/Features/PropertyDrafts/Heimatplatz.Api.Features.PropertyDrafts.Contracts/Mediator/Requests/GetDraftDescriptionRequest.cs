using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyDrafts.Contracts.Mediator.Requests;

/// <summary>
/// Leichtgewichtiger Polling-Endpoint fuer den Fortschritt der KI-Beschreibungs-Generierung
/// eines Entwurfs (laeuft als Hintergrund-Job, dauert einige Minuten).
/// </summary>
public record GetDraftDescriptionRequest(
    Guid Id
) : IRequest<GetDraftDescriptionResponse>;

/// <summary>
/// Aktueller Generierungs-Zustand; GeneratedText ist nur bei Status Finished gesetzt,
/// ErrorMessage nur bei Status Failed.
/// </summary>
public record GetDraftDescriptionResponse(
    DraftDescriptionStatus Status,
    string? GeneratedText,
    string? ErrorMessage
);
