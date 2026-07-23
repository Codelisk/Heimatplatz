using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Fuellt die Platzhalter einer Vorlage aus einem Kontakt und liefert den fertigen
/// Entwurf. Ersetzung passiert bewusst serverseitig (Backend-First), damit Anrede-Regeln
/// nur an einer Stelle stehen. ContactId leer = neutrale Vorschau mit Anrede-Fallback.
/// </summary>
public record RenderMarketingTemplateRequest(
    Guid TemplateId,
    Guid? ContactId = null
) : IRequest<RenderMarketingTemplateResponse>;

public record RenderMarketingTemplateResponse(
    bool Success,
    string? Subject,
    string? Body,
    string? Error
);
