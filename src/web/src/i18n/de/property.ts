/**
 * Immobilien-Detailseite, Bildergalerie und Kontakt-Elemente.
 * Key-Präfixe: "property." (Format-Fallbacks), "detail." (Detailseite +
 * Detail-Sektionen), "gallery." (ImageGallery/Lightbox), "contact." (ContactDock + Kontakt-CTAs).
 */
export const property = {
  // Format-Fallbacks (format.ts)
  "property.priceOpen": "Preis offen",
  "property.priceOnRequest": "Preis auf Anfrage",

  // Detailseite: Meta / Kopf
  "detail.metaTitle": "{title} in {city}",
  "detail.metaDescription": "{title} in {postalCode} {city}, Oberösterreich. {price}. {area}.",

  // Detailseite: Aktions-Buttons auf dem Foto
  "detail.actionFavorite": "Merken",
  "detail.actionBlock": "Blockieren",
  "detail.actionShare": "Teilen",

  // Detailseite: Fakten-Kacheln
  "detail.factPrice": "Preis",
  "detail.factArea": "Fläche",
  "detail.factBuiltArea": "Bebaute Fläche",
  "detail.factLivingArea": "Wohnfläche",
  "detail.factPlot": "Grund",
  "detail.factRooms": "Zimmer",
  "detail.noInfo": "Keine Angabe",

  // Detailseite: Abschnitts-Überschriften
  "detail.sectionDetails": "Details",
  "detail.sectionFeaturesHeading": "Ausstattung & Merkmale",
  "detail.sectionDescription": "Beschreibung",

  // Detailseite: Anbieter-Sidebar
  "detail.sellerCourt": "Gericht",
  "detail.sellerProvider": "Anbieter",
  "detail.updatedAt": "Aktualisiert: {value}",
  "detail.liveOffer": "Live-Angebot",
  "detail.sourceLabel": "Quelle",
  "detail.openOriginal": "Original-Inserat öffnen",
  "detail.openOriginalHint": "Alle Angaben direkt beim Anbieter ansehen",

  // Detail-Sektionen (detail-sections.ts): Abschnittstitel
  "detail.sectionBasics": "Basisdaten",
  "detail.sectionAreas": "Flächen",
  "detail.sectionBuilding": "Gebäude",
  "detail.sectionEquipment": "Ausstattung",
  "detail.sectionPlot": "Grundstück",
  "detail.sectionAuction": "Versteigerung",
  "detail.sectionCosts": "Kosten",

  // Detail-Sektionen: Zeilen-Labels
  "detail.labelPurchasePrice": "Kaufpreis",
  "detail.labelEstimatedValue": "Schätzwert",
  "detail.labelMinimumBid": "Mindestgebot",
  "detail.labelPropertyType": "Immobilienart",
  "detail.labelPostalCode": "PLZ",
  "detail.labelCity": "Ort",
  "detail.labelAddress": "Adresse",
  "detail.labelLivingArea": "Wohnfläche",
  "detail.labelPlotArea": "Grundstücksfläche",
  "detail.labelTotalArea": "Gesamtfläche",
  "detail.labelBuildingArea": "Bebaute Fläche",
  "detail.labelRooms": "Zimmer",
  "detail.labelBedrooms": "Schlafzimmer",
  "detail.labelBathrooms": "Badezimmer",
  "detail.labelFloors": "Stockwerke",
  "detail.labelYearBuilt": "Baujahr",
  "detail.labelCondition": "Zustand",
  "detail.labelApartmentFloor": "Etage",
  "detail.labelBuildingCondition": "Gebäudezustand",
  "detail.labelGarage": "Garage",
  "detail.labelGarden": "Garten",
  "detail.labelBasement": "Keller",
  "detail.labelElevator": "Aufzug",
  "detail.labelZoning": "Widmung",
  "detail.labelBuildingRights": "Baurecht",
  "detail.labelBuildable": "Bebaubar",
  "detail.labelUtilities": "Versorgung",
  "detail.labelSoilQuality": "Bodenqualität",
  "detail.labelCadastralMunicipality": "Katastralgemeinde",
  "detail.labelPlotNumber": "Grundstücksnummer",
  "detail.labelRegistrationNumber": "Einlagezahl",
  "detail.labelCourt": "Gericht",
  "detail.labelFileNumber": "Aktenzeichen",
  "detail.labelAuctionDate": "Termin",
  "detail.labelStatus": "Status",
  "detail.labelViewingDate": "Besichtigung",
  "detail.labelBiddingDeadline": "Bietfrist",
  "detail.labelOwnershipShare": "Eigentumsanteil",
  "detail.labelEdictUrl": "Edikt",
  "detail.openEdict": "Edikt öffnen",
  "detail.linkCopied": "Link kopiert",
  "detail.labelPricePerM2": "Preis / m²",
  "detail.labelCreatedAt": "Eingestellt am",

  // Detail-Sektionen: Wert-Übersetzungen (Enums)
  "detail.yes": "Ja",
  "detail.no": "Nein",
  "detail.conditionLikeNew": "Neuwertig",
  "detail.conditionGood": "Gut",
  "detail.conditionAverage": "Durchschnittlich",
  "detail.conditionNeedsRenovation": "Sanierungsbedürftig",
  "detail.zoningResidential": "Wohngebiet",
  "detail.zoningCommercial": "Gewerbegebiet",
  "detail.zoningIndustrial": "Industriegebiet",
  "detail.zoningAgricultural": "Landwirtschaft",
  "detail.zoningMixed": "Mischgebiet",
  "detail.soilHigh": "Hoch",
  "detail.soilMedium": "Mittel",
  "detail.soilLow": "Niedrig",
  "detail.statusPending": "Anhängig",
  "detail.statusScheduled": "Terminiert",
  "detail.statusInProgress": "Laufend",
  "detail.statusCompleted": "Abgeschlossen",
  "detail.statusCancelled": "Aufgehoben",

  // Bildergalerie + Lightbox
  "gallery.imageAlt": "{title} – Bild {index} von {count}",
  "gallery.openImageAria": "{title} – Bild {index} in Großansicht öffnen",
  "gallery.viewPhoto": "Foto ansehen",
  "gallery.allPhotos": "Alle {count} Fotos",
  "gallery.morePhotos": "+{count} Fotos",
  "gallery.lightboxLabel": "Bildergalerie: {title}",
  "gallery.openOriginalTab": "Original in neuem Tab öffnen",
  "gallery.close": "Galerie schließen",
  "gallery.prev": "Vorheriges Bild",
  "gallery.next": "Nächstes Bild",
  "gallery.showImage": "Bild {index} anzeigen",

  // Kontakt (Sidebar + ContactDock)
  "contact.copy": "Kopieren",
  "contact.copied": "Kopiert",
  "contact.copyFailed": "Nicht kopiert",
  "contact.copyEmail": "E-Mail {email} kopieren",
  "contact.copyPhone": "Telefonnummer {phone} kopieren",
  "contact.writeEmail": "E-Mail schreiben",
  "contact.call": "Anrufen",
  "contact.viaProvider": "Kontakt über Anbieterangaben oder Originalinserat.",
  "contact.notProvided": "Direkter Kontakt ist nicht angegeben.",
  "contact.originalListing": "Originalinserat",
  "contact.dockLabel": "Schneller Kontakt",
  "contact.dockToggle": "Kontakt",
} as const;
