using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations;

/// <summary>
/// Fuegt die Datenschutzerklaerung in die LegalSettings Tabelle ein.
/// </summary>
public partial class SeedLegalSettingsPrivacyPolicy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");

        var responsiblePartyJson = """
            {"companyName":"Heimatplatz GmbH","street":"Musterstrasse 1","postalCode":"4020","city":"Linz","country":"Oesterreich","email":"datenschutz@heimatplatz.at","phone":null,"dataProtectionOfficer":null}
            """.Trim();

        var sectionsJson = """
            [{"sortOrder":1,"title":"Verantwortlicher","content":"Verantwortlicher im Sinne der Datenschutz-Grundverordnung (DSGVO) ist die im Abschnitt genannte Stelle.","isVisible":true},{"sortOrder":2,"title":"Welche Daten wir erheben","content":"Bei der Nutzung unserer Website/App werden folgende Daten verarbeitet:\n\n- Server-Logdaten (IP-Adresse, Zugriffszeitpunkt, Browser-Typ)\n- Registrierungsdaten (Name, E-Mail-Adresse, Passwort)\n- Nutzungsdaten (Sucheinstellungen, Favoriten, Kontaktanfragen)\n- Immobiliendaten bei Inseratserstellung","isVisible":true},{"sortOrder":3,"title":"Zweck und Rechtsgrundlage","content":"Wir verarbeiten Ihre Daten zu folgenden Zwecken:\n\na) Vertragserfuellung (Art. 6 Abs. 1 lit. b DSGVO): Bereitstellung der Plattform, Verwaltung Ihres Benutzerkontos, Vermittlung von Immobilienanfragen.\n\nb) Berechtigte Interessen (Art. 6 Abs. 1 lit. f DSGVO): Gewaehrleistung der IT-Sicherheit, Analyse zur Verbesserung unserer Dienste, Betrugspraevention.\n\nc) Einwilligung (Art. 6 Abs. 1 lit. a DSGVO): Versand von Benachrichtigungen ueber neue Immobilien (sofern aktiviert).","isVisible":true},{"sortOrder":4,"title":"Speicherdauer","content":"- Server-Logs: 30 Tage\n- Benutzerkonto-Daten: Bis zur Loeschung des Kontos\n- Kontaktanfragen: 3 Jahre nach Abschluss\n- Inserate: Bis zur Loeschung durch den Nutzer","isVisible":true},{"sortOrder":5,"title":"Empfaenger der Daten","content":"Ihre Daten werden an folgende Empfaenger weitergegeben:\n\n- Hosting-Anbieter (Serverstandort: EU)\n- Immobilienanbieter bei Kontaktanfragen (nur freigegebene Daten)\n\nEine Uebermittlung in Drittlaender findet nicht statt.","isVisible":true},{"sortOrder":6,"title":"Ihre Rechte","content":"Sie haben folgende Rechte bezueglich Ihrer personenbezogenen Daten:\n\n- Auskunft ueber die gespeicherten Daten (Art. 15 DSGVO)\n- Berichtigung unrichtiger Daten (Art. 16 DSGVO)\n- Loeschung Ihrer Daten (Art. 17 DSGVO)\n- Einschraenkung der Verarbeitung (Art. 18 DSGVO)\n- Datenuebertragbarkeit (Art. 20 DSGVO)\n- Widerspruch gegen die Verarbeitung (Art. 21 DSGVO)\n- Widerruf einer erteilten Einwilligung (Art. 7 Abs. 3 DSGVO)","isVisible":true},{"sortOrder":7,"title":"Beschwerderecht","content":"Sie haben das Recht, sich bei der zustaendigen Aufsichtsbehoerde zu beschweren:\n\nOesterreichische Datenschutzbehoerde\nBarichgasse 40-42\n1030 Wien\nE-Mail: dsb@dsb.gv.at\nWebsite: https://www.dsb.gv.at","isVisible":true},{"sortOrder":8,"title":"Cookies und Local Storage","content":"Unsere Website verwendet ausschliesslich technisch notwendige Cookies bzw. Local Storage fuer:\n\n- Speicherung Ihrer Anmeldedaten (Session)\n- Speicherung Ihrer Filtereinstellungen\n\nFuer technisch notwendige Cookies ist keine Einwilligung erforderlich (Paragraph 165 Abs. 3 TKG).","isVisible":true},{"sortOrder":9,"title":"Kontakt","content":"Bei Fragen zum Datenschutz wenden Sie sich bitte an die oben genannte E-Mail-Adresse.","isVisible":true}]
            """.Trim();

        migrationBuilder.Sql($"""
            INSERT INTO [LegalSettings] ([Id], [SettingType], [ResponsiblePartyJson], [SectionsJson], [Version], [EffectiveDate], [IsActive], [CreatedAt], [UpdatedAt])
            SELECT NEWID(), 'PrivacyPolicy', N'{responsiblePartyJson.Replace("'", "''")}', N'{sectionsJson.Replace("'", "''")}', '1.0', SYSDATETIMEOFFSET(), 1, SYSDATETIMEOFFSET(), NULL
            WHERE NOT EXISTS (SELECT 1 FROM [LegalSettings] WHERE [SettingType] = 'PrivacyPolicy')
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM [LegalSettings] WHERE [SettingType] = 'PrivacyPolicy'");
    }
}
