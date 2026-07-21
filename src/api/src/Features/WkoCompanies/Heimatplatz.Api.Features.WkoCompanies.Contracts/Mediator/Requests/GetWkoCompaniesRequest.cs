using Shiny.Mediator;

namespace Heimatplatz.Api.Features.WkoCompanies.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Abrufen aller WKO-Firmen mit optionalen Filtern
/// </summary>
public record GetWkoCompaniesRequest(
    int Page = 1,
    int PageSize = 25,
    string? City = null,
    string? PostalCode = null,
    string? SearchText = null,
    bool? IsActive = null
) : IRequest<GetWkoCompaniesResponse>;

/// <summary>
/// Response mit WKO-Firmen (paginiert)
/// </summary>
public record GetWkoCompaniesResponse
{
    public required List<WkoCompanyDto> Companies { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

/// <summary>
/// DTO fuer eine Gewerbeberechtigung (Eintrag im Abschnitt "Berechtigungen" der Detailseite)
/// </summary>
public record WkoCompanyPermitDto
{
    public string? FachgruppeName { get; init; }
    public string? Description { get; init; }
    public string? ManagingDirector { get; init; }
    public string? GisaNumber { get; init; }
}

/// <summary>
/// DTO fuer WKO-Firmen-Details
/// </summary>
public record WkoCompanyDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? CategoryText { get; init; }

    // Adressdaten
    public string? Street { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }

    // Kontaktdaten
    public List<string> Phones { get; init; } = [];
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? OpeningHoursText { get; init; }

    // Firmendaten laut Gewerbedatenbank
    public string? CompanyRegisterNumber { get; init; }
    public string? CompanyCourt { get; init; }
    public string? Gln { get; init; }
    public string? LegalForm { get; init; }
    public int? FoundedYear { get; init; }
    public bool IsTrainingCompany { get; init; }

    public List<WkoCompanyPermitDto> Permits { get; init; } = [];

    public required DateTimeOffset CreatedAt { get; init; }

    // Scraping-Daten
    public required Guid WkoFirmaId { get; init; }
    public required string DetailUrl { get; init; }
    public string? SourceSearchTerm { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? FirstSeenAt { get; init; }
    public DateTimeOffset? LastScrapedAt { get; init; }
    public DateTimeOffset? RemovedAt { get; init; }
}
