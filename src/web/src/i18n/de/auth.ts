/**
 * Auth-Flows: Anmelden, Registrieren, Passwort vergessen/zurücksetzen,
 * E-Mail-Bestätigung sowie geteilte Auth-Bausteine (AuthShell, AuthGate).
 * Key-Präfix: "auth."
 */
export const auth = {
  // Gemeinsame Formularfelder
  "auth.emailLabel": "E-Mail Adresse",
  "auth.emailPlaceholder": "max@beispiel.at",
  "auth.passwordLabel": "Passwort",
  "auth.passwordPlaceholder": "Ihr Passwort",
  "auth.passwordMinHint": "Mindestens 8 Zeichen",
  "auth.firstNameLabel": "Vorname",
  "auth.firstNamePlaceholder": "Max",
  "auth.lastNameLabel": "Nachname",
  "auth.lastNamePlaceholder": "Mustermann",
  "auth.newPasswordLabel": "Neues Passwort",
  "auth.newPasswordConfirmLabel": "Neues Passwort bestätigen",
  "auth.changePassword": "Passwort ändern",
  "auth.login": "Anmelden",
  "auth.registerFree": "Kostenlos registrieren",
  "auth.backToLoginPrefix": "Zurück zur",
  "auth.backToLoginLink": "Anmeldung",

  // AuthShell (geteiltes Layout der Auth-Seiten)
  "auth.brandTagline":
    "Immobilien-Favoriten, Inserate und Suchprofile für Oberösterreich an einem Ort.",

  // Anmelden
  "auth.loginTitle": "Willkommen zurück",
  "auth.loginDescription":
    "Melden Sie sich an, um gespeicherte Immobilien, Favoriten und Inserate zu verwalten.",
  "auth.noAccountPrompt": "Noch kein Konto?",
  "auth.registerLink": "Registrieren",

  // Registrieren
  "auth.createAccount": "Konto erstellen",
  "auth.registerDescription":
    "Registrieren Sie sich für Immobilien-Angebote, Favoriten und Inserate in Oberösterreich.",
  "auth.passwordConfirmLabel": "Passwort bestätigen",
  "auth.passwordConfirmPlaceholder": "Passwort erneut eingeben",
  "auth.wantsToSellTitle": "Ich möchte auch Immobilien anbieten",
  "auth.wantsToSellHint": "Suchen und Favorisieren ist in jedem Konto enthalten",
  "auth.sellerTypeLegend": "Anbietertyp",
  "auth.sellerTypePrivate": "Privatperson",
  "auth.sellerTypePrivateHint": "Eigene Immobilie privat anbieten",
  "auth.sellerTypeBroker": "Makler",
  "auth.sellerTypeBrokerHint": "Inserate im Namen eines Unternehmens verwalten",
  "auth.sellerTypeManager": "Hausverwaltung",
  "auth.sellerTypeManagerHint": "Objekte im Auftrag von Eigentümern verwalten",
  "auth.companyNameLabel": "Firmenname",
  "auth.companyNamePlaceholder": "Immobilienunternehmen",
  "auth.haveAccountPrompt": "Bereits ein Konto?",

  // Passwort vergessen
  "auth.forgotPassword": "Passwort vergessen?",
  "auth.forgotDescription":
    "Geben Sie Ihre E-Mail-Adresse ein. Wenn ein Konto existiert, senden wir Ihnen einen Link zum Zurücksetzen des Passworts.",
  "auth.forgotSubmit": "Link zum Zurücksetzen anfordern",

  // Passwort zurücksetzen
  "auth.resetTitle": "Neues Passwort wählen",
  "auth.resetDescription":
    "Wählen Sie ein neues Passwort für Ihr Heimatplatz-Konto. Nach der Änderung werden alle angemeldeten Geräte abgemeldet.",
  "auth.resetConfirmPlaceholder": "Passwort wiederholen",

  // E-Mail bestätigen
  "auth.verifyTitle": "E-Mail-Adresse bestätigen",
  "auth.verifyDescription": "Wir prüfen Ihren Bestätigungslink.",
  "auth.verifyPending": "Ihre E-Mail-Adresse wird bestätigt...",
  "auth.verifyContinue": "Weiter zu Ihrem Konto",
  "auth.verifyErrorHelpPrefix": "Sie können in Ihrem",
  "auth.verifyErrorHelpLink": "Profil",
  "auth.verifyErrorHelpSuffix": "eine neue Bestätigungs-E-Mail anfordern.",

  // Rollen-Badges (Session-Anzeige in Header/Profil)
  "auth.roleAdmin": "Administrator",
  "auth.roleSellerBroker": "Verkäufer · Makler",
  "auth.roleSellerPropertyManager": "Verkäufer · Hausverwaltung",
  "auth.roleSellerPrivate": "Verkäufer · Privat",
  "auth.roleBuyer": "Käufer",
  "auth.notSignedIn": "Nicht angemeldet",

  // Generischer API-Fehler (apiRequest-Fallback ohne Servermeldung)
  "auth.requestFailedStatus": "Online-Anfrage fehlgeschlagen: {status}",

  // Registrieren/Anmelden: Validierung + Statusmeldungen
  "auth.errorPasswordMismatch": "Die Passwörter stimmen nicht überein.",
  "auth.errorSellerTypeRequired": "Bitte einen Anbietertyp auswählen.",
  "auth.errorCompanyNameRequired": "Bitte den Firmennamen angeben.",
  "auth.statusRegistering": "Konto wird erstellt...",
  "auth.statusLoggingIn": "Anmeldung wird geprüft...",
  "auth.statusLoggedIn": "Erfolgreich angemeldet.",
  "auth.errorLoginFailed": "Die Anmeldung ist fehlgeschlagen.",

  // Passwort ändern (Profil)
  "auth.statusChangingPassword": "Passwort wird geändert...",
  "auth.statusPasswordChanged": "Passwort geändert. Andere Geräte wurden abgemeldet.",
  "auth.errorChangePasswordFailed": "Passwort-Änderung fehlgeschlagen.",

  // Passwort vergessen
  "auth.statusSendingRequest": "Anfrage wird gesendet...",
  "auth.errorForgotFailed": "Die Anfrage ist fehlgeschlagen.",

  // Passwort zurücksetzen
  "auth.errorResetLinkIncomplete": "Der Link ist unvollständig. Bitte fordern Sie über „Passwort vergessen“ einen neuen an.",
  "auth.statusSettingPassword": "Passwort wird gesetzt...",
  "auth.errorResetFailed": "Das Zurücksetzen ist fehlgeschlagen.",

  // E-Mail-Verifikation: Seite /email-bestaetigen
  "auth.errorVerifyLinkIncomplete": "Der Bestätigungslink ist unvollständig (Token fehlt).",
  "auth.verifyAlreadyVerified": "{email} war bereits bestätigt.",
  "auth.verifySuccess": "{email} wurde erfolgreich bestätigt. Vielen Dank!",
  "auth.errorVerifyFailed": "Die Bestätigung ist fehlgeschlagen.",

  // E-Mail-Verifikation: Profil-Banner
  "auth.verifyEmailPendingHint": "{email} ist noch nicht bestätigt. Bitte prüfen Sie Ihr Postfach (auch den Spam-Ordner).",
  "auth.verifyStatusLoadFailed": "Der Status konnte nicht geladen werden.",
  "auth.statusSendingVerification": "Bestätigungs-E-Mail wird gesendet...",
  "auth.verifyAlreadyDone": "Ihre E-Mail-Adresse ist bereits bestätigt.",
  "auth.verifySent": "Bestätigungs-E-Mail wurde gesendet. Bitte prüfen Sie Ihr Postfach.",
  "auth.errorVerifySendFailed": "Der Versand ist fehlgeschlagen.",
} as const;
