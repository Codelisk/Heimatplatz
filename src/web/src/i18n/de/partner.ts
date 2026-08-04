/**
 * Partner-Seite (/partner/) und Intern-Pflege (/intern/partner/).
 * Design-Richtung laut docs/partner-seite-konzept.md: ruhig, Niveau der Inserats-Karten.
 */
export const partner = {
  // --- Oeffentliche Seite ---
  "partner.metaTitle": "Unsere Partner – regionale Makler auf Heimatplatz",
  "partner.metaDescription":
    "Diese regionalen Makler und Partner arbeiten mit Heimatplatz zusammen. Ihre Objekte werden automatisch übernommen und laufend aktualisiert.",
  "partner.kicker": "Gemeinsam für die Region",
  "partner.title": "Unsere Partner",
  "partner.intro":
    "Hinter den Inseraten auf Heimatplatz stehen regionale Maklerinnen und Makler, die Oberösterreich kennen. Diese Partner vertrauen uns ihre Objekte an.",
  "partner.statsPartnersLabel": "Partner",
  "partner.statsListingsLabel": "aktive Inserate",
  "partner.statsListingsLabelOne": "aktives Inserat",
  "partner.brokersHeading": "Makler-Partner",
  "partner.brokersSub":
    "Alle Objekte unserer Partner werden automatisch übernommen und laufend aktualisiert – die Inseratszahl kommt live aus der Datenbank.",
  "partner.categoryBroker": "Makler-Partner",
  "partner.categoryDataSource": "Datenquelle",
  "partner.sinceStampWord": "Partner seit",
  "partner.activeListings": "{count} aktive Inserate",
  "partner.activeListingsOne": "1 aktives Inserat",
  "partner.viewListings": "Inserate ansehen",
  "partner.websiteLink": "Website",
  "partner.logoAlt": "Logo von {name}",
  "partner.placeholderTitle": "Ihre Objekte auf Heimatplatz?",
  "partner.placeholderText":
    "Wir übernehmen Ihre Inserate automatisch – Sie gewinnen Sichtbarkeit in der Region und einen festen Platz auf dieser Seite.",
  "partner.placeholderCta": "Partner werden",
  "partner.transparencyLabel": "Transparenz:",
  "partner.transparencyText":
    "Zwangsversteigerungen stammen aus den öffentlichen Edikten der österreichischen Justiz (edikte.justiz.gv.at) und sind keine Partner-Inserate.",
  "partner.ctaTitle": "Ihre Immobilien auf Heimatplatz?",
  "partner.ctaText": "Kostenlos in der Startphase – Anbindung über OpenImmo oder direkt.",
  "partner.ctaButton": "Werden Sie Partner",

  // --- Intern-Pflege ---
  "partner.internMetaTitle": "Partner verwalten",
  "partner.internTitle": "Partner",
  "partner.internIntro":
    "Partner für die öffentliche Partner-Seite pflegen – Änderungen sind ohne Deploy sofort sichtbar.",
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
    "Verknüpft die Live-Inseratszahl – muss exakt dem SourceName des Import-Feeds entsprechen, z. B. immobaer.at. Leer = keine Zählung.",
  "partner.internFieldSellerName": "Anbietername in Inseraten",
  "partner.internFieldSellerNameHint":
    "Grundlage für den „Inserate ansehen“-Suchlink, z. B. Immobär Immobilien. Leer = Link nutzt den Partnernamen.",
  "partner.internFieldDisplayOrder": "Reihenfolge (niedrig = zuerst)",
  "partner.internFieldVisible": "Auf der Partner-Seite sichtbar",
  "partner.internFieldLogo": "Logo hochladen (PNG/JPG)",
  "partner.internFieldLogoHint":
    "Wird selbst gehostet (kein Hotlink). Ein neues Logo ersetzt das bisherige.",
  "partner.internLogoCurrent": "Aktuelles Logo",
  "partner.internLogoRemove": "Logo entfernen",
  "partner.internSave": "Speichern",
} as const;
