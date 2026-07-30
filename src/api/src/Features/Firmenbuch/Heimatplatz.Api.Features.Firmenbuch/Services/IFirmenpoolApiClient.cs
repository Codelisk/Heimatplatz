namespace Heimatplatz.Api.Features.Firmenbuch.Services;

public interface IFirmenpoolApiClient
{
    /// <summary>Eine Seite des Firmenpool-Katalogs (GET /api/firmenbuch/companies).</summary>
    Task<FirmenpoolCompanyPage> GetCompaniesAsync(int page, int pageSize, CancellationToken ct = default);
}

public record FirmenpoolCompanyPage
{
    public List<FirmenpoolCompanyItem> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

/// <summary>
/// Katalog-Stammsatz einer Firma, wie ihn die Firmenpool-API liefert. Feldbedeutungen wie in
/// <see cref="Data.Entities.FirmenbuchCompany"/>; First-/LastSeenAt sind die Sichtungszeitpunkte
/// der QUELLE (Firmenpool-Crawl), nicht des hiesigen Spiegels.
/// </summary>
public record FirmenpoolCompanyItem
{
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
