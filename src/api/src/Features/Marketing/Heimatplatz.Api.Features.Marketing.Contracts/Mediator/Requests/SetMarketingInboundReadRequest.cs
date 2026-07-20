using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>Rueckmeldung als gelesen/ungelesen markieren. Bewusst komplett im Body.</summary>
public record SetMarketingInboundReadRequest(Guid Id, bool IsRead) : IRequest<SetMarketingInboundReadResponse>;

public record SetMarketingInboundReadResponse(bool Success);
