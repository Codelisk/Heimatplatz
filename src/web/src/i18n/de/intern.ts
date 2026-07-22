/**
 * Interner Bereich (/intern): Dashboard, Edikte-Sync, Nutzer- und Immobilienverwaltung.
 * Key-Präfix: "intern."
 * Zugriffsschutz liegt auf Netzwerk-Ebene (Caddy-IP-Sperre auf /intern*), die Seiten
 * selbst sind login-frei.
 */
export const intern = {
  // Dashboard (/intern)
  "intern.metaTitle": "Intern",
  "intern.metaDescription": "Interner Bereich.",
  "intern.title": "Intern",
  "intern.intro":
    "Nur von der freigegebenen IP erreichbar (siehe Caddyfile). Kein Login nötig - die Netzwerk-Sperre ist die Zugriffsschranke.",

  // Kennzahlen
  "intern.statsHeading": "Überblick",
  "intern.statsLoadFailed":
    "Kennzahlen konnten nicht geladen werden. Ist die API erreichbar und ADMIN_API_KEY gesetzt?",
  "intern.statsTotalUsers": "Nutzer gesamt",
  "intern.statsNewUsers7": "Neu (7 Tage)",
  "intern.statsNewUsers30": "Neu (30 Tage)",
  "intern.statsTotalProperties": "Inserate gesamt",
  "intern.statsUserProperties": "Nutzer-Inserate",
  "intern.statsForeclosures": "Zwangsversteigerungen",
  "intern.statsHidden": "Ausgeblendet",

  // Navigation zu den Verwaltungsseiten
  "intern.navUsers": "Nutzer",
  "intern.navUsersDescription": "Registrierte Nutzer einsehen und durchsuchen - neueste zuerst.",
  "intern.navProperties": "Immobilien",
  "intern.navPropertiesDescription":
    "Inserate von Nutzern und Zwangsversteigerungen verwalten: ausblenden, einblenden, löschen.",
  "intern.navMarketing": "Marketing",
  "intern.navMarketingDescription":
    "E-Mails mit KI-generiertem Text erstellen, prüfen und von info@heimatplatz.at versenden.",
  "intern.navAnalytics": "Analytics",
  "intern.navAnalyticsDescription":
    "Rybbit-Traffic-Dashboard öffnen und Suchperformance-Kennzahlen aus der Google Search Console einsehen.",

  // Edikte-Sync (Zwangsversteigerungen)
  "intern.syncTriggered":
    "Sync wurde gestartet und läuft im Hintergrund (Scraping mit Verzögerung pro Edikt - je nach Anzahl ein bis wenige Minuten). Lade diese Seite in 1-2 Minuten neu, um das Ergebnis zu sehen.",
  "intern.syncFailed": "Sync konnte nicht gestartet werden. Ist die API erreichbar?",
  "intern.syncHeading": "Edikte-Sync (Zwangsversteigerungen)",
  "intern.lastSync": "Letzter Sync",
  "intern.activeAuctions": "Aktive Auktionen",
  "intern.removedAuctions": "Entfernt/Abgeschlossen",
  "intern.totalChanges": "Änderungen (gesamt)",
  "intern.never": "Noch nie",
  "intern.statusLoadFailed": "Status konnte nicht geladen werden.",
  "intern.startSync": "Sync jetzt starten",
  "intern.syncExplainer":
    "Holt die aktuelle Ediktsliste (Oberösterreich) von edikte.justiz.gv.at, aktualisiert die Zwangsversteigerungen und leitet daraus Immobilien-Inserate ab. Läuft nicht automatisch - nur bei manuellem Auslösen hier.",

  // WKO-Firmen-Sync
  "intern.wkoSyncHeading": "WKO-Firmen-Sync (Immobilienbranche OÖ)",
  "intern.wkoSyncTriggered":
    "WKO-Sync wurde gestartet und läuft im Hintergrund (mehrere Suchbegriffe mit Verzögerung pro Request - beim ersten Lauf mit allen Detailseiten kann das 30-60 Minuten dauern, danach nur noch wenige Minuten). Lade diese Seite später neu, um das Ergebnis zu sehen.",
  "intern.wkoSyncFailed": "WKO-Sync konnte nicht gestartet werden. Ist die API erreichbar?",
  "intern.wkoActiveCompanies": "Aktive Firmen",
  "intern.wkoRemovedCompanies": "Nicht mehr gelistet",
  "intern.wkoStartSync": "WKO-Sync jetzt starten",
  "intern.wkoSyncExplainer":
    "Durchsucht firmen.wko.at nach Immobilien-Firmen in Oberösterreich (Makler, Treuhänder, Verwaltung, Büro) und speichert Kontaktdaten, Firmendaten und Gewerbeberechtigungen. Bereits bekannte Firmen werden beim erneuten Lauf übersprungen - nur neue Firmen werden voll gescraped. Läuft nicht automatisch - nur bei manuellem Auslösen hier.",

  // Nutzerverwaltung (/intern/nutzer)
  "intern.usersMetaTitle": "Intern – Nutzer",
  "intern.usersTitle": "Nutzer",
  "intern.usersIntro": "Registrierte Nutzer, neueste Registrierungen zuerst.",
  "intern.usersSearchPlaceholder": "Name, E-Mail oder Firma suchen …",
  "intern.searchButton": "Suchen",
  "intern.usersEmpty": "Keine Nutzer gefunden.",
  "intern.usersLoadFailed":
    "Nutzer konnten nicht geladen werden. Ist die API erreichbar und ADMIN_API_KEY gesetzt?",
  "intern.usersTotal": "{count} Nutzer",
  "intern.usersColUser": "Nutzer",
  "intern.usersColRole": "Rolle",
  "intern.usersColVerified": "E-Mail bestätigt",
  "intern.usersColRegistered": "Registriert",
  "intern.usersColListings": "Inserate",
  "intern.roleBuyer": "Käufer",
  "intern.roleSellerPrivate": "Privatverkäufer",
  "intern.roleBroker": "Makler",
  "intern.rolePropertyManager": "Hausverwaltung",
  "intern.roleAdmin": "Admin",
  "intern.verifiedYes": "Ja",
  "intern.verifiedNo": "Nein",
  "intern.usersShowListings": "Inserate anzeigen",

  // Immobilienverwaltung (/intern/immobilien)
  "intern.propsMetaTitle": "Intern – Immobilien",
  "intern.propsTitle": "Immobilien",
  "intern.propsIntro":
    "Alle Inserate inklusive ausgeblendeter - Nutzer-Inserate und Zwangsversteigerungen. Ausblenden entfernt ein Inserat aus Web und App, ohne es zu löschen.",
  "intern.propsSearchPlaceholder": "Titel, Adresse, Ort, Anbieter oder E-Mail …",
  "intern.propsFilterSourceAll": "Alle Quellen",
  "intern.propsFilterSourceUser": "Nutzer-Inserate",
  "intern.propsFilterSourceForeclosure": "Zwangsversteigerungen",
  "intern.propsFilterStatusAll": "Sichtbar + ausgeblendet",
  "intern.propsFilterStatusVisible": "Nur sichtbare",
  "intern.propsFilterStatusHidden": "Nur ausgeblendete",
  "intern.filterButton": "Filtern",
  "intern.propsFilterUserNote": "Gefiltert nach Nutzer: {user}",
  "intern.filterReset": "Filter zurücksetzen",
  "intern.propsEmpty": "Keine Inserate gefunden.",
  "intern.propsLoadFailed":
    "Inserate konnten nicht geladen werden. Ist die API erreichbar und ADMIN_API_KEY gesetzt?",
  "intern.propsTotal": "{count} Inserate",
  "intern.badgeHidden": "Ausgeblendet",
  "intern.badgeForeclosure": "Zwangsversteigerung",
  "intern.propsOwnerPrefix": "Anbieter:",
  "intern.propsCreatedPrefix": "Eingestellt:",
  "intern.actionHide": "Ausblenden",
  "intern.actionShow": "Einblenden",
  "intern.actionDelete": "Löschen",
  "intern.actionOpen": "Öffnen",
  "intern.confirmDelete":
    "Dieses Inserat endgültig löschen? Das kann nicht rückgängig gemacht werden.",
  "intern.actionHiddenOk": "Inserat wurde ausgeblendet.",
  "intern.actionShownOk": "Inserat wurde wieder eingeblendet.",
  "intern.actionDeletedOk": "Inserat wurde gelöscht.",
  "intern.actionFailed":
    "Aktion fehlgeschlagen. Ist die API erreichbar und ADMIN_API_KEY gesetzt?",

  // Paging (beide Verwaltungsseiten)
  "intern.pagePrev": "Zurück",
  "intern.pageNext": "Weiter",
  "intern.pageInfo": "Seite {page} von {pages}",

  // Marketing (/intern/marketing)
  "intern.marketingMetaTitle": "Intern – Marketing",
  "intern.marketingTitle": "Marketing",
  "intern.marketingIntro":
    "E-Mail an einen Kontakt: Text selbst schreiben oder per KI aus Stichwörtern generieren lassen, prüfen und anpassen, dann von info@heimatplatz.at versenden. Die Signatur mit den Impressum-Kontaktdaten wird beim Versand automatisch angehängt.",
  "intern.marketingEmailHeading": "Empfänger",
  "intern.marketingRecipientLabel": "Empfänger-E-Mail",
  "intern.marketingRecipientPlaceholder": "kontakt@beispiel.at",
  "intern.marketingRecipientNameLabel": "Empfänger-Name (optional, für die Anrede)",
  "intern.marketingRecipientNamePlaceholder": "z. B. Frau Maier, Firma Muster GmbH",
  "intern.marketingCcLabel": "CC (optional, erhält eine offene Kopie)",
  "intern.marketingCcPlaceholder": "kopie@beispiel.at",
  "intern.marketingBccLabel": "BCC (optional, erhält eine verdeckte Kopie – für andere Empfänger unsichtbar)",
  "intern.marketingBccPlaceholder": "verdeckte-kopie@beispiel.at",
  "intern.marketingAiHeading": "Text per KI generieren (optional)",
  "intern.marketingAiHint":
    "Wer den Text lieber selbst schreibt, überspringt diesen Schritt und tippt unten direkt in den Entwurf.",
  "intern.marketingKeywordsLabel": "Stichwörter / Inhalt",
  "intern.marketingKeywordsPlaceholder":
    "Worum geht es? Anlass, gewünschte Punkte, Tonalität – die KI macht daraus den E-Mail-Text.",
  "intern.marketingGenerate": "Text generieren",
  "intern.marketingRegenerate": "Neu generieren",
  "intern.marketingGenerating":
    "Die KI erstellt den Text – das kann ein bis zwei Minuten dauern …",
  "intern.marketingGenerateFailed": "Generierung fehlgeschlagen: {error}",
  "intern.marketingConfirmOverwrite":
    "Betreff und E-Mail-Text werden durch den KI-Vorschlag ersetzt. Fortfahren?",
  "intern.marketingDraftHeading": "Entwurf schreiben & senden",
  "intern.marketingSubjectLabel": "Betreff",
  "intern.marketingBodyLabel": "E-Mail-Text",
  "intern.marketingBodyPlaceholder":
    "E-Mail-Text hier selbst schreiben – oder oben per KI generieren lassen. Grußformel ohne Namen, die Signatur folgt automatisch.",
  "intern.marketingSignatureLabel": "Signatur (wird automatisch angehängt)",
  "intern.marketingSend": "E-Mail senden",
  "intern.marketingSending": "E-Mail wird versendet …",
  "intern.marketingConfirmSend": "E-Mail jetzt an {email} senden?",
  "intern.marketingConfirmCc": "CC an {cc}",
  "intern.marketingConfirmBcc": "BCC an {bcc}",
  "intern.marketingSendOk": "E-Mail wurde an {email} versendet.",
  "intern.marketingSendOkNoSmtp":
    "Achtung: Es ist kein SMTP-Server konfiguriert (EMAIL_SMTP_* fehlt in der Server-.env) – die E-Mail wurde nur im API-Log ausgegeben, NICHT zugestellt.",
  "intern.marketingSendFailed": "Versand fehlgeschlagen: {error}",
  "intern.marketingValidationRecipient": "Bitte eine gültige Empfänger-E-Mail-Adresse eingeben.",
  "intern.marketingValidationCc": "Bitte eine gültige CC-E-Mail-Adresse eingeben (oder das Feld leer lassen).",
  "intern.marketingValidationBcc": "Bitte eine gültige BCC-E-Mail-Adresse eingeben (oder das Feld leer lassen).",
  "intern.marketingValidationKeywords": "Bitte Stichwörter eingeben.",
  "intern.marketingValidationDraft": "Betreff und E-Mail-Text dürfen nicht leer sein.",
  "intern.marketingApiUnreachable":
    "API nicht erreichbar oder ADMIN_API_KEY fehlt.",
  "intern.marketingSentContactLink": "Zum Kontakt",

  // Marketing-Dashboard (/intern/marketing)
  "intern.marketingDashIntro":
    "Marketing-Zentrale: E-Mails mit KI-Text erstellen und versenden, Rückmeldungen sammeln und die Kontaktdatenbank potentieller Kunden pflegen.",
  "intern.marketingStatsHeading": "Auswertung",
  "intern.marketingStatsContacts": "Kontakte gesamt",
  "intern.marketingStatsLeads": "Leads",
  "intern.marketingStatsContacted": "Kontaktiert",
  "intern.marketingStatsReplied": "Antwort erhalten",
  "intern.marketingStatsInterested": "Interessiert",
  "intern.marketingStatsCustomers": "Kunden",
  "intern.marketingStatsEmails30": "Mails (30 Tage)",
  "intern.marketingStatsEmailsTotal": "Mails gesamt",
  "intern.marketingStatsReplies30": "Antworten (30 Tage)",
  "intern.marketingStatsUnread": "Ungelesen",
  "intern.marketingStatsReplyRate": "Antwortquote",
  "intern.marketingNavCompose": "E-Mail schreiben",
  "intern.marketingNavComposeDescription":
    "Stichwörter eingeben, Text per KI generieren, prüfen und von info@heimatplatz.at versenden.",
  "intern.marketingNavInbox": "Posteingang",
  "intern.marketingNavInboxDescription":
    "Rückmeldungen auf Marketing-Mails und Nachrichten bekannter Kontakte.",
  "intern.marketingNavContacts": "Kontakte",
  "intern.marketingNavContactsDescription":
    "Kontaktdatenbank potentieller Kunden: Makler, Hausverwaltungen, Gemeinden, Partner.",
  "intern.marketingNavSent": "Gesendet",
  "intern.marketingNavSentDescription": "Versand-Historie aller Marketing-E-Mails.",
  "intern.marketingUnreadBadge": "{count} neu",

  // Kontakte (/intern/marketing/kontakte)
  "intern.mkContactsMetaTitle": "Intern – Marketing-Kontakte",
  "intern.mkContactsTitle": "Kontakte",
  "intern.mkContactsIntro":
    "Potentielle Kunden und Partner. Beim Versand einer Marketing-Mail wird der Empfänger automatisch hier angelegt.",
  "intern.mkContactsSearchPlaceholder": "E-Mail, Name oder Firma suchen …",
  "intern.mkContactsFilterStatusAll": "Alle Status",
  "intern.mkContactsFilterTypeAll": "Alle Typen",
  "intern.mkContactsEmpty": "Keine Kontakte gefunden.",
  "intern.mkContactsLoadFailed":
    "Kontakte konnten nicht geladen werden. Ist die API erreichbar und ADMIN_API_KEY gesetzt?",
  "intern.mkContactsTotal": "{count} Kontakte",
  "intern.mkContactsNewHeading": "Neuen Kontakt anlegen",
  "intern.mkContactColContact": "Kontakt",
  "intern.mkContactColStatus": "Status",
  "intern.mkContactColLastContact": "Letzter Kontakt",
  "intern.mkContactColActivity": "Mails / Antworten",
  "intern.mkContactDetail": "Detail",
  "intern.mkContactWrite": "E-Mail schreiben",
  "intern.mkContactSavedOk": "Kontakt wurde gespeichert.",
  "intern.mkContactDeletedOk": "Kontakt wurde gelöscht.",
  "intern.mkContactActionFailed": "Aktion fehlgeschlagen: {error}",
  "intern.mkContactConfirmDelete":
    "Diesen Kontakt samt Versand-Historie und Rückmeldungen endgültig löschen?",

  // Kontakt-Formular
  "intern.mkFieldEmail": "E-Mail",
  "intern.mkFieldName": "Name",
  "intern.mkFieldCompany": "Firma",
  "intern.mkFieldPhone": "Telefon",
  "intern.mkFieldType": "Typ",
  "intern.mkFieldStatus": "Status",
  "intern.mkFieldNotes": "Notizen",
  "intern.mkSave": "Speichern",
  "intern.mkDelete": "Löschen",

  // Kontakt-Detail (/intern/marketing/kontakte/detail)
  "intern.mkDetailMetaTitle": "Intern – Kontakt",
  "intern.mkDetailNotFound": "Kontakt wurde nicht gefunden.",
  "intern.mkDetailTimelineHeading": "Verlauf",
  "intern.mkDetailTimelineEmpty": "Noch keine E-Mails oder Rückmeldungen.",
  "intern.mkDetailSentPrefix": "Gesendet:",
  "intern.mkDetailReplyPrefix": "Antwort:",
  "intern.mkDetailSource": "Quelle",
  "intern.mkDetailCreated": "Angelegt",

  // Kontakt-Typen (MarketingContactType, API liefert Enum-Namen als Text)
  "intern.mkTypeUnknown": "Unbekannt",
  "intern.mkTypeBroker": "Makler",
  "intern.mkTypePropertyManager": "Hausverwaltung",
  "intern.mkTypePrivateSeller": "Privatverkäufer",
  "intern.mkTypeMunicipality": "Gemeinde",
  "intern.mkTypePartner": "Partner",
  "intern.mkTypeOther": "Sonstige",

  // Kontakt-Status (MarketingContactStatus)
  "intern.mkStatusLead": "Lead",
  "intern.mkStatusContacted": "Kontaktiert",
  "intern.mkStatusReplied": "Antwort erhalten",
  "intern.mkStatusInterested": "Interessiert",
  "intern.mkStatusCustomer": "Kunde",
  "intern.mkStatusNotInterested": "Kein Interesse",
  "intern.mkStatusDoNotContact": "Nicht kontaktieren",

  // Gesendet (/intern/marketing/gesendet)
  "intern.mkSentMetaTitle": "Intern – Gesendete Mails",
  "intern.mkSentTitle": "Gesendet",
  "intern.mkSentIntro": "Versand-Historie aller Marketing-E-Mails, neueste zuerst.",
  "intern.mkSentEmpty": "Noch keine E-Mails versendet.",
  "intern.mkSentLoadFailed":
    "Versand-Historie konnte nicht geladen werden. Ist die API erreichbar und ADMIN_API_KEY gesetzt?",
  "intern.mkSentTotal": "{count} E-Mails",
  "intern.mkSentBadgeLoggedOnly": "Nur geloggt",
  "intern.mkSentBadgeFailed": "Zustellung fehlgeschlagen",
  "intern.mkSentReplies": "{count} Antwort(en)",
  "intern.mkSentShowBody": "Text anzeigen",

  // Posteingang (/intern/marketing/eingang)
  "intern.mkInboxMetaTitle": "Intern – Posteingang",
  "intern.mkInboxTitle": "Posteingang",
  "intern.mkInboxIntro":
    "Rückmeldungen auf Marketing-Mails und Nachrichten bekannter Kontakte aus info@heimatplatz.at. Beim Öffnen wird das Postfach automatisch abgerufen (max. alle 5 Minuten).",
  "intern.mkInboxEmpty": "Keine Rückmeldungen vorhanden.",
  "intern.mkInboxLoadFailed":
    "Posteingang konnte nicht geladen werden. Ist die API erreichbar und ADMIN_API_KEY gesetzt?",
  "intern.mkInboxTotal": "{count} Rückmeldungen",
  "intern.mkInboxSyncNow": "Jetzt abrufen",
  "intern.mkInboxSyncOk": "Postfach abgerufen: {count} neue Rückmeldung(en).",
  "intern.mkInboxSyncFailed": "Abruf fehlgeschlagen: {error}",
  "intern.mkInboxNotConfigured":
    "Postfach-Abruf nicht konfiguriert (EMAIL_SMTP_* fehlt) – es werden nur bereits gespeicherte Rückmeldungen angezeigt.",
  "intern.mkInboxFilterAll": "Alle",
  "intern.mkInboxFilterUnread": "Nur ungelesene",
  "intern.mkInboxBadgeUnread": "Neu",
  "intern.mkInboxBadgeBounce": "Unzustellbar",
  "intern.mkInboxRepliedTo": "Antwort auf: {subject}",
  "intern.mkInboxMarkRead": "Als gelesen markieren",
  "intern.mkInboxMarkUnread": "Als ungelesen markieren",
  "intern.mkInboxShowBody": "Nachricht anzeigen",
  "intern.mkInboxReadOk": "Markierung aktualisiert.",

  // WKO-Firmen-Übersicht (/intern/firmen)
  "intern.navWkoCompanies": "Firmen (WKO)",
  "intern.navWkoCompaniesDescription":
    "Von firmen.wko.at gescrapte Immobilien-Firmen in Oberösterreich einsehen und durchsuchen.",
  "intern.wkoMetaTitle": "Intern – Firmen (WKO)",
  "intern.wkoTitle": "Firmen (WKO)",
  "intern.wkoIntro":
    "Immobilien-Firmen in Oberösterreich, gescraped von firmen.wko.at. Firmenbuch-Spalten (amtliches Gründungsdatum, EUID, Geschäftsführung) sind erst befüllt, sobald ein Firmenbuch-HVD-API-Key konfiguriert ist und der Sync erneut lief.",
  "intern.wkoSearchPlaceholder": "Name oder Branche suchen …",
  "intern.wkoFilterCityAll": "Alle Orte",
  "intern.wkoFilterStatusAll": "Alle",
  "intern.wkoFilterStatusActive": "Nur aktive",
  "intern.wkoFilterStatusInactive": "Nur nicht mehr gelistete",
  "intern.wkoLoadFailed": "Firmen konnten nicht geladen werden. Ist die API erreichbar?",
  "intern.wkoTotal": "{count} Firmen",
  "intern.wkoEmpty": "Keine Firmen gefunden.",
  "intern.wkoColFounded": "Gegründet",
  "intern.wkoFoundedOfficial": "amtlich",
  "intern.wkoFoundedApprox": "laut Gewerbeberechtigung",
  "intern.wkoBadgeInactive": "nicht mehr gelistet",
  "intern.wkoBadgeTrainingCompany": "Lehrbetrieb",
  "intern.wkoDetailLink": "Details",
  "intern.wkoWkoLink": "WKO-Eintrag",

  // WKO-Firmen-Detail (/intern/firmen/detail)
  "intern.wkoDetailMetaTitle": "Intern – Firmendetail",
  "intern.wkoDetailNotFound": "Firma nicht gefunden.",
  "intern.wkoDetailBackToList": "Zurück zur Übersicht",
  "intern.wkoDetailContactHeading": "Kontakt",
  "intern.wkoDetailCompanyHeading": "Firmendaten",
  "intern.wkoDetailFirmenbuchHeading": "Amtliche Firmenbuch-Daten",
  "intern.wkoDetailFirmenbuchNotEnriched":
    "Noch nicht angereichert (kein Firmenbuch-HVD-API-Key konfiguriert oder Sync steht noch aus).",
  "intern.wkoDetailPermitsHeading": "Gewerbeberechtigungen",
  "intern.wkoDetailPermitsEmpty": "Keine Gewerbeberechtigungen erfasst.",
  "intern.wkoDetailManagingDirectorsHeading": "Geschäftsführung laut Firmenbuch",
  "intern.wkoDetailScrapingHeading": "Scraping-Daten",
  "intern.wkoDetailFieldStreet": "Straße",
  "intern.wkoDetailFieldPostalCode": "PLZ",
  "intern.wkoDetailFieldCity": "Ort",
  "intern.wkoDetailFieldPhones": "Telefon",
  "intern.wkoDetailFieldEmail": "E-Mail",
  "intern.wkoDetailFieldWebsite": "Website",
  "intern.wkoDetailFieldOpeningHours": "Öffnungszeiten",
  "intern.wkoDetailFieldLegalForm": "Rechtsform",
  "intern.wkoDetailFieldCompanyRegisterNumber": "Firmenbuchnummer",
  "intern.wkoDetailFieldCompanyCourt": "Firmengericht",
  "intern.wkoDetailFieldGln": "GLN",
  "intern.wkoDetailFieldFoundedYear": "Gründungsjahr (Näherung)",
  "intern.wkoDetailFieldEuid": "EUID",
  "intern.wkoDetailFieldFirmenbuchFoundedDate": "Amtliches Gründungsdatum",
  "intern.wkoDetailFieldSourceSearchTerm": "Gefunden über Suchbegriff",
  "intern.wkoDetailFieldFirstSeenAt": "Erstmals gescraped",
  "intern.wkoDetailFieldLastScrapedAt": "Zuletzt gescraped",
  "intern.wkoDetailFieldFirmenbuchEnrichedAt": "Firmenbuch zuletzt angereichert",

  // Analytics-Section (/intern/analytics)
  "intern.analyticsMetaTitle": "Intern – Analytics",
  "intern.analyticsTitle": "Analytics",
  "intern.analyticsIntro":
    "Traffic-Analytics (Rybbit, selbstgehostet, cookieless) und Suchperformance (Google Search Console) an einer Stelle.",
  "intern.rybbitHeading": "Rybbit – Traffic-Analytics",
  "intern.rybbitExplainer":
    "Echtzeit-Besucherzahlen, Seitenaufrufe und Verhalten. Läuft auf einem eigenen Server (analytics.heimatplatz.at), eigenes Login.",
  "intern.rybbitOpenButton": "Rybbit-Dashboard öffnen",
  "intern.searchConsoleHeading": "Google Search Console – Suchperformance",
  "intern.searchConsoleExplainer": "Klicks, Impressionen und Ranking-Position der letzten 28 Tage aus der echten Google-Suche.",
  "intern.searchConsoleNotConfigured":
    "Noch nicht konfiguriert (kein Service-Account-Key hinterlegt). Siehe Features/SearchConsole/README.md für die Einrichtung.",
  "intern.searchConsoleLoadFailed": "Suchperformance-Daten konnten nicht geladen werden.",
  "intern.searchConsoleClicks": "Klicks",
  "intern.searchConsoleImpressions": "Impressionen",
  "intern.searchConsoleCtr": "CTR",
  "intern.searchConsolePosition": "Ø Position",
  "intern.searchConsoleTopQueries": "Top-Suchbegriffe",
  "intern.searchConsoleTopPages": "Top-Seiten",
  "intern.searchConsoleColQuery": "Suchbegriff",
  "intern.searchConsoleColPage": "Seite",
  "intern.searchConsoleColClicks": "Klicks",
  "intern.searchConsoleColImpressions": "Impr.",
  "intern.searchConsoleColCtr": "CTR",
  "intern.searchConsoleColPosition": "Position",

  // Feedback (/intern/feedback)
  "intern.navFeedback": "Feedback",
  "intern.navFeedbackDescription": "Nutzer-Anfragen lesen und beantworten (Wünsche, Probleme, Fragen, Lob).",
  "intern.fbMetaTitle": "Intern - Feedback",
  "intern.fbTitle": "Feedback",
  "intern.fbIntro":
    "Anfragen der Nutzer aus App und Web. Antworten landen im Verlauf des Nutzers und lösen eine Push-Benachrichtigung aus.",
  "intern.fbLoadFailed":
    "Anfragen konnten nicht geladen werden. Ist die API erreichbar und ADMIN_API_KEY gesetzt?",
  "intern.fbTotal": "{count} Anfragen",
  "intern.fbEmpty": "Keine Anfragen gefunden.",
  "intern.fbFilterStatus": "Status",
  "intern.fbFilterCategory": "Kategorie",
  "intern.fbFilterAll": "Alle",
  "intern.fbSearchPlaceholder": "Suche nach Betreff, Name oder E-Mail …",
  "intern.fbSearchSubmit": "Filtern",
  "intern.fbBadgeUnread": "Neu",
  "intern.fbMessageCount": "{count} Nachrichten",
  "intern.fbStatsOpen": "Offen",
  "intern.fbStatsInProgress": "In Arbeit",
  "intern.fbStatsUnread": "Ungelesen",
  "intern.fbStatsTotal": "Gesamt",

  // Feedback-Detail (/intern/feedback/detail)
  "intern.fbDetailMetaTitle": "Intern - Feedback-Anfrage",
  "intern.fbDetailNotFound": "Anfrage nicht gefunden.",
  "intern.fbDetailUser": "Nutzer",
  "intern.fbDetailSource": "Plattform",
  "intern.fbDetailAppVersion": "App-Version",
  "intern.fbDetailCreatedAt": "Erstellt",
  "intern.fbDetailAuthorTeam": "Heimatplatz-Team",
  "intern.fbDetailVoiceMessage": "Sprachnachricht",
  "intern.fbDetailAttachmentImageAlt": "Bild-Anhang",
  "intern.fbReplyHeading": "Antworten",
  "intern.fbReplyHint":
    "Die Antwort erscheint im Verlauf des Nutzers (App + Web) und löst eine Push-Benachrichtigung aus.",
  "intern.fbReplyPlaceholder": "Antwort an den Nutzer …",
  "intern.fbReplySubmit": "Antwort senden",
  "intern.fbReplyOk": "Antwort gesendet - der Nutzer wurde benachrichtigt.",
  "intern.fbReplyFailed": "Antwort konnte nicht gesendet werden.",
  "intern.fbStatusHeading": "Status",
  "intern.fbStatusSubmit": "Status setzen",
  "intern.fbStatusOk": "Status aktualisiert.",
  "intern.fbStatusFailed": "Status konnte nicht gesetzt werden.",
  "intern.fbCategoryIdea": "Wunsch / Idee",
  "intern.fbCategoryProblem": "Problem",
  "intern.fbCategoryQuestion": "Frage",
  "intern.fbCategoryPraise": "Lob",
  "intern.fbCategoryOther": "Sonstiges",
  "intern.fbStatusOpen": "Offen",
  "intern.fbStatusInProgress": "In Arbeit",
  "intern.fbStatusAnswered": "Beantwortet",
  "intern.fbStatusClosed": "Abgeschlossen",
  "intern.fbSourceWeb": "Web",
  "intern.fbSourceAndroid": "Android",
  "intern.fbSourceIos": "iOS",
  "intern.fbSourceWindows": "Windows",
  "intern.fbSourceUnknown": "Unbekannt",
} as const;
