/**
 * Zwangsversteigerungen: Detailseite /zwangsversteigerungen/[slug] und
 * user-sichtbare Labels aus features/foreclosures/api.ts. Key-Präfix: "zv."
 */
export const foreclosures = {
  // Kategorien (Werte identisch zu den bisherigen Labels — sie fließen auch in Slugs ein!)
  "zv.categoryEinfamilienhaus": "Einfamilienhaus",
  "zv.categoryZweifamilienhaus": "Zweifamilienhaus",
  "zv.categoryMehrfamilienhaus": "Mehrfamilienhaus",
  "zv.categoryWohnungseigentum": "Wohnungseigentum",
  "zv.categoryGewerblicheLiegenschaft": "Gewerbliche Liegenschaft",
  "zv.categoryGrundstueck": "Grundstück",
  "zv.categoryLandUndForstwirtschaft": "Land- und Forstwirtschaft",
  "zv.categorySonstiges": "Sonstiges",
  "zv.categoryFallback": "Zwangsversteigerung",

  // Format-Fallbacks
  "zv.dateOpen": "Termin offen",
  "zv.notSpecified": "Nicht angegeben",

  // Titel-/Beschreibungsmuster (Meta + Seitenkopf)
  "zv.titlePattern": "{category} in {postalCode} {city}",
  "zv.descriptionPattern": "{object} in {postalCode} {city}. {price}. Termin: {date}.{courtSuffix}",
  "zv.descriptionCourtSuffix": " Gericht: {court}.",

  // Detailseite: Badges + Fakten
  "zv.badge": "ZV",
  "zv.factDate": "Termin",
  "zv.factEstimatedValue": "Schätzwert",
  "zv.factMinimumBid": "Mindestgebot",
  "zv.factArea": "Fläche",

  // Detailseite: Abschnitte
  "zv.objectDescriptionHeading": "Objektbeschreibung",
  "zv.notesHeading": "Hinweise",
  "zv.documentsHeading": "Dokumente",

  // Detailseite: Gericht-Sidebar
  "zv.courtSource": "Gericht / Quelle",
  "zv.fallbackCourt": "Bezirksgericht",
  "zv.caseNumber": "Aktenzeichen {number}",
  "zv.courtAuction": "Gerichtliche Immobilienversteigerung",
  "zv.priceHint": "Mindestgebot oder Schätzwert, soweit veröffentlicht.",
  "zv.officialDocument": "Amtliches Dokument",
  "zv.openEdict": "Edikt öffnen",
  "zv.openEdictHint": "Alle Angaben im Original-Edikt ansehen",
  "zv.edictUnavailable": "Edikt noch nicht verfügbar.",
  "zv.firstSeen": "Erfasst: {date}",
  "zv.updated": "Aktualisiert: {date}",

  // Detailseite: Schnellzugriff-Leiste unten
  "zv.quickAccess": "Schnellzugriff",
  "zv.dateShort": "Termin: {date}",
  "zv.viewEdict": "Edikt ansehen",
  "zv.edictPending": "Edikt offen",

  // Dokument-Links
  "zv.docEdict": "Edikt",
  "zv.docFloorPlan": "Grundriss",
  "zv.docSitePlan": "Lageplan",
  "zv.docLongAppraisal": "Langschätzung",
  "zv.docShortAppraisal": "Kurzschätzung",

  // Detail-Sektionen (api.ts): Abschnittstitel
  "zv.sectionAuction": "Versteigerung",
  "zv.sectionBasics": "Basisdaten",
  "zv.sectionLegal": "Rechtliches",

  // Detail-Sektionen: Zeilen-Labels
  "zv.labelDate": "Termin",
  "zv.labelEstimatedValue": "Schätzwert",
  "zv.labelMinimumBid": "Mindestgebot",
  "zv.labelStatus": "Status",
  "zv.labelOwnershipShare": "Eigentumsanteil",
  "zv.labelViewing": "Besichtigung",
  "zv.labelBiddingDeadline": "Gebotsfrist",
  "zv.labelCategory": "Kategorie",
  "zv.labelCity": "Ort",
  "zv.labelAddress": "Adresse",
  "zv.labelTotalArea": "Gesamtfläche",
  "zv.labelPlot": "Grundstück",
  "zv.labelBuildingArea": "Bebaute Fläche",
  "zv.labelRooms": "Zimmer",
  "zv.labelYearBuilt": "Baujahr",
  "zv.labelCondition": "Zustand",
  "zv.labelCourt": "Gericht",
  "zv.labelCaseNumber": "Aktenzeichen",
  "zv.labelRegistrationNumber": "Einlagezahl",
  "zv.labelCadastralMunicipality": "Katastralgemeinde",
  "zv.labelPlotNumber": "Grundstücksnummer",
  "zv.labelSheet": "Blatt",
  "zv.labelZoning": "Flächenwidmung",
} as const;
