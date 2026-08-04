/**
 * Partnerverzeichnis (/partner/), Partner-Nachweis auf Objekt-Detailseiten und
 * Intern-Pflege (/intern/partner/).
 *
 * Design-Richtung laut docs/partner-seite-konzept.md (Blue-Ocean-Fassung):
 * redaktionelles Verzeichnis im Regionalblatt-Stil - nachpruefbare Fakten statt
 * Marketing, Rot nur als Signalfarbe, keine Effekt-Gimmicks.
 */
export const partner = {
  // --- Oeffentliche Seite: Masthead ---
  // Bewusst "Makler" statt "Partner" (Kundenwunsch 4.8.): keine offizielle
  // Partnerprogramm-Anmutung, sondern schlicht die regionalen Maklerbueros,
  // deren Objekte auf Heimatplatz erscheinen. Route bleibt /partner/.
  "partner.metaTitle": "Maklerverzeichnis – regionale Makler auf Heimatplatz",
  "partner.metaDescription":
    "Das Maklerverzeichnis von Heimatplatz: regionale Maklerbüros aus Oberösterreich, deren Objekte auf Heimatplatz erscheinen – mit Zahlen direkt aus der Datenbank.",
  "partner.kicker": "Heimatplatz · Gemeinsam für die Region",
  "partner.title": "Maklerverzeichnis",
  "partner.datelineStand": "Stand: {date}",
  "partner.datelineRegion": "Oberösterreich",
  "partner.datelinePartners": "{count} Makler",
  "partner.datelineListings": "{count} aktive Inserate",
  "partner.datelineListingsOne": "1 aktives Inserat",
  "partner.lead":
    "Hinter den Inseraten auf Heimatplatz stehen regionale Maklerinnen und Makler, die ihre Gemeinden persönlich kennen. Dieses Verzeichnis führt die Maklerbüros, deren Objekte bei uns erscheinen – mit Zahlen direkt aus der Datenbank statt Werbeversprechen.",

  // --- Verzeichnis-Eintraege ---
  "partner.directoryHeading": "Aus der Region",
  "partner.directoryColumn": "Zahlen live aus der Datenbank",
  "partner.categoryBroker": "Maklerbüro",
  "partner.categoryDataSource": "Datenquelle",
  "partner.metaSince": "Seit {year}",
  "partner.statListingsLabel": "aktive Inserate",
  "partner.statListingsLabelOne": "aktives Inserat",
  "partner.viewListings": "Inserate ansehen",
  "partner.websiteLink": "Website",
  "partner.logoAlt": "Logo von {name}",
  "partner.emptyDirectory":
    "Das Verzeichnis ist noch jung – der erste Eintrag Ihres Büros könnte hier stehen.",

  // --- Randspalte: Zahlen-Kasten ---
  "partner.railStatsHeading": "In Zahlen",
  "partner.railStatsBrokers": "Makler",
  "partner.railStatsListings": "Aktive Inserate",
  "partner.railStatsStand": "Stand",

  // --- Fussnote (Edikte-Transparenz) ---
  "partner.footnote":
    "Zwangsversteigerungen stammen aus den öffentlichen Edikten der österreichischen Justiz (edikte.justiz.gv.at) und sind keine Partner-Inserate.",

  // --- Anzeige in eigener Sache (CTA) ---
  "partner.adKicker": "Anzeige in eigener Sache",
  "partner.adTitle": "Ihre Objekte auf Heimatplatz?",
  "partner.adText":
    "Wir übernehmen Ihre Inserate automatisch – kostenlos in der Startphase. Ihr Büro bekommt einen festen Eintrag in diesem Verzeichnis.",
  "partner.adButton": "Jetzt melden",

  // --- Makler-Nachweis auf Objekt-Detailseiten ---
  "partner.detailBadgeTitle": "Makler aus der Region",
  "partner.detailBadgeSince": "Seit {year} auf Heimatplatz",
  "partner.detailBadgeText": "Inserate kommen direkt vom Maklerbüro",
  "partner.detailBadgeLink": "Zum Maklerverzeichnis",

  // --- Intern-Pflege ---
  "partner.internMetaTitle": "Partner verwalten",
  "partner.internTitle": "Partner",
  "partner.internIntro":
    "Partner für das öffentliche Partnerverzeichnis pflegen – Änderungen sind ohne Deploy sofort sichtbar.",
  "partner.internSaved": "Partner gespeichert.",
  "partner.internDeleted": "Partner gelöscht.",
  "partner.internFailed": "Aktion fehlgeschlagen: {error}",
  "partner.internListHeading": "Alle Partner",
  "partner.internEmpty": "Noch keine Partner angelegt.",
  "partner.internHiddenTag": "ausgeblendet",
  "partner.internListListings": "{count} Inserate",
  "partner.internEdit": "Bearbeiten",
  "partner.internDelete": "Löschen",
  "partner.internDeleteConfirm": "Diesen Partner endgültig löschen? Das hochgeladene Logo wird mitgelöscht.",
  "partner.internNewHeading": "Neuen Partner anlegen",
  "partner.internEditHeading": "Partner bearbeiten",
  "partner.internCancelEdit": "Bearbeiten abbrechen",
  "partner.internFieldName": "Name",
  "partner.internFieldCategory": "Kategorie",
  "partner.internFieldDescription": "Kurzbeschreibung (2–3 Sätze)",
  "partner.internFieldWebsite": "Website (https://…)",
  "partner.internFieldRegion": "Region",
  "partner.internFieldRegionPlaceholder": "z. B. Innviertel, Oberösterreich",
  "partner.internFieldSinceYear": "Partner seit (Jahr)",
  "partner.internFieldSourceName": "Quellname des Feeds (Property.SourceName)",
  "partner.internFieldSourceNameHint":
    "Verknüpft Live-Inseratszahl und Detailseiten-Nachweis – muss exakt dem SourceName des Import-Feeds entsprechen, z. B. immobaer.at. Leer = keine Zählung.",
  "partner.internFieldSellerName": "Anbietername in Inseraten",
  "partner.internFieldSellerNameHint":
    "Grundlage für den „Inserate ansehen“-Suchlink, z. B. Immobär Immobilien. Leer = Link nutzt den Partnernamen.",
  "partner.internFieldDisplayOrder": "Reihenfolge (niedrig = zuerst)",
  "partner.internFieldVisible": "Im Partnerverzeichnis sichtbar",
  "partner.internFieldLogo": "Logo hochladen (PNG/JPG)",
  "partner.internFieldLogoHint":
    "Wird selbst gehostet (kein Hotlink). Ein neues Logo ersetzt das bisherige.",
  "partner.internLogoCurrent": "Aktuelles Logo",
  "partner.internLogoRemove": "Logo entfernen",
  "partner.internSave": "Speichern",
} as const;
