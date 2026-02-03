using System.Text.Json;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Heimatplatz.Api.Features.Legal.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Legal.Data.Seeding;

public class LegalSettingsSeeder(AppDbContext dbContext) : ISeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public int Order => 5; // Frueh ausfuehren, da keine Abhaengigkeiten

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Pruefen ob bereits Datenschutzerklaerung vorhanden ist
        if (await dbContext.Set<LegalSettings>().AnyAsync(x => x.SettingType == "PrivacyPolicy", cancellationToken))
            return;

        var responsibleParty = new ResponsiblePartyDto(
            CompanyName: "Heimatplatz GmbH",
            Street: "Musterstrasse 1",
            PostalCode: "4020",
            City: "Linz",
            Country: "Oesterreich",
            Email: "datenschutz@heimatplatz.at",
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
                "a) Vertragserfuellung (Art. 6 Abs. 1 lit. b DSGVO): Bereitstellung der Plattform, Verwaltung Ihres Benutzerkontos, Vermittlung von Immobilienanfragen.\n\n" +
                "b) Berechtigte Interessen (Art. 6 Abs. 1 lit. f DSGVO): Gewaehrleistung der IT-Sicherheit, Analyse zur Verbesserung unserer Dienste, Betrugspraevention.\n\n" +
                "c) Einwilligung (Art. 6 Abs. 1 lit. a DSGVO): Versand von Benachrichtigungen ueber neue Immobilien (sofern aktiviert)."),

            new(4, "Speicherdauer",
                "- Server-Logs: 30 Tage\n" +
                "- Benutzerkonto-Daten: Bis zur Loeschung des Kontos\n" +
                "- Kontaktanfragen: 3 Jahre nach Abschluss\n" +
                "- Inserate: Bis zur Loeschung durch den Nutzer"),

            new(5, "Empfaenger der Daten",
                "Ihre Daten werden an folgende Empfaenger weitergegeben:\n\n" +
                "- Hosting-Anbieter (Serverstandort: EU)\n" +
                "- Immobilienanbieter bei Kontaktanfragen (nur freigegebene Daten)\n\n" +
                "Eine Uebermittlung in Drittlaender findet nicht statt."),

            new(6, "Ihre Rechte",
                "Sie haben folgende Rechte bezueglich Ihrer personenbezogenen Daten:\n\n" +
                "- Auskunft ueber die gespeicherten Daten (Art. 15 DSGVO)\n" +
                "- Berichtigung unrichtiger Daten (Art. 16 DSGVO)\n" +
                "- Loeschung Ihrer Daten (Art. 17 DSGVO)\n" +
                "- Einschraenkung der Verarbeitung (Art. 18 DSGVO)\n" +
                "- Datenuebertragbarkeit (Art. 20 DSGVO)\n" +
                "- Widerspruch gegen die Verarbeitung (Art. 21 DSGVO)\n" +
                "- Widerruf einer erteilten Einwilligung (Art. 7 Abs. 3 DSGVO)"),

            new(7, "Beschwerderecht",
                "Sie haben das Recht, sich bei der zustaendigen Aufsichtsbehoerde zu beschweren:\n\n" +
                "Oesterreichische Datenschutzbehoerde\n" +
                "Barichgasse 40-42\n" +
                "1030 Wien\n" +
                "E-Mail: dsb@dsb.gv.at\n" +
                "Website: https://www.dsb.gv.at"),

            new(8, "Cookies und Local Storage",
                "Unsere Website verwendet ausschliesslich technisch notwendige Cookies bzw. Local Storage fuer:\n\n" +
                "- Speicherung Ihrer Anmeldedaten (Session)\n" +
                "- Speicherung Ihrer Filtereinstellungen\n\n" +
                "Fuer technisch notwendige Cookies ist keine Einwilligung erforderlich (Paragraph 165 Abs. 3 TKG)."),

            new(9, "Kontakt",
                "Bei Fragen zum Datenschutz wenden Sie sich bitte an die oben genannte E-Mail-Adresse.")
        };

        var privacyPolicy = new LegalSettings
        {
            SettingType = "PrivacyPolicy",
            ResponsiblePartyJson = JsonSerializer.Serialize(responsibleParty, JsonOptions),
            SectionsJson = JsonSerializer.Serialize(sections, JsonOptions),
            Version = "1.0",
            EffectiveDate = DateTimeOffset.UtcNow,
            IsActive = true
        };

        dbContext.Set<LegalSettings>().Add(privacyPolicy);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
