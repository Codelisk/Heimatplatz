/**
 * Misc-Seiten: 404, Debug-Werkzeuge (/debug) sowie statische Rahmentexte der
 * Rechtsseiten (Impressum/Datenschutz). Der interne Bereich hat eine eigene
 * Domain-Datei (./intern.ts, Präfix "intern.").
 * Key-Präfixe: "notFound." / "misc." / "legal."
 */
export const misc = {
  // 404
  "notFound.metaTitle": "Seite nicht gefunden",
  "notFound.metaDescription": "Die angeforderte Seite existiert nicht oder wurde verschoben.",
  "notFound.kicker": "Fehler 404",
  "notFound.title": "Seite nicht gefunden",
  "notFound.text":
    "Die angeforderte Seite existiert nicht oder wurde verschoben. Vielleicht finden Sie das gesuchte Angebot über die Startseite.",
  "notFound.backHome": "Zur Startseite",

  // Debug-Werkzeuge (/debug)
  "misc.debugMetaTitle": "Debug",
  "misc.debugMetaDescription": "Debug-Werkzeuge für die lokale Entwicklung.",
  "misc.debugTitle": "Debug",
  "misc.debugIntro":
    "Entwicklungs-Werkzeuge wie in der MAUI-App: Testbenutzer anmelden und zwischen lokaler Entwicklungs-API und Test-API umschalten.",
  "misc.debugBlocked":
    "Die Debug-Werkzeuge sind nur verfügbar, wenn die Seite vom Entwicklungs-PC aus aufgerufen wird (localhost oder LAN-IP).",
  "misc.debugAuthHeading": "Anmeldestatus",
  "misc.debugAuthLoggedOut": "Aktuell: Ausgeloggt",
  "misc.debugAuthCurrent": "Aktuell: {role} — {email}",
  "misc.debugLogoutButton": "Ausgeloggt",
  "misc.debugLoginSeller": "Als Verkäufer (privat) anmelden",
  "misc.debugLoginBroker": "Als Makler anmelden",
  "misc.debugLoginPropertyManager": "Als Hausverwaltung anmelden",
  "misc.debugLoginBuyer": "Als Käufer anmelden",
  "misc.debugAuthExplainer":
    "Verwendet die Testkonten des aktuell gewählten API-Endpunkts. Dadurch funktionieren auch authentifizierte API-Aufrufe mit der ausgewählten Rolle.",
  "misc.debugRoleBuyer": "Käufer",
  "misc.debugRoleSellerPrivate": "Verkäufer (privat)",
  "misc.debugRoleSellerBroker": "Verkäufer (Makler)",
  "misc.debugRoleSellerPropertyManager": "Verkäufer (Hausverwaltung)",
  "misc.debugRoleBroker": "Makler",
  "misc.debugRolePropertyManager": "Hausverwaltung",
  "misc.debugRoleAdmin": "Administrator",
  "misc.debugSessionRemoved": "Sitzung entfernt.",
  "misc.debugLoggingInAs": "Anmeldung als {label}...",
  "misc.debugLoggedInAs": "Als {label} angemeldet.",
  "misc.debugLoginFailed":
    "Anmeldung als {label} fehlgeschlagen. Läuft der gewählte API-Endpunkt und sind die Testbenutzer vorhanden? ({error})",
  "misc.debugApiHeading": "API-Endpunkt",
  "misc.debugApiDevelopment": "Entwicklung",
  "misc.debugApiTest": "Test",
  "misc.debugApiActive": "Aktiv:",
  "misc.debugApiExplainer":
    "Wirkt sofort für alle folgenden API-Aufrufe im Browser (Live-Angebote, Favoriten, Anmeldung usw.). Nach dem Wechsel ggf. neu anmelden, da Tokens nur für die jeweilige API gelten. Statisch gebaute Inhalte (z.B. Detailseiten) stammen weiterhin aus dem Build.",

  // Rechtsseiten: statische Rahmentexte (Inhalte kommen aus der API)
  "legal.kicker": "Rechtliches",
  "legal.updatedAt": "Stand: {date}",
  "legal.version": "Version {version}",
  "legal.effectiveSince": "Gültig seit: {date}",
  "legal.contact": "Kontakt",

  // Impressum
  "legal.imprintMetaTitle": "Impressum",
  "legal.imprintMetaDescription":
    "Impressum und Anbieterkennzeichnung von Heimatplatz mit Kontakt-, Gewerbe-, Kammer- und Steuerdaten.",
  "legal.imprintTitle": "Impressum",
  "legal.imprintIntro": "Anbieterkennzeichnung und Kontaktangaben zur Plattform Heimatplatz.",
  "legal.groupEcgUgb": "Angaben gemäß ECG und UGB",
  "legal.groupTrade": "Gewerbliche Angaben",
  "legal.groupChamber": "Kammermitgliedschaft",
  "legal.groupTax": "Steuerdaten",
  "legal.labelCompany": "Unternehmen",
  "legal.labelLegalForm": "Rechtsform",
  "legal.labelOwner": "Inhaber",
  "legal.labelAddress": "Adresse",
  "legal.labelEmail": "E-Mail",
  "legal.labelPhone": "Telefon",
  "legal.labelWebsite": "Website",
  "legal.labelTrade": "Gewerbe",
  "legal.labelGisaNumber": "GISA-Zahl",
  "legal.labelTradeAuthority": "Gewerbebehörde",
  "legal.labelProfessionalLaw": "Berufsrecht",
  "legal.labelChamber": "Kammer",
  "legal.labelTradeGroup": "Fachgruppe",
  "legal.labelUidNumber": "UID-Nummer",
  "legal.labelTaxNumber": "Steuernummer",
  "legal.labelDuns": "DUNS",
  "legal.labelGln": "GLN",
  "legal.registerData": "Registerdaten",
  "legal.labelGisaShort": "GISA",
  "legal.labelUidShort": "UID",

  // Datenschutz
  "legal.privacyMetaTitle": "Datenschutz",
  "legal.privacyMetaDescription":
    "Datenschutzerklärung von Heimatplatz: personenbezogene Daten, Rechtsgrundlagen, Speicherdauer, Empfänger und Betroffenenrechte.",
  "legal.privacyTitle": "Datenschutzerklärung",
  "legal.privacyIntro": "Informationen zur Verarbeitung personenbezogener Daten auf Heimatplatz.",
  "legal.responsibleParty": "Verantwortlicher",
  "legal.labelDataProtectionOfficer": "Datenschutzbeauftragter",
  "legal.privacyContactHeading": "Kontakt Datenschutz",
  "legal.dataSubjectRights": "Betroffenenrechte",
  "legal.dataSubjectRightsText":
    "Auskunft, Berichtigung, Löschung, Einschränkung, Datenübertragbarkeit, Widerspruch und Widerruf einer Einwilligung können per E-Mail geltend gemacht werden.",
} as const;
