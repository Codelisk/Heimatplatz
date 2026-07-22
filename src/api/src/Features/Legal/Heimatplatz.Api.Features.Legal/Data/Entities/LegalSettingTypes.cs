namespace Heimatplatz.Api.Features.Legal.Data.Entities;

/// <summary>
/// Erlaubte Werte fuer <see cref="LegalSettings.SettingType"/>. Frueher als Magic Strings
/// ueber Seeder und Handler verteilt.
/// </summary>
public static class LegalSettingTypes
{
    public const string PrivacyPolicy = "PrivacyPolicy";
    public const string Imprint = "Imprint";

    /// <summary>
    /// Kontakt-Zusatzfelder (Support-Adresse, Erreichbarkeit, Social-Profile). Ergaenzt das
    /// Impressum, ersetzt es nicht - siehe ContactSettingsDto.
    /// </summary>
    public const string Contact = "Contact";
}
