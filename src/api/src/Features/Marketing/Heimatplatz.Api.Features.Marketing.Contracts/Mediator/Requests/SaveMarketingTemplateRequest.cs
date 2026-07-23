using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Vorlage anlegen (Id=null) oder bearbeiten (Id gesetzt). Der Name ist eindeutig;
/// Dubletten liefern Success=false + Error. Betreff und Text duerfen Platzhalter
/// enthalten (siehe MarketingTemplatePlaceholders) - unbekannte Platzhalter bleiben
/// beim Rendern unveraendert stehen und fallen so im Editor auf.
/// Bewusst komplett im Body (kein Route-Parameter).
/// </summary>
public record SaveMarketingTemplateRequest(
    Guid? Id,
    string Name,
    string? Description,
    string Subject,
    string Body,
    bool IsActive = true,
    int DisplayOrder = 0
) : IRequest<SaveMarketingTemplateResponse>;

public record SaveMarketingTemplateResponse(bool Success, Guid? Id, string? Error);
