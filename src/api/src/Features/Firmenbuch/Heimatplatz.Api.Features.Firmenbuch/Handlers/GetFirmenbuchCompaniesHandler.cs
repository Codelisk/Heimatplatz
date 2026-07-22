using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Firmenbuch.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Firmenbuch.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Firmenbuch.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/firmenbuch")]
public class GetFirmenbuchCompaniesHandler(AppDbContext dbContext)
    : IRequestHandler<GetFirmenbuchCompaniesRequest, GetFirmenbuchCompaniesResponse>
{
    private const int MaxPageSize = 200;

    [MediatorHttpGet("/companies", OperationId = "GetFirmenbuchCompanies")]
    public async Task<GetFirmenbuchCompaniesResponse> Handle(
        GetFirmenbuchCompaniesRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<FirmenbuchCompany>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchText))
            query = query.Where(c => c.Name.Contains(request.SearchText) || c.Fnr == request.SearchText);

        if (!string.IsNullOrWhiteSpace(request.Sitz))
            query = query.Where(c => c.Sitz != null && c.Sitz.Contains(request.Sitz));

        // "aufrecht" ist in der Schnittstelle der leere Status - als eigener Filterwert abgebildet
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = request.Status == "aufrecht"
                ? query.Where(c => c.Status == null)
                : query.Where(c => c.Status == request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.RechtsformCode))
            query = query.Where(c => c.RechtsformCode == request.RechtsformCode);

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new GetFirmenbuchCompaniesResponse
        {
            Companies = entities.Select(c => new FirmenbuchCompanyDto
            {
                Id = c.Id,
                Fnr = c.Fnr,
                Name = c.Name,
                Status = c.Status,
                Sitz = c.Sitz,
                RechtsformCode = c.RechtsformCode,
                RechtsformText = c.RechtsformText,
                Rechtseigenschaft = c.Rechtseigenschaft,
                GerichtCode = c.GerichtCode,
                GerichtText = c.GerichtText,
                SourceOrtNr = c.SourceOrtNr,
                FirstSeenAt = c.FirstSeenAt,
                LastSeenAt = c.LastSeenAt
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
