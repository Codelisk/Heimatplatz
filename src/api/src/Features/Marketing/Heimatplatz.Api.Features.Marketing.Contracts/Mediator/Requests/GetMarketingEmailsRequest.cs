using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>Versand-Historie (neueste zuerst), optional auf einen Kontakt gefiltert.</summary>
public record GetMarketingEmailsRequest(
    Guid? ContactId,
    int Page = 0,
    int PageSize = 50
) : IRequest<GetMarketingEmailsResponse>;

public record GetMarketingEmailsResponse(
    List<MarketingEmailDto> Emails,
    int Total,
    int PageSize,
    int CurrentPage,
    bool HasMore
);
