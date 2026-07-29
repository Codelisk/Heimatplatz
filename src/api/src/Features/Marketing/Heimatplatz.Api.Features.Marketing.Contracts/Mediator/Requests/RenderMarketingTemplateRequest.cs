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

/// <summary>
/// <see cref="Warnings"/>: alles, was beim Befuellen nicht sauber aufging (fehlende
/// Kontaktdaten, Anrede-Fallback, unbekannte Platzhalter). Nicht befuellbare Platzhalter
/// bleiben sichtbar im Text stehen - stumm leere Luecken waren die fehleranfaelligste
/// Stelle des alten Renderers.
/// </summary>
public record RenderMarketingTemplateResponse(
    bool Success,
    string? Subject,
    string? Body,
    string? Error,
    IReadOnlyList<string>? Warnings = null
);
