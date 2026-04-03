using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations;

/// <summary>
/// Seeds PrivacyPolicy and Imprint into LegalSettings using SQLite-compatible syntax.
/// Replaces the broken 20260206120000_SeedLegalSettingsPrivacyPolicy migration.
/// </summary>
public partial class SeedLegalSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Privacy Policy
        migrationBuilder.Sql("""
            INSERT INTO LegalSettings (Id, SettingType, ResponsiblePartyJson, SectionsJson, Version, EffectiveDate, IsActive, CreatedAt, UpdatedAt)
            SELECT
                lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)),2) || '-' || hex(randomblob(6))),
                'PrivacyPolicy',
                '{"companyName":"Ing. Daniel Hufnagl","street":"Stockham 44/Tuer 2","postalCode":"4663","city":"Laakirchen","country":"Oesterreich","email":"info@heimatplatz.at","phone":null,"dataProtectionOfficer":null}',
                '[{"sortOrder":1,"title":"Verantwortlicher","content":"Verantwortlicher im Sinne der Datenschutz-Grundverordnung (DSGVO) ist die im Abschnitt genannte Stelle.","isVisible":true},{"sortOrder":2,"title":"Welche Daten wir erheben","content":"Bei der Nutzung unserer Website/App werden folgende Daten verarbeitet:\n\n- Server-Logdaten (IP-Adresse, Zugriffszeitpunkt, Browser-Typ)\n- Registrierungsdaten (Name, E-Mail-Adresse, Passwort)\n- Nutzungsdaten (Sucheinstellungen, Favoriten, Kontaktanfragen)\n- Immobiliendaten bei Inseratserstellung","isVisible":true},{"sortOrder":3,"title":"Zweck und Rechtsgrundlage","content":"Wir verarbeiten Ihre Daten zu folgenden Zwecken:\n\na) Vertragserfuellung (Art. 6 Abs. 1 lit. b DSGVO): Bereitstellung der Plattform, Verwaltung Ihres Benutzerkontos, Vermittlung von Immobilienanfragen.\n\nb) Berechtigte Interessen (Art. 6 Abs. 1 lit. f DSGVO): Gewaehrleistung der IT-Sicherheit, Analyse zur Verbesserung unserer Dienste, Betrugspraevention.\n\nc) Einwilligung (Art. 6 Abs. 1 lit. a DSGVO): Versand von Benachrichtigungen ueber neue Immobilien (sofern aktiviert).","isVisible":true},{"sortOrder":4,"title":"Speicherdauer","content":"- Server-Logs: 30 Tage\n- Benutzerkonto-Daten: Bis zur Loeschung des Kontos\n- Kontaktanfragen: 3 Jahre nach Abschluss\n- Inserate: Bis zur Loeschung durch den Nutzer","isVisible":true},{"sortOrder":5,"title":"Empfaenger der Daten","content":"Ihre Daten werden an folgende Empfaenger weitergegeben:\n\n- Hosting-Anbieter (Serverstandort: EU)\n- Immobilienanbieter bei Kontaktanfragen (nur freigegebene Daten)\n\nEine Uebermittlung in Drittlaender findet nicht statt.","isVisible":true},{"sortOrder":6,"title":"Ihre Rechte","content":"Sie haben folgende Rechte bezueglich Ihrer personenbezogenen Daten:\n\n- Auskunft ueber die gespeicherten Daten (Art. 15 DSGVO)\n- Berichtigung unrichtiger Daten (Art. 16 DSGVO)\n- Loeschung Ihrer Daten (Art. 17 DSGVO)\n- Einschraenkung der Verarbeitung (Art. 18 DSGVO)\n- Datenuebertragbarkeit (Art. 20 DSGVO)\n- Widerspruch gegen die Verarbeitung (Art. 21 DSGVO)\n- Widerruf einer erteilten Einwilligung (Art. 7 Abs. 3 DSGVO)","isVisible":true},{"sortOrder":7,"title":"Beschwerderecht","content":"Sie haben das Recht, sich bei der zustaendigen Aufsichtsbehoerde zu beschweren:\n\nOesterreichische Datenschutzbehoerde\nBarichgasse 40-42\n1030 Wien\nE-Mail: dsb@dsb.gv.at\nWebsite: https://www.dsb.gv.at","isVisible":true},{"sortOrder":8,"title":"Cookies und Local Storage","content":"Unsere Website verwendet ausschliesslich technisch notwendige Cookies bzw. Local Storage fuer:\n\n- Speicherung Ihrer Anmeldedaten (Session)\n- Speicherung Ihrer Filtereinstellungen\n\nFuer technisch notwendige Cookies ist keine Einwilligung erforderlich (Paragraph 165 Abs. 3 TKG).","isVisible":true},{"sortOrder":9,"title":"Kontakt","content":"Bei Fragen zum Datenschutz wenden Sie sich bitte an die oben genannte E-Mail-Adresse.","isVisible":true}]',
                '1.0',
                datetime('now'),
                1,
                datetime('now'),
                NULL
            WHERE NOT EXISTS (SELECT 1 FROM LegalSettings WHERE SettingType = 'PrivacyPolicy');
            """);

        // Imprint
        migrationBuilder.Sql("""
            INSERT INTO LegalSettings (Id, SettingType, ResponsiblePartyJson, SectionsJson, Version, EffectiveDate, IsActive, CreatedAt, UpdatedAt)
            SELECT
                lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)),2) || '-' || hex(randomblob(6))),
                'Imprint',
                '{"companyName":"Ing. Daniel Hufnagl","legalForm":"Einzelunternehmen","owner":"Ing. Daniel Hufnagl","street":"Stockham 44/Tuer 2","postalCode":"4663","city":"Laakirchen","country":"Oesterreich","email":"info@heimatplatz.at","phone":null,"website":"https://www.heimatplatz.at","uidNumber":"ATU75151817","taxNumber":"532163383","dunsNumber":"30-080-8592","gln":"9110026231195","gisaNumber":"31233118","trade":"Dienstleistungen in der automatischen Datenverarbeitung und Informationstechnik","tradeAuthority":"Bezirkshauptmannschaft Gmunden","professionalLaw":"Gewerbeordnung 1994 (GewO)","chamberMembership":"Wirtschaftskammer Oberoesterreich","tradeGroup":"Fachgruppe Unternehmensberatung, Buchhaltung und Informationstechnologie"}',
                '[{"sortOrder":1,"title":"Haftungsausschluss","content":"Die Inhalte dieser Website wurden mit groesster Sorgfalt erstellt. Fuer die Richtigkeit, Vollstaendigkeit und Aktualitaet der Inhalte uebernehmen wir jedoch keine Gewaehr.","isVisible":true},{"sortOrder":2,"title":"Urheberrecht","content":"Die durch den Seitenbetreiber erstellten Inhalte und Werke auf diesen Seiten unterliegen dem oesterreichischen Urheberrecht. Die Vervielfaeltigung, Bearbeitung, Verbreitung und jede Art der Verwertung ausserhalb der Grenzen des Urheberrechtes beduerfen der schriftlichen Zustimmung des jeweiligen Autors bzw. Erstellers.","isVisible":true},{"sortOrder":3,"title":"Streitschlichtung","content":"Die Europaeische Kommission stellt eine Plattform zur Online-Streitbeilegung (OS) bereit: https://ec.europa.eu/consumers/odr/\n\nWir sind nicht bereit oder verpflichtet, an Streitbeilegungsverfahren vor einer Verbraucherschlichtungsstelle teilzunehmen.","isVisible":true}]',
                '1.0',
                datetime('now'),
                1,
                datetime('now'),
                NULL
            WHERE NOT EXISTS (SELECT 1 FROM LegalSettings WHERE SettingType = 'Imprint');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM LegalSettings WHERE SettingType IN ('PrivacyPolicy', 'Imprint')");
    }
}
