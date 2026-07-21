namespace Heimatplatz.Api.Features.WkoCompanies.Data.Entities;

/// <summary>
/// Einzelne Gewerbeberechtigung aus dem Abschnitt "Berechtigungen" der WKO-Detailseite.
/// Wird als JSON-Liste auf der Entity gespeichert (siehe WkoCompanyConfiguration) - Berechtigungen
/// werden nie unabhaengig von der Firma abgefragt, ein eigenes Kind-Entity waere unnoetig.
/// </summary>
public record WkoCompanyPermit
{
    public string? FachgruppeName { get; init; }
    public string? Description { get; init; }
    public string? ManagingDirector { get; init; }
    public string? GisaNumber { get; init; }
}
