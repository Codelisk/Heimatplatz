using Shiny.Mediator;

namespace Heimatplatz.Api.Features.WkoCompanies.Contracts.Mediator.Requests;

public record GetWkoCompanyByIdRequest(Guid Id) : IRequest<GetWkoCompanyByIdResponse>;

public record GetWkoCompanyByIdResponse
{
    public WkoCompanyDto? Company { get; init; }
}
