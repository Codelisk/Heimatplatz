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
  "partner.metaTitle": "Partnerverzeichnis – regionale Makler auf Heimatplatz",
  "partner.metaDescription":
    "Das Partnerverzeichnis von Heimatplatz: regionale Maklerbetriebe aus Oberösterreich, deren Objekte automatisch übernommen und laufend aktualisiert werden.",
  "partner.kicker": "Heimatplatz · Gemeinsam für die Region",
  "partner.title": "Partnerverzeichnis",
  "partner.datelineStand": "Stand: {date}",
  "partner.datelineRegion": "Oberösterreich",
  "partner.datelinePartners": "{count} Partner",
  "partner.datelineListings": "{count} aktive Inserate",
  "partner.datelineListingsOne": "1 aktives Inserat",
  "partner.lead":
    "Hinter den Inseraten auf Heimatplatz stehen regionale Maklerinnen und Makler, die ihre Gemeinden persönlich kennen. Dieses Verzeichnis führt alle Betriebe, die uns ihre Objekte anvertrauen – mit Zahlen direkt aus der Datenbank statt Werbeversprechen.",

  // --- Verzeichnis-Eintraege ---
  "partner.directoryHeading": "Makler-Partner",
  "partner.categoryBroker": "Makler-Partner",
  "partner.categoryDataSource": "Datenquelle",
  "partner.factSince": "Partner seit {year}",
  "partner.factAutoImport": "Objekte werden automatisch übernommen",
  "partner.factListings": "{count} aktive Inserate",
  "partner.factListingsOne": "1 aktives Inserat",
  "partner.viewListings": "Inserate ansehen",
  "partner.websiteLink": "Website",
  "partner.logoAlt": "Logo von {name}",
  "partner.emptyDirectory":
    "Das Verzeichnis ist noch jung – der erste Eintrag Ihres Betriebs könnte hier stehen.",

  // --- Partnerschafts-Erklaerung ---
  "partner.charterHeading": "Wofür unsere Partner stehen",
  "partner.charter1Title": "Daten direkt vom Anbieter",
  "partner.charter1Text":
    "Objektdaten kommen unverändert vom Maklerbetrieb, werden automatisch übernommen und laufend aktualisiert – ohne Abschreiben, ohne Umwege.",
  "partner.charter2Title": "Regional verankert",
  "partner.charter2Text":
    "Unsere Partner arbeiten in Oberösterreich und kennen die Orte, die sie verkaufen – keine anonymen Portalanbieter.",
  "partner.charter3Title": "Klar gekennzeichnet",
  "partner.charter3Text":
    "Jedes Partner-Inserat nennt seine Herkunft. Zwangsversteigerungen stammen aus den öffentlichen Edikten der Justiz (edikte.justiz.gv.at) und sind keine Partner-Inserate.",

  // --- Anzeige in eigener Sache (CTA) ---
  "partner.adKicker": "Anzeige in eigener Sache",
  "partner.adTitle": "Ihre Objekte auf Heimatplatz?",
  "partner.adText":
    "Anbindung über OpenImmo oder direkt – kostenlos in der Startphase. Ihr Betrieb bekommt einen festen Eintrag in diesem Verzeichnis.",
  "partner.adButton": "Partner werden",

  // --- Partner-Nachweis auf Objekt-Detailseiten ---
  "partner.detailBadgeTitle": "Heimatplatz-Partnerbetrieb",
  "partner.detailBadgeSince": "Partner seit {year}",
  "partner.detailBadgeText": "Objektdaten kommen automatisch vom Makler",
  "partner.detailBadgeLink": "Zum Partnerverzeichnis",

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
