/**
 * Startseite und Immobilien-Suche: Meta-Texte, Filterleiste (mobil/Desktop),
 * OrtPicker, Sortierung, Pagination, Filtereinstellungs-Seite sowie die
 * Immobilien-Karten (SSR- und client-gerenderte Varianten inkl. Login-Dialog).
 * Key-Präfixe: "home." / "search." / "card."
 */
export const home = {
  // Startseite (Meta)
  "home.metaTitle": "Heimatplatz",
  "home.metaDescription":
    "Immobilien in Oberösterreich finden: Häuser, Wohnungen, Grundstücke und Zwangsversteigerungen mit Filtern und Push-Benachrichtigungen.",

  // Trefferliste und Status
  "search.resultCountOne": "{count} Objekt",
  "search.resultCountMany": "{count} Objekte",
  "search.loading": "Angebote werden geladen...",
  "search.loadError": "Angebote konnten nicht geladen werden. Bitte später erneut versuchen.",
  "search.emptyState": "Keine Treffer für diese Filter. Bitte Ort, Zeitraum oder Anbieter anpassen.",

  // Sortierung
  "search.sortLabel": "Sortierung",
  "search.sortNewest": "Neueste",
  "search.sortOldest": "Älteste",
  "search.sortPriceAsc": "Preis aufsteigend",
  "search.sortPriceDesc": "Preis absteigend",
  "search.sortAreaDesc": "Fläche absteigend",
  "search.sortAreaAsc": "Fläche aufsteigend",
  "search.sortPostalAsc": "PLZ aufsteigend",
  "search.sortPostalDesc": "PLZ absteigend",
  "search.sortPostal": "PLZ",

  // Pagination
  "search.paginationLabel": "Suchergebnisse Seiten",
  "search.pagePrev": "Vorherige Seite",
  "search.pageNext": "Nächste Seite",
  "search.pageOf": "Seite {current} von {total}",

  // Filterleiste (mobil) und Filter-Chips — geteilte Labels leben in
  // common.ts unter "filter.*" (period*, type*, seller*, locationPlaceholder)
  "search.filterToggle": "Filter",
  "search.filterReset": "Filter zurücksetzen",
  "search.ageAll": "Alle",
  "search.typeForeclosurePlural": "Zwangsversteigerungen",

  // OrtPicker
  "search.ortPlaceholderMulti": "Orte auswählen",
  "search.ortPickerTitleSingle": "Ort auswählen",
  "search.ortPickerTitleMulti": "Orte auswählen",
  "search.ortPickerSelectAllRegion": "Alle Orte in {region} auswählen",
  "search.ortPickerCountSelected": "{count} Orte ausgewählt",
  "search.ortPickerRegionCount": "{count} ausgewählt",
  "search.ortPickerWholeRegion": "{region} (ganzer Bezirk)",

  // Seite /filter-einstellungen/
  "search.settingsMetaTitle": "Filtereinstellungen",
  "search.settingsMetaDescription": "Standardfilter für die Immobiliensuche in Oberösterreich speichern.",
  "search.settingsTitle": "Filtereinstellungen",
  "search.settingsIntro": "Standardfilter für Ort, Immobilientyp, Anbieter, Zeitraum und Sortierung speichern.",
  "search.settingsOrteTitle": "Orte",
  "search.settingsOrteHint": "Mehrere Bezirke und Gemeinden auswählen. Keine Auswahl bedeutet: alle Orte in Oberösterreich.",
  "search.settingsAgeTitle": "Zeitraum",
  "search.settingsAgeHint": "Entspricht dem Zeitraum-Filter der Startseite.",
  "search.settingsTypeTitle": "Immobilientyp",
  "search.settingsSellerTitle": "Anbietertyp",
  "search.settingsSortTitle": "Sortierung",
  "search.settingsSortHint": "Die Standardsortierung entspricht den Optionen der Immobiliensuche.",
  "search.settingsSave": "Filter speichern",
  "search.settingsTypeRequired": "Bitte mindestens einen Immobilientyp auswählen.",
  "search.settingsSellerRequired": "Bitte mindestens einen Anbietertyp auswählen.",

  // Status-Meldungen der Einstellungs-Formulare (Filter- und Benachrichtigungs-Seite,
  // beide laufen über bindPreferenceForms in PropertyStateScript)
  "search.prefsSyncing": "Einstellungen werden synchronisiert...",
  "search.prefsSavedLocal": "Einstellungen lokal gespeichert.",
  "search.prefsSavedSynced": "Einstellungen gespeichert und synchronisiert.",
  "search.prefsSavedLocalLoginHint": "Einstellungen lokal gespeichert. Nach dem Login werden sie synchronisiert.",
  "search.prefsSyncFailed": "Lokal gespeichert, Online-Sync fehlgeschlagen.",
  "search.prefsSyncFailedDetail": "Lokal gespeichert, Online-Sync fehlgeschlagen: {message}",

  // Immobilien-Karten: Typ-Badges
  "card.typeHouse": "Haus",
  "card.typeLand": "Grund",
  "card.typeApartment": "Wohnung",
  "card.typeForeclosure": "Zwangsversteigerung",

  // Immobilien-Karten: Anbieter-Labels
  "card.sellerCourt": "Gericht",
  "card.sellerPrivate": "Privat",
  "card.sellerAgent": "Makler",
  "card.sellerManager": "Verwaltung",

  // Immobilien-Karten: Fakten
  "card.liveOffer": "Live-Angebot",
  "card.price": "Preis",
  "card.area": "Fläche",
  "card.areaUnknown": "Fläche k.A.",
  "card.areaOpen": "Fläche offen",
  "card.livingAreaValue": "{area} m² Wfl",
  "card.plotAreaValue": "{area} m² Grund",
  // ZV: LivingArea traegt die bebaute Flaeche aus dem Edikt - nicht als Wohnflaeche ausgeben
  "card.builtAreaValue": "{area} m² bebaut",
  "card.chipLivingArea": "{value} m² Wfl",
  "card.chipBuiltArea": "{value} m² bebaut",
  "card.chipRooms": "{value} Zi",
  // ZV-Karten: beschrifteter Auktionstermin statt unbeschriftetem Einstelldatum
  "card.auctionDate": "Termin {date}",
  // ZV-Karten: Countdown-Chip unten links im Foto - relative Angabe, das
  // absolute Datum bleibt in der Fusszeile (kein doppeltes Datum auf der Karte)
  "card.auctionToday": "Versteigerung heute",
  "card.auctionTomorrow": "Versteigerung morgen",
  "card.auctionInDays": "Versteigerung in {days} Tagen",

  // Immobilien-Karten: Aktionen — "Bearbeiten"/"Details" leben in common.ts
  // (common.edit/common.details)
  "card.favoriteAdd": "Favorit speichern",
  "card.favoriteRemove": "Favorit entfernen",
  "card.blockAdd": "Immobilie blockieren",
  "card.blockRemove": "Blockierung entfernen",
  "card.blockRelease": "Blockierung aufheben",
  "card.deleteConfirm": "Immobilie wirklich löschen?",
  "card.actionSyncFailed": "Die Änderung konnte nicht gespeichert werden.",
  "card.actionSyncFailedDetail": "Die Änderung konnte nicht gespeichert werden: {message}",

  // Login-Dialog (Gast tippt Favorit/Blockieren an) — CTAs nutzen
  // auth.login bzw. common.register
  "card.loginRequiredTitle": "Anmeldung erforderlich",
  "card.loginRequiredText": "Melden Sie sich an oder registrieren Sie sich, um Immobilien zu merken oder zu blockieren.",

  // Lokale Entwurfs-Karten (Meine Immobilien)
  "card.draftLocal": "Lokaler Entwurf",
  "card.draftNewTitle": "Neues Inserat",
  "card.draftNoAddress": "Adresse offen",
  "card.draftTypeFallback": "Immobilie",
  "card.draftImageAlt": "Immobilienfoto",
} as const;
