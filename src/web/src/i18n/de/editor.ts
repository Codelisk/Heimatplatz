/**
 * WYSIWYG-Inserats-Editor (/inserieren/, /immobilien/bearbeiten/) inkl.
 * PropertyWysiwygForm mit Live-Vorschau. Key-Präfix: "editor."
 */
export const editor = {
  // Seite /immobilien/bearbeiten/
  "editor.editTitle": "Immobilie bearbeiten",
  "editor.editMetaDescription": "Eigene Immobilie in Oberösterreich bearbeiten.",
  "editor.editAuthTitle": "Anmelden zum Bearbeiten",
  "editor.editAuthDescription": "Melden Sie sich an, um Ihre Immobilie zu bearbeiten.",

  // Seite /inserieren/
  "editor.createMetaTitle": "Immobilie in Oberösterreich inserieren",
  "editor.createMetaDescription":
    "Immobilie in Oberösterreich inserieren: Haus, Grundstück oder Zwangsversteigerung mit Fotos, Adresse, Preis und Kontaktdaten vorbereiten.",
  "editor.createHeading": "Immobilie inserieren",
  "editor.createAuthTitle": "Anmelden zum Inserieren",
  "editor.createAuthDescription":
    "Melden Sie sich an, um Ihre Immobilie in Oberösterreich zu inserieren – kostenlos als Privatperson, Makler oder Hausverwaltung.",

  // Formular-Aktionen
  "editor.submitSave": "Änderungen speichern",
  "editor.submitCreate": "Inserat speichern",

  // Foto-Bereich
  "editor.photoHelpEdit": "Bestehende Bilder bleiben erhalten, neue werden beim Speichern hochgeladen.",
  "editor.photoHelpCreate": "Mindestens ein Foto ist für ein veröffentlichtes Inserat erforderlich.",
  "editor.heroAlt": "Erstes Foto Ihres Inserats",
  "editor.addPhotos": "Fotos hinzufügen",
  "editor.photoFormats": "JPG, PNG oder WebP – klicken oder Bilder hierher ziehen, das erste wird zum Titelbild",
  "editor.photoCount": "Fotos {current} / {max}",
  "editor.photoPickerFailed":
    "Der Dateidialog konnte nicht geöffnet werden. Ziehen Sie die Bilder stattdessen auf die Fotofläche.",

  // Kopf / Live-Vorschau
  "editor.livePreviewHint":
    "Live-Vorschau: Tippen Sie direkt in Titel, Preis oder Beschreibung – genau so sehen Käufer Ihr Inserat.",
  "editor.propertyType": "Immobilientyp",

  // Adresse + Titel
  "editor.postalCodePlaceholder": "PLZ",
  "editor.postalCodeLabel": "Postleitzahl",
  "editor.cityLabel": "Ort",
  "editor.addressPlaceholder": "Straße und Hausnummer",
  "editor.addressLabel": "Adresse",
  "editor.titlePlaceholder": "Ihr Titel – z.B. Sonniges Einfamilienhaus mit Garten",
  "editor.titleLabel": "Titel des Inserats",

  // Fakten-Kacheln
  "editor.priceLabel": "Preis",
  "editor.livingAreaLabel": "Wohnfläche",
  "editor.plotLabel": "Grund",
  "editor.roomsLabel": "Zimmer",

  // Details
  "editor.detailsHeading": "Details",
  "editor.buildingHeading": "Gebäude",
  "editor.yearBuiltLabel": "Baujahr",

  // Ausstattung & Merkmale
  "editor.featuresHeading": "Ausstattung & Merkmale",
  "editor.featurePlaceholder": "Merkmal hinzufügen, z.B. Garage…",
  "editor.featureLabel": "Merkmal hinzufügen",
  "editor.featureHint": "Enter fügt hinzu. Ohne eigene Merkmale zeigt das Inserat automatisch Typ, Ort und Fläche.",
  "editor.removeFeature": "Merkmal {feature} entfernen",

  // Beschreibung
  "editor.descriptionHeading": "Beschreibung",
  "editor.descriptionPlaceholder":
    "Beschreiben Sie Lage, Zustand und Besonderheiten Ihrer Immobilie (mind. 50 Zeichen)…",

  // Versteigerungsdaten (Foreclosure-Felder)
  "editor.foreclosureLegend": "Versteigerungsdaten",
  "editor.fcCourt": "Gericht *",
  "editor.fcCourtPlaceholder": "Bezirksgericht",
  "editor.fcFileNumber": "Aktenzeichen *",
  "editor.fcFileNumberPlaceholder": "Geschäftszahl",
  "editor.fcAuctionDate": "Versteigerungstermin *",
  "editor.fcMinimumBid": "Mindestgebot (EUR) *",
  "editor.fcEstimatedValue": "Schätzwert (EUR)",
  "editor.fcStatus": "Status",
  "editor.fcStatusScheduled": "Terminiert",
  "editor.fcStatusPending": "Anhängig",
  "editor.fcStatusInProgress": "Laufend",
  "editor.fcStatusCompleted": "Abgeschlossen",
  "editor.fcStatusCancelled": "Aufgehoben",
  "editor.fcViewingDate": "Besichtigung",
  "editor.fcBiddingDeadline": "Gebotsfrist",
  "editor.fcOwnershipShare": "Eigentumsanteil",
  "editor.fcOwnershipSharePlaceholder": "z.B. 1/1",
  "editor.fcEdictUrl": "Edikt-Link",
  "editor.fcRegistrationNumber": "Einlagezahl",
  "editor.fcCadastralMunicipality": "Katastralgemeinde",
  "editor.fcPlotNumber": "Grundstücksnummer",
  "editor.fcTotalArea": "Gesamtfläche (m²)",
  "editor.fcBuildingArea": "Bebaute Fläche (m²)",
  "editor.fcZoning": "Flächenwidmung",
  "editor.fcBuildingCondition": "Gebäudezustand",
  "editor.fcNotes": "Hinweise",

  // Originalinserat (Quelle, angezeigt wie die Quellen-Karte der Detailseite)
  "editor.originalUrlLabel": "Originalinserat",
  "editor.originalUrlPlaceholder": "https://…",
  "editor.originalUrlHint": "Optional: Link zum Inserat beim ursprünglichen Anbieter – erscheint als „Original-Inserat öffnen“.",

  // Anbieter-Sidebar (Vorschau der Profildaten)
  "editor.sellerLabel": "Anbieter",
  "editor.sellerNamePlaceholder": "Ihr Name",
  "editor.sellerEmailPlaceholder": "ihre@email.at",
  "editor.updatedToday": "Aktualisiert: heute",
  "editor.sellerHint": "Anbieterdaten kommen aus Ihrem Profil.",
  "editor.sellerProfileLink": "Profil bearbeiten",
  "editor.loginHint": "Melden Sie sich an – Name und Kontakt kommen aus Ihrem Profil.",

  // Optionaler Ansprechpartner (zweite Kontakt-Karte des Inserats)
  "editor.contactAdd": "Ansprechpartner hinzufügen",
  "editor.contactLegend": "Ansprechpartner",
  "editor.contactRemove": "Entfernen",
  "editor.contactNamePlaceholder": "Name des Ansprechpartners",
  "editor.contactNameLabel": "Name des Ansprechpartners",
  "editor.contactEmailPlaceholder": "E-Mail-Adresse",
  "editor.contactEmailLabel": "E-Mail des Ansprechpartners",
  "editor.contactPhonePlaceholder": "Telefonnummer",
  "editor.contactPhoneLabel": "Telefonnummer des Ansprechpartners",
  "editor.contactPublicHint": "Wird öffentlich im Inserat angezeigt.",

  // Anbietertyp-Labels (wie getApiSellerLabel in live-api.ts)
  "editor.sellerTypePrivate": "Privat",
  "editor.sellerTypeBroker": "Makler",
  "editor.sellerTypeBuilder": "Bauträger",
  "editor.sellerTypeManager": "Verwaltung",

  // Auto-Merkmal-Chips der Live-Vorschau
  "editor.areaLiving": "{value} m² Wohnfläche",
  "editor.areaPlot": "{value} m² Grundstück",

  // Entwurfs-Validierung (validatePropertyDraft, Spiegel der Server-Validierung)
  "editor.valTitleMin": "Titel muss mindestens 10 Zeichen lang sein.",
  "editor.valDescriptionMin": "Beschreibung muss mindestens 50 Zeichen lang sein.",
  "editor.valPriceInvalid": "Bitte geben Sie einen gültigen Preis ein.",
  "editor.valStreetRequired": "Bitte geben Sie eine Straße ein.",
  "editor.valCityRequired": "Bitte wählen Sie einen Ort aus.",
  "editor.valPostalCodeInvalid": "Bitte geben Sie eine gültige vierstellige PLZ ein.",
  "editor.valYearBuiltFuture": "Das Baujahr darf nicht in der Zukunft liegen.",
  "editor.valLivingAreaUnrealistic": "Bitte geben Sie eine realistische Wohnfläche an.",
  "editor.valPlotAreaUnrealistic": "Bitte geben Sie eine realistische Grundstücksfläche an.",
  "editor.valRoomsUnrealistic": "Bitte geben Sie eine realistische Zimmeranzahl an.",
  "editor.valCourtRequired": "Bitte das zuständige Gericht angeben.",
  "editor.valFileNumberRequired": "Bitte das Aktenzeichen angeben.",
  "editor.valAuctionDateInvalid": "Bitte einen gültigen Versteigerungstermin angeben.",
  "editor.valMinimumBidRequired": "Bitte ein Mindestgebot größer als 0 angeben.",
  "editor.valOriginalUrlInvalid": "Der Link zum Originalinserat muss eine vollständige http(s)-Adresse sein.",
  "editor.valContactNameRequired": "Bitte geben Sie den Namen des Ansprechpartners an.",
  "editor.valContactReachRequired":
    "Bitte geben Sie für den Ansprechpartner eine E-Mail-Adresse oder Telefonnummer an.",

  // Bild-Picker (Laufzeit-Meldungen)
  "editor.photoRequired": "Bitte fügen Sie mindestens ein Foto hinzu.",
  "editor.photoRemove": "Foto entfernen",
  "editor.photoLimitReached": "Nur {remaining} weitere Bilder konnten hinzugefügt werden.",
  "editor.fileReadFailed": "Datei konnte nicht gelesen werden.",

  // Location-Select
  "editor.locationSelectPlaceholder": "Ort auswählen",
  "editor.locationSelectHint": "Ort auswählen oder Ort/PLZ manuell eintragen.",
  "editor.locationManualOption": "Ort manuell eintragen",
  "editor.locationLoadFailedDetail": "Orte konnten nicht geladen werden: {message}",
  "editor.locationLoadFailed": "Orte konnten nicht geladen werden.",
  "editor.locationNotFound": "Der Ort konnte nicht eindeutig in Oberösterreich gefunden werden.",

  // Edit-Formular: Ladezustaende
  "editor.statusDraftLoaded": "Lokaler Entwurf geladen.",
  "editor.statusLoading": "Immobilie wird geladen...",
  "editor.statusLoaded": "Immobilie geladen.",
  "editor.statusNotFound": "Immobilie nicht gefunden.",
  "editor.statusLoadFailedDetail": "Immobilie konnte nicht geladen werden: {message}",
  "editor.statusLoadFailed": "Immobilie konnte nicht geladen werden.",

  // Speicher-/Upload-Status
  "editor.statusSavingChanges": "Änderungen werden gespeichert...",
  "editor.statusSavingListing": "Inserat wird gespeichert...",
  "editor.statusSavingDraftLocal": "Entwurf wird lokal gespeichert...",
  "editor.statusUploadingImages": "Bilder werden hochgeladen...",
  "editor.statusUpdateFailedDetail": "Online-Aktualisierung fehlgeschlagen, Entwurf lokal gesichert: {message}",
  "editor.statusUpdateFailed": "Online-Aktualisierung fehlgeschlagen, Entwurf lokal gesichert.",
  "editor.statusSaveFailedDetail": "Online-Speichern fehlgeschlagen, Entwurf lokal gesichert: {message}",
  "editor.statusSaveFailed": "Online-Speichern fehlgeschlagen, Entwurf lokal gesichert.",
} as const;
