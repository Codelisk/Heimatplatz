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
public class GetWkoCompaniesHandler(AppDbContext dbContext)
    : IRequestHandler<GetWkoCompaniesRequest, GetWkoCompaniesResponse>
{
    // Obergrenze pro Seite: schuetzt vor PageSize=1000000-Anfragen
    private const int MaxPageSize = 200;

    [MediatorHttpGet("/", OperationId = "GetWkoCompanies")]
    public async Task<GetWkoCompaniesResponse> Handle(
        GetWkoCompaniesRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<WkoCompany>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.City))
            query = query.Where(c => c.City != null && c.City.Contains(request.City));

        if (!string.IsNullOrWhiteSpace(request.PostalCode))
            query = query.Where(c => c.PostalCode != null && c.PostalCode.StartsWith(request.PostalCode));

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            query = query.Where(c =>
                c.Name.Contains(request.SearchText)
                || (c.CategoryText != null && c.CategoryText.Contains(request.SearchText)));
        }

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        // Inkrementelle Abfragen: "alle Firmen ab Datum X" - wahlweise nach Gruendungsdatum
        // (FoundedDate, fruehestes Berechtigungs-Datum) oder nach Scrape-Zeitpunkt (FirstSeenAt).
        if (request.FoundedFrom.HasValue)
        {
            var foundedFrom = request.FoundedFrom.Value.ToUniversalTime();
            query = query.Where(c => c.FoundedDate >= foundedFrom);
        }

        if (request.FirstSeenFrom.HasValue)
        {
            var firstSeenFrom = request.FirstSeenFrom.Value.ToUniversalTime();
            query = query.Where(c => c.FirstSeenAt >= firstSeenFrom);
        }

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id) // stabiler Tiebreaker fuer deterministisches Paging
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new GetWkoCompaniesResponse
        {
            Companies = entities.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    internal static WkoCompanyDto MapToDto(WkoCompany c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        CategoryText = c.CategoryText,
        Street = c.Street,
        PostalCode = c.PostalCode,
        City = c.City,
        Phones = c.Phones,
        Email = c.Email,
        Website = c.Website,
        OpeningHoursText = c.OpeningHoursText,
        CompanyRegisterNumber = c.CompanyRegisterNumber,
        CompanyCourt = c.CompanyCourt,
        Gln = c.Gln,
        LegalForm = c.LegalForm,
        FoundedYear = c.FoundedYear,
        FoundedDate = c.FoundedDate,
        IsTrainingCompany = c.IsTrainingCompany,
        Permits = c.Permits.Select(p => new WkoCompanyPermitDto
        {
            FachgruppeName = p.FachgruppeName,
            Description = p.Description,
            ManagingDirector = p.ManagingDirector,
            GisaNumber = p.GisaNumber,
            Since = p.Since
        }).ToList(),
        CreatedAt = c.CreatedAt,
        WkoFirmaId = c.WkoFirmaId,
        DetailUrl = c.DetailUrl,
        SourceSearchTerm = c.SourceSearchTerm,
        IsActive = c.IsActive,
        FirstSeenAt = c.FirstSeenAt,
        LastScrapedAt = c.LastScrapedAt,
        RemovedAt = c.RemovedAt
    };
}
