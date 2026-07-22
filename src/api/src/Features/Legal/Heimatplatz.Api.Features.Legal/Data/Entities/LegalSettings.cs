using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Legal.Data.Entities;

/// <summary>
/// Entity fuer rechtliche Einstellungen und Kontakt-Stammdaten
/// (Datenschutz, Impressum, Kontaktdaten)
/// </summary>
public class LegalSettings : BaseEntity
{
    /// <summary>
    /// Typ der Einstellung - siehe <see cref="LegalSettingTypes"/>
    /// </summary>
    public required string SettingType { get; set; }

    /// <summary>
    /// Je nach SettingType als JSON: ResponsiblePartyDto (PrivacyPolicy),
    /// ImprintPartyDto (Imprint) oder ContactSettingsDto (Contact)
    /// </summary>
    public string? ResponsiblePartyJson { get; set; }

    /// <summary>
    /// Abschnitte als JSON-Array (List of LegalSectionDto). Beim Contact-Datensatz leer.
    /// </summary>
    public string? SectionsJson { get; set; }

    /// <summary>
    /// Versionsnummer z.B. "1.0", "1.1"
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Datum ab dem diese Version gueltig ist
    /// </summary>
    public DateTimeOffset EffectiveDate { get; set; }

    /// <summary>
    /// Ist dies die aktive Version?
    /// </summary>
    public bool IsActive { get; set; }
}
