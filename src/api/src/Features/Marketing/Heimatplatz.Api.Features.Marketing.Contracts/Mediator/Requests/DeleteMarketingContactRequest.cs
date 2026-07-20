using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Loescht einen Kontakt samt Versand-Historie und Rueckmeldungen (DSGVO-Kaskade).
/// </summary>
public record DeleteMarketingContactRequest(Guid Id) : IRequest<DeleteMarketingContactResponse>;

public record DeleteMarketingContactResponse(bool Success);
