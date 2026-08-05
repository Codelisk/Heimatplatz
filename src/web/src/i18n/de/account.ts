/**
 * Account-Bereiche: Profil/Konto-Dashboard, Favoriten, Blockiert,
 * Meine Immobilien und Benachrichtigungs-Einstellungen.
 * Key-Präfixe: "account.", "profile.", "favorites.", "blocked.",
 * "myProperties.", "notifications."
 */
export const account = {
  // Profil (/profil/)
  "profile.title": "Mein Konto",
  "profile.metaDescription": "Profil und Konto für Heimatplatz verwalten.",
  "profile.guestTitle": "Ihr Platz für die Immobiliensuche",
  "profile.guestIntro":
    "Melden Sie sich an, um Favoriten, Benachrichtigungen und Inserate an einem Ort zu verwalten.",
  "profile.guestFavoritesTitle": "Favoriten speichern",
  "profile.guestFavoritesText":
    "Interessante Objekte merken und auf allen Geräten wiederfinden.",
  "profile.guestNotificationsTitle": "Nichts verpassen",
  "profile.guestNotificationsText":
    "Benachrichtigungen für neue passende Immobilien erhalten.",
  "profile.guestSellTitle": "Selbst inserieren",
  "profile.guestSellText":
    "Eigene Immobilien als Privatperson, Makler oder Hausverwaltung anbieten.",
  "profile.notLoggedIn": "Nicht angemeldet",
  "profile.emailVerifiedBadge": "E-Mail bestätigt",
  "profile.logout": "Abmelden",
  "profile.verifyStatusLoading": "Status wird geladen...",
  "profile.resendVerification": "Bestätigungs-E-Mail senden",
  "profile.statFavorites": "Favoriten",
  "profile.statBlocked": "Blockiert",
  "profile.statMine": "Meine Inserate",
  "profile.quickAccessTitle": "Schnellzugriff",
  "profile.quickAccessAria": "Konto-Schnellzugriff",
  "profile.quickInsertLabel": "Immobilie inserieren",
  "profile.quickInsertDescription": "Neues Inserat erstellen",
  "profile.quickNotificationsLabel": "Benachrichtigungen",
  "profile.quickNotificationsDescription": "Hinweise zu neuen Objekten",
  "profile.quickFiltersLabel": "Filtereinstellungen",
  "profile.quickFiltersDescription": "Gespeicherte Suche anpassen",
  "profile.sectionTitle": "Profil",
  "profile.sectionSubtitle": "Name und Verkäufer-Einstellungen",
  "profile.phoneLabel": "Telefonnummer (optional)",
  "profile.phonePlaceholder": "+43 …",
  "profile.phoneHint":
    "Wird als Erreichbarkeit in Ihren neuen und aktualisierten Inseraten öffentlich angezeigt.",
  "profile.wantsToSellTitle": "Ich möchte Immobilien anbieten",
  "profile.wantsToSellHint":
    "Sie können das Anbieten jederzeit aktivieren oder wieder deaktivieren. Bestehende Inserate behalten ihre bisherigen Angaben.",
  "profile.sellerTypeBrokerHint": "Gewerblich vermitteln",
  "profile.sellerTypeManagerHint": "Objekte im Auftrag verwalten",
  "profile.companyNamePlaceholder": "z.B. RE/MAX Premium",
  "profile.saveProfile": "Profil speichern",
  "profile.securityTitle": "Sicherheit",
  "profile.securitySubtitle":
    "Passwort ändern – andere Geräte werden dabei abgemeldet",
  "profile.currentPasswordLabel": "Aktuelles Passwort",
  "profile.deleteAccount": "Konto löschen",
  "profile.deleteAccountText":
    "Ihr Profil, Ihre Inserate, Favoriten, Blockierungen und Benachrichtigungs-Einstellungen werden unwiderruflich entfernt. Diese Aktion kann nicht rückgängig gemacht werden.",
  "profile.deleteConfirmTitle": "Sind Sie sicher?",
  "profile.deleteConfirmText": "Alle Konto-Daten werden endgültig gelöscht.",
  "profile.deleteConfirmButton": "Endgültig löschen",

  // Profil speichern (Status im Formular)
  "profile.statusSaving": "Profil wird gespeichert...",
  "profile.statusSaved": "Profil gespeichert.",
  "profile.statusSaveFailed": "Speichern fehlgeschlagen.",

  // Konto löschen (Status + window.confirm-Fallback)
  "profile.deleteStatusDeleting": "Konto wird gelöscht...",
  "profile.deleteStatusDone": "Ihr Konto wurde dauerhaft gelöscht. Sie werden weitergeleitet...",
  "profile.deleteFailedDetail": "Löschung fehlgeschlagen: {message}",
  "profile.deleteFailed": "Löschung fehlgeschlagen.",
  "profile.deleteConfirmPrompt":
    "Möchten Sie Ihr Konto wirklich endgültig löschen? Ihr Profil, Ihre Inserate, Favoriten, Blockierungen und Benachrichtigungs-Einstellungen werden unwiderruflich entfernt. Diese Aktion kann nicht rückgängig gemacht werden.",

  // Favoriten (/favoriten/)
  "favorites.metaTitle": "Meine Favoriten",
  "favorites.metaDescription": "Favorisierte Immobilien aus Oberösterreich.",
  "favorites.emptyTitle": "Noch keine Favoriten",
  "favorites.emptyText":
    "Markieren Sie Immobilien als Favoriten, um sie hier zu sehen.",
  "favorites.authTitle": "Ihre Favoriten warten nach der Anmeldung",
  "favorites.authText":
    "Melden Sie sich an, um Ihre gespeicherten Immobilien auf diesem und anderen Geräten zu sehen.",

  // Blockiert (/blockiert/)
  "blocked.metaTitle": "Blockierte Immobilien",
  "blocked.metaDescription": "Ausgeblendete Immobilien aus Oberösterreich.",
  "blocked.emptyTitle": "Keine blockierten Immobilien",
  "blocked.emptyText":
    "Blockierte Immobilien werden hier angezeigt und in der Suche ausgeblendet.",
  "blocked.authTitle": "Blockierte Immobilien nach der Anmeldung verwalten",
  "blocked.authText":
    "Melden Sie sich an, um ausgeblendete Immobilien anzusehen oder wieder freizugeben.",

  // Meine Immobilien (/meine-immobilien/)
  "myProperties.metaTitle": "Meine Immobilien",
  "myProperties.metaDescription":
    "Eigene Inserate verwalten, bearbeiten und löschen.",
  "myProperties.emptyTitle": "Noch keine eigenen Inserate",
  "myProperties.emptyText": "Legen Sie Ihr erstes Inserat an – Fotos, Eckdaten und Beschreibung in einem Formular.",
  "myProperties.authTitle": "Eigene Inserate nach der Anmeldung verwalten",
  "myProperties.authText":
    "Melden Sie sich an, um Ihre Immobilien zu erstellen, zu bearbeiten und zu verwalten.",
  "myProperties.createCta": "Neue Immobilie hinzufügen",
  "myProperties.enableSellerCta": "Anbieten im Profil aktivieren",
  "myProperties.statusEdited": "Änderungen gespeichert.",
  "myProperties.statusPublished": "Inserat veröffentlicht.",
  "myProperties.statusDraftSaved": "Entwurf gespeichert.",
  "myProperties.deleteFailed": "Die Immobilie konnte nicht gelöscht werden.",

  // Remote-Listen (Favoriten/Blockiert/Meine Inserate): Fehler-Empty-States
  "myProperties.sellerNotEnabledTitle": "Anbieten ist noch nicht aktiviert",
  "myProperties.sellerNotEnabledText":
    "Aktivieren Sie „Ich möchte Immobilien anbieten“ in Ihrem Profil, um Inserate zu erstellen und hier zu verwalten.",
  "account.listLoadFailedTitle": "Liste konnte nicht geladen werden",
  "account.listLoadFailedText": "Bitte laden Sie die Seite neu oder versuchen Sie es später erneut.",

  // Benachrichtigungen (/benachrichtigungen/)
  "notifications.title": "Benachrichtigungen",
  "notifications.metaDescription":
    "Benachrichtigungen für neue Immobilien in Oberösterreich konfigurieren.",
  "notifications.intro":
    "Suchbenachrichtigungen für neue Objekte in Linz, Wels, Steyr und weiteren Regionen speichern und mit dem Benutzerkonto synchronisieren.",
  "notifications.webPushTitle": "Push in diesem Browser",
  "notifications.webPushChecking": "Status wird geprüft...",
  "notifications.webPushEnable": "Aktivieren",
  "notifications.webPushDisable": "Deaktivieren",
  "notifications.webPushUnsupported":
    "Dieser Browser unterstützt keine Push-Benachrichtigungen. Unter iOS die Seite zuerst zum Home-Bildschirm hinzufügen.",
  "notifications.webPushBlocked":
    "Benachrichtigungen sind für diese Website blockiert. Bitte in den Browser-Einstellungen wieder erlauben.",
  "notifications.webPushActive": "Aktiv - neue passende Immobilien erscheinen als Browser-Benachrichtigung.",
  "notifications.webPushLoginFirst": "Zum Aktivieren bitte zuerst anmelden.",
  "notifications.webPushOffer": "Neue passende Immobilien direkt als Browser-Benachrichtigung erhalten.",
  "notifications.webPushSettingUp": "Push wird eingerichtet...",
  "notifications.webPushNotConfigured": "Web-Push ist am Server derzeit nicht konfiguriert.",
  "notifications.webPushEnableFailedDetail": "Aktivierung fehlgeschlagen: {message}",
  "notifications.webPushEnableFailed": "Aktivierung fehlgeschlagen.",
  "notifications.enableHint":
    "Push- oder E-Mail-Hinweise für neue passende Immobilien",
  "notifications.filterTitle": "Filter für Benachrichtigungen",
  "notifications.filterIntro":
    "Legt fest, für welche neuen Objekte Benachrichtigungen verschickt werden.",
  "notifications.filterModeSameTitle": "Wie Filtereinstellungen",
  "notifications.filterModeSameHint":
    "Nutzt automatisch Ihre gespeicherten Suchfilter.",
  "notifications.filterModeCustomTitle": "Eigene Filter",
  "notifications.filterModeCustomHint":
    "Kriterien nur für Benachrichtigungen festlegen.",
  "notifications.filterModeAllTitle": "Alle neuen Objekte",
  "notifications.filterModeAllHint":
    "Hinweis bei jedem neuen Inserat in Oberösterreich.",
  "notifications.adjustFilters": "Filtereinstellungen anpassen",
  "notifications.propertyTypeTitle": "Immobilientyp",
  "notifications.typeHouse": "Haus",
  "notifications.typeLand": "Grund",
  "notifications.typeForeclosure": "Zwangsversteigerungen",
  "notifications.sellerTypeTitle": "Anbietertyp",
  "notifications.sellerPrivate": "Privat",
  "notifications.sellerBroker": "Makler",
  "notifications.newBuildTitle": "Neubauprojekte",
  "notifications.newBuildHint": "Häuser, die erst geplant oder noch in Bau sind.",
  "notifications.newBuildLabel": "Bei Neubauprojekten benachrichtigen",
  "notifications.locationsTitle": "Orte",
  "notifications.locationsHint":
    "Leer lassen bedeutet: alle Orte in Oberösterreich. Mehrere Orte werden wie in der App gespeichert.",
  "notifications.locationsPlaceholder": "Orte auswählen",
  "notifications.save": "Benachrichtigungen speichern",
} as const;
