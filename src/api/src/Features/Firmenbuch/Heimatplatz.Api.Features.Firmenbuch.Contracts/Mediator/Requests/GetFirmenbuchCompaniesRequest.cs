using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Firmenbuch.Contracts.Mediator.Requests;

/// <summary>
/// Katalog-Abfrage mit optionalen Filtern. <paramref name="Status"/>: "aufrecht" filtert auf
/// aufrechte Firmen (Status null), sonst exakter Statuswert ("historisch", "geloescht").
/// </summary>
public record GetFirmenbuchCompaniesRequest(
    int Page = 1,
    int PageSize = 25,
    string? SearchText = null,
    string? Sitz = null,
    string? Status = null,
    string? RechtsformCode = null
) : IRequest<GetFirmenbuchCompaniesResponse>;

public record GetFirmenbuchCompaniesResponse
{
    public required List<FirmenbuchCompanyDto> Companies { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public record FirmenbuchCompanyDto
{
    public required Guid Id { get; init; }
    public required string Fnr { get; init; }
    public required string Name { get; init; }
    public string? Status { get; init; }
    public string? Sitz { get; init; }
    public string? RechtsformCode { get; init; }
    public string? RechtsformText { get; init; }
    public string? Rechtseigenschaft { get; init; }
    public string? GerichtCode { get; init; }
    public string? GerichtText { get; init; }
    public string? SourceOrtNr { get; init; }
    public DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }
}
