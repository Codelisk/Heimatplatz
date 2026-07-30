namespace Heimatplatz.Api.Features.Firmenbuch.Services;

public interface IFirmenpoolApiClient
{
    /// <summary>Eine Seite des Firmenpool-Katalogs (GET /api/firmenbuch/companies).</summary>
    Task<FirmenpoolCompanyPage> GetCompaniesAsync(FirmenpoolCompanyQuery query, CancellationToken ct = default);

    /// <summary>
    /// Voller Firmendatensatz inkl. Adresse, Funktionaeren und Gewerben
    /// (GET /api/firmenbuch/companies/{fnr}); null wenn die FNR dort unbekannt ist.
    /// </summary>
    Task<FirmenpoolCompanyDetail?> GetCompanyDetailAsync(string fnr, CancellationToken ct = default);
}

/// <summary>
/// Abfrage fuer die Firmenliste. Listen-Parameter sind kommaseparierte Strings, weil die
/// Firmenpool-API sie als GET-Query so erwartet (ODER-verknuepft, serverseitig gefiltert -
/// Trefferzahl und Seitengrenzen bleiben exakt).
/// </summary>
public record FirmenpoolCompanyQuery
{
    /// <summary>1-basiert (Firmenpool-Konvention).</summary>
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;

    /// <summary>Name-Teilstring oder exakte FNR.</summary>
    public string? SearchText { get; init; }

    /// <summary>Kommaseparierte Orte, Teilstring-Vergleich auf den Sitz.</summary>
    public string? Sitz { get; init; }

    /// <summary>"aufrecht" = nur Firmen ohne Loeschungsvermerk.</summary>
    public string? Status { get; init; }

    /// <summary>Kommaseparierte Schlagworte, der Name muss mindestens eines enthalten.</summary>
    public string? NameContainsAny { get; init; }

    /// <summary>
    /// Kommaseparierte Schlagworte gegen den amtlichen Wortlaut der aktiven
    /// GISA-Gewerbeberechtigungen (z.B. "Immobilientreuhänder"). Zusammen mit
    /// <see cref="NameContainsAny"/> gilt in der Quelle ODER: ein Treffer auf einer
    /// der beiden Seiten genügt.
    /// </summary>
    public string? GewerbeContainsAny { get; init; }

    /// <summary>Kommaseparierte FNRs, die ausgeblendet werden (z.B. bereits uebernommene).</summary>
    public string? ExcludeFnrs { get; init; }
}

public record FirmenpoolCompanyPage
{
    public List<FirmenpoolCompanyItem> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

/// <summary>Katalog-Stammsatz einer Firma, wie ihn die Firmenpool-Liste liefert.</summary>
public record FirmenpoolCompanyItem
{
    public required string Fnr { get; init; }
    public required string Name { get; init; }

    /// <summary>null = aufrecht; sonst "gelöscht" oder "historisch".</summary>
    public string? Status { get; init; }

    public string? Sitz { get; init; }
    public string? RechtsformCode { get; init; }
    public string? RechtsformText { get; init; }
    public string? Rechtseigenschaft { get; init; }
    public string? GerichtCode { get; init; }
    public string? GerichtText { get; init; }
    public string? SourceOrtNr { get; init; }

    /// <summary>Anzahl ausgewerteter Jahresabschluesse im Firmenpool.</summary>
    public int StatementCount { get; init; }

    public DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }
}

/// <summary>Voller Firmendatensatz aus dem Firmenpool-Auszug (AUSZUG_V2-Daten + GISA).</summary>
public record FirmenpoolCompanyDetail
{
    public required string Fnr { get; init; }
    public required string Name { get; init; }
    public string? Status { get; init; }
    public string? Euid { get; init; }
    public DateOnly? Gegruendet { get; init; }
    public string? Strasse { get; init; }
    public string? Hausnummer { get; init; }
    public string? Plz { get; init; }
    public string? Ort { get; init; }
    public string? Staat { get; init; }
    public string? Sitz { get; init; }
    public string? RechtsformCode { get; init; }
    public string? RechtsformText { get; init; }
    public string? GerichtText { get; init; }
    public string? Handelsregisternummer { get; init; }
    public DateTimeOffset? AuszugStand { get; init; }
    public int AbschluesseVorhanden { get; init; }
    public List<FirmenpoolOfficer> Funktionaere { get; init; } = [];
    public List<FirmenpoolTrade> Gewerbe { get; init; } = [];
}

public record FirmenpoolOfficer
{
    public required string Name { get; init; }
    public string? FunktionCode { get; init; }
    public string? FunktionText { get; init; }
    public DateOnly? Seit { get; init; }
    public bool Aktiv { get; init; }
}

public record FirmenpoolTrade
{
    public long GisaZahl { get; init; }
    public string? Schluessel { get; init; }
    public string? Wortlaut { get; init; }
    public string? Plz { get; init; }
    public string? Ort { get; init; }
    public List<string> WeitereStandorte { get; init; } = [];
    public bool Aktiv { get; init; }
}
