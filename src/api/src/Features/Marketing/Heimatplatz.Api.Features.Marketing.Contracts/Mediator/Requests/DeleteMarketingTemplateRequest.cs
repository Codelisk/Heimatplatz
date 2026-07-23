using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>Loescht eine E-Mail-Vorlage. Bereits versendete Mails bleiben unberuehrt.</summary>
public record DeleteMarketingTemplateRequest(Guid Id) : IRequest<DeleteMarketingTemplateResponse>;

public record DeleteMarketingTemplateResponse(bool Success);
