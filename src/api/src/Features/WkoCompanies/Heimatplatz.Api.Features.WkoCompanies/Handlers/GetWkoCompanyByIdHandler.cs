using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.WkoCompanies.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.WkoCompanies.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.WkoCompanies.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/wko-companies")]
public class GetWkoCompanyByIdHandler(AppDbContext dbContext)
    : IRequestHandler<GetWkoCompanyByIdRequest, GetWkoCompanyByIdResponse>
{
    [MediatorHttpGet("/{Id}", OperationId = "GetWkoCompanyById")]
    public async Task<GetWkoCompanyByIdResponse> Handle(
        GetWkoCompanyByIdRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Set<WkoCompany>()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        return new GetWkoCompanyByIdResponse
        {
            Company = entity == null ? null : GetWkoCompaniesHandler.MapToDto(entity)
        };
    }
}
