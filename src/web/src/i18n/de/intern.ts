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
    "E-Mail an einen Kontakt: Stichwörter eingeben, Text per KI generieren lassen, prüfen und anpassen, dann von info@heimatplatz.at versenden. Die Signatur mit den Impressum-Kontaktdaten wird beim Versand automatisch angehängt.",
  "intern.marketingEmailHeading": "E-Mail erstellen",
  "intern.marketingRecipientLabel": "Empfänger-E-Mail",
  "intern.marketingRecipientPlaceholder": "kontakt@beispiel.at",
  "intern.marketingRecipientNameLabel": "Empfänger-Name (optional, für die Anrede)",
  "intern.marketingRecipientNamePlaceholder": "z. B. Frau Maier, Firma Muster GmbH",
  "intern.marketingKeywordsLabel": "Stichwörter / Inhalt",
  "intern.marketingKeywordsPlaceholder":
    "Worum geht es? Anlass, gewünschte Punkte, Tonalität – die KI macht daraus den E-Mail-Text.",
  "intern.marketingGenerate": "Text generieren",
  "intern.marketingRegenerate": "Neu generieren",
  "intern.marketingGenerating":
    "Die KI erstellt den Text – das kann ein bis zwei Minuten dauern …",
  "intern.marketingGenerateFailed": "Generierung fehlgeschlagen: {error}",
  "intern.marketingDraftHeading": "Entwurf prüfen & senden",
  "intern.marketingSubjectLabel": "Betreff",
  "intern.marketingBodyLabel": "E-Mail-Text",
  "intern.marketingSignatureLabel": "Signatur (wird automatisch angehängt)",
  "intern.marketingSend": "E-Mail senden",
  "intern.marketingSending": "E-Mail wird versendet …",
  "intern.marketingConfirmSend": "E-Mail jetzt an {email} senden?",
  "intern.marketingSendOk": "E-Mail wurde an {email} versendet.",
  "intern.marketingSendOkNoSmtp":
    "Achtung: Es ist kein SMTP-Server konfiguriert (EMAIL_SMTP_* fehlt in der Server-.env) – die E-Mail wurde nur im API-Log ausgegeben, NICHT zugestellt.",
  "intern.marketingSendFailed": "Versand fehlgeschlagen: {error}",
  "intern.marketingValidationRecipient": "Bitte eine gültige Empfänger-E-Mail-Adresse eingeben.",
  "intern.marketingValidationKeywords": "Bitte Stichwörter eingeben.",
  "intern.marketingValidationDraft": "Betreff und E-Mail-Text dürfen nicht leer sein.",
  "intern.marketingApiUnreachable":
    "API nicht erreichbar oder ADMIN_API_KEY fehlt.",
} as const;
