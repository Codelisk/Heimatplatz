namespace Heimatplatz.Api.Features.Partners.Contracts.Models;

/// <summary>
/// Kategorien als String-Konstanten statt Enum (gleiche Idee wie LegalSettingTypes):
/// neue Kategorien brauchen dann keine Contract-/Client-Aenderung, nur einen neuen Wert.
/// </summary>
public static class PartnerCategories
{
    /// <summary>Makler-Partner, dessen Objekte auf Heimatplatz laufen (z.B. per OpenImmo-Feed).</summary>
    public const string Broker = "Broker";

    /// <summary>Datenquelle (Transparenz-Eintrag), kein Kooperationspartner.</summary>
    public const string DataSource = "DataSource";

    public static readonly IReadOnlyList<string> All = [Broker, DataSource];
}
