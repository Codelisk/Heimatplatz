using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Kontakt-Detail mit Timeline (gesendete Mails, Rueckmeldungen und Historien-Eintraege
/// wie Anrufe/Notizen/Statuswechsel - jeweils neueste zuerst).
/// </summary>
public record GetMarketingContactDetailRequest(Guid Id) : IRequest<GetMarketingContactDetailResponse>;

public record GetMarketingContactDetailResponse(
    MarketingContactDto? Contact,
    List<MarketingEmailDto> Emails,
    List<MarketingInboundEmailDto> Replies,
    List<MarketingActivityDto> Activities
);
