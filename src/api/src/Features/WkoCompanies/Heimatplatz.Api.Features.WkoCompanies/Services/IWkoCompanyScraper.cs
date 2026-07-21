using Heimatplatz.Api.Features.WkoCompanies.Data.Entities;

namespace Heimatplatz.Api.Features.WkoCompanies.Services;

public interface IWkoCompanyScraper
{
    /// <summary>
    /// Sucht auf firmen.wko.at nach <paramref name="keyword"/> in <paramref name="region"/> und
    /// laedt dabei alle Ergebnisseiten nach (simuliert wiederholtes Klicken auf "Mehr laden").
    /// </summary>
    Task<List<WkoCompanySearchResult>> SearchAsync(string keyword, string region, CancellationToken ct = default);

    /// <summary>Laedt die Detailseite einer Firma und liefert die dort zusaetzlich verfuegbaren Felder.</summary>
    Task<WkoCompanyDetail> GetDetailAsync(string detailUrl, CancellationToken ct = default);
}

/// <summary>Aus einer Trefferliste (Suchergebnis-Karte) extrahierte Firma.</summary>
public record WkoCompanySearchResult
{
    public required Guid WkoFirmaId { get; init; }
    public required string Name { get; init; }
    public required string DetailUrl { get; init; }
    public string? CategoryText { get; init; }
    public string? Street { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public List<string> Phones { get; init; } = [];
    public string? Email { get; init; }
    public string? Website { get; init; }
    public bool IsTrainingCompany { get; init; }
}

/// <summary>Von der Detailseite zusaetzlich verfuegbare Felder.</summary>
public record WkoCompanyDetail
{
    public string? Name { get; init; }
    public string? CategoryText { get; init; }
    public string? Street { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public List<string> Phones { get; init; } = [];
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? OpeningHoursText { get; init; }
    public string? CompanyRegisterNumber { get; init; }
    public string? CompanyCourt { get; init; }
    public string? Gln { get; init; }
    public string? LegalForm { get; init; }
    public int? FoundedYear { get; init; }
    public List<WkoCompanyPermit> Permits { get; init; } = [];
}
