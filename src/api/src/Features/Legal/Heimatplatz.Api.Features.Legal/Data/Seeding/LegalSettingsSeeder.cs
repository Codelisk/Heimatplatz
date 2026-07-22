using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Heimatplatz.Api.Features.Legal.Data.Entities;
using Heimatplatz.Api.Features.Legal.Services;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Legal.Data.Seeding;

/// <summary>
/// Legt Datenschutz, Impressum und Kontaktdaten an, wenn sie fehlen.
///
/// Alle Firmenwerte kommen aus <see cref="CompanyMasterData"/> - die Kopien im
/// GetPrivacyPolicyHandler (On-Demand-Seed) sind bewusst entfernt, weil sie
/// auseinandergelaufen waren. Aendern tut man Stammdaten zur Laufzeit ueber
/// /intern/kontakt, nicht hier.
/// </summary>
public class LegalSettingsSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 5; // Früh ausführen, da keine Abhängigkeiten

    /// <summary>
    /// Rechtsinhalte (Datenschutz/Impressum) sind Pflichtdaten - läuft auch in Produktion
    /// </summary>
    public bool IsDemoData => false;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPrivacyPolicyAsync(cancellationToken);
        await SeedImprintAsync(cancellationToken);
        await SeedContactAsync(cancellationToken);
    }

    private async Task SeedPrivacyPolicyAsync(CancellationToken cancellationToken)
    {
        if (await ExistsAsync(LegalSettingTypes.PrivacyPolicy, cancellationToken))
            return;

        var responsibleParty = new ResponsiblePartyDto(
            CompanyName: CompanyMasterData.CompanyName,
            Street: CompanyMasterData.Street,
            PostalCode: CompanyMasterData.PostalCode,
            City: CompanyMasterData.City,
            Country: CompanyMasterData.Country,
            Email: CompanyMasterData.Email,
            Phone: null,
            DataProtectionOfficer: null
        );

        var sections = new List<LegalSectionDto>
        {
            new(1, "Verantwortlicher",
                "Verantwortlicher im Sinne der Datenschutz-Grundverordnung (DSGVO) ist die im Abschnitt genannte Stelle."),

            new(2, "Welche Daten wir erheben",
                "Bei der Nutzung unserer Website/App werden folgende Daten verarbeitet:\n\n" +
                "- Server-Logdaten (IP-Adresse, Zugriffszeitpunkt, Browser-Typ)\n" +
                "- Registrierungsdaten (Name, E-Mail-Adresse, Passwort)\n" +
                "- Nutzungsdaten (Sucheinstellungen, Favoriten, Kontaktanfragen)\n" +
                "- Immobiliendaten bei Inseratserstellung"),

            new(3, "Zweck und Rechtsgrundlage",
                "Wir verarbeiten Ihre Daten zu folgenden Zwecken:\n\n" +
                "a) Vertragserfüllung (Art. 6 Abs. 1 lit. b DSGVO): Bereitstellung der Plattform, Verwaltung Ihres Benutzerkontos, Vermittlung von Immobilienanfragen.\n\n" +
                "b) Berechtigte Interessen (Art. 6 Abs. 1 lit. f DSGVO): Gewährleistung der IT-Sicherheit, Analyse zur Verbesserung unserer Dienste, Betrugsprävention.\n\n" +
                "c) Einwilligung (Art. 6 Abs. 1 lit. a DSGVO): Versand von Benachrichtigungen über neue Immobilien (sofern aktiviert)."),

            new(4, "Speicherdauer",
                "- Server-Logs: 30 Tage\n" +
                "- Benutzerkonto-Daten: Bis zur Löschung des Kontos\n" +
                "- Kontaktanfragen: 3 Jahre nach Abschluss\n" +
                "- Inserate: Bis zur Löschung durch den Nutzer"),

            new(5, "Empfänger der Daten",
                "Ihre Daten werden an folgende Empfänger weitergegeben:\n\n" +
                "- Hosting-Anbieter (Serverstandort: EU)\n" +
                "- Immobilienanbieter bei Kontaktanfragen (nur freigegebene Daten)\n\n" +
                "Eine Übermittlung in Drittländer findet nicht statt."),

            new(6, "Ihre Rechte",
                "Sie haben folgende Rechte bezüglich Ihrer personenbezogenen Daten:\n\n" +
                "- Auskunft über die gespeicherten Daten (Art. 15 DSGVO)\n" +
                "- Berichtigung unrichtiger Daten (Art. 16 DSGVO)\n" +
                "- Löschung Ihrer Daten (Art. 17 DSGVO)\n" +
                "- Einschränkung der Verarbeitung (Art. 18 DSGVO)\n" +
                "- Datenübertragbarkeit (Art. 20 DSGVO)\n" +
                "- Widerspruch gegen die Verarbeitung (Art. 21 DSGVO)\n" +
                "- Widerruf einer erteilten Einwilligung (Art. 7 Abs. 3 DSGVO)"),

            new(7, "Beschwerderecht",
                "Sie haben das Recht, sich bei der zuständigen Aufsichtsbehörde zu beschweren:\n\n" +
                "Österreichische Datenschutzbehörde\n" +
                "Barichgasse 40-42\n" +
                "1030 Wien\n" +
                "E-Mail: dsb@dsb.gv.at\n" +
                "Website: https://www.dsb.gv.at"),

            new(8, "Cookies und Local Storage",
                "Unsere Website verwendet ausschließlich technisch notwendige Cookies bzw. Local Storage für:\n\n" +
                "- Speicherung Ihrer Anmeldedaten (Session)\n" +
                "- Speicherung Ihrer Filtereinstellungen\n\n" +
                "Für technisch notwendige Cookies ist keine Einwilligung erforderlich (Paragraph 165 Abs. 3 TKG)."),

            new(9, "Kontakt",
                "Bei Fragen zum Datenschutz wenden Sie sich bitte an die oben genannte E-Mail-Adresse.")
        };

        await AddAsync(LegalSettingTypes.PrivacyPolicy, responsibleParty, sections, cancellationToken);
    }

    private async Task SeedImprintAsync(CancellationToken cancellationToken)
    {
        if (await ExistsAsync(LegalSettingTypes.Imprint, cancellationToken))
            return;

        var party = new ImprintPartyDto(
            CompanyName: CompanyMasterData.CompanyName,
            LegalForm: CompanyMasterData.LegalForm,
            Owner: CompanyMasterData.Owner,
            Street: CompanyMasterData.Street,
            PostalCode: CompanyMasterData.PostalCode,
            City: CompanyMasterData.City,
            Country: CompanyMasterData.Country,
            Email: CompanyMasterData.Email,
            Phone: CompanyMasterData.Phone,
            Website: CompanyMasterData.Website,
            UidNumber: CompanyMasterData.UidNumber,
            TaxNumber: CompanyMasterData.TaxNumber,
            DunsNumber: CompanyMasterData.DunsNumber,
            Gln: CompanyMasterData.Gln,
            GisaNumber: CompanyMasterData.GisaNumber,
            Trade: CompanyMasterData.Trade,
            TradeAuthority: CompanyMasterData.TradeAuthority,
            ProfessionalLaw: CompanyMasterData.ProfessionalLaw,
            ChamberMembership: CompanyMasterData.ChamberMembership,
            TradeGroup: CompanyMasterData.TradeGroup
        );

        var sections = new List<LegalSectionDto>
        {
            new(1, "Haftungsausschluss",
                "Die Inhalte dieser Website wurden mit größter Sorgfalt erstellt. " +
                "Für die Richtigkeit, Vollständigkeit und Aktualität der Inhalte " +
                "übernehmen wir jedoch keine Gewähr."),

            new(2, "Urheberrecht",
                "Die durch den Seitenbetreiber erstellten Inhalte und Werke auf diesen Seiten " +
                "unterliegen dem österreichischen Urheberrecht. Die Vervielfältigung, Bearbeitung, " +
                "Verbreitung und jede Art der Verwertung außerhalb der Grenzen des Urheberrechtes " +
                "bedürfen der schriftlichen Zustimmung des jeweiligen Autors bzw. Erstellers."),

            new(3, "Streitschlichtung",
                "Die Europäische Kommission stellt eine Plattform zur Online-Streitbeilegung (OS) bereit: " +
                "https://ec.europa.eu/consumers/odr/\n\n" +
                "Wir sind nicht bereit oder verpflichtet, an Streitbeilegungsverfahren " +
                "vor einer Verbraucherschlichtungsstelle teilzunehmen.")
        };

        await AddAsync(LegalSettingTypes.Imprint, party, sections, cancellationToken);
    }

    /// <summary>
    /// Der Contact-Datensatz startet bewusst LEER: alles faellt damit auf das Impressum
    /// zurueck. Support-Adresse, Erreichbarkeit und Social-Profile werden ueber
    /// /intern/kontakt gepflegt, sobald es sie gibt.
    /// </summary>
    private async Task SeedContactAsync(CancellationToken cancellationToken)
    {
        if (await ExistsAsync(LegalSettingTypes.Contact, cancellationToken))
            return;

        await AddAsync(LegalSettingTypes.Contact, new ContactSettingsDto(), sections: null, cancellationToken);
    }

    private Task<bool> ExistsAsync(string settingType, CancellationToken cancellationToken)
        => dbContext.Set<LegalSettings>().AnyAsync(x => x.SettingType == settingType && x.IsActive, cancellationToken);

    private async Task AddAsync<TParty>(string settingType, TParty party, List<LegalSectionDto>? sections, CancellationToken cancellationToken)
    {
        dbContext.Set<LegalSettings>().Add(new LegalSettings
        {
            SettingType = settingType,
            ResponsiblePartyJson = LegalJson.Serialize(party),
            SectionsJson = sections == null ? null : LegalJson.Serialize(sections),
            Version = "1.0",
            EffectiveDate = DateTimeOffset.UtcNow,
            IsActive = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
