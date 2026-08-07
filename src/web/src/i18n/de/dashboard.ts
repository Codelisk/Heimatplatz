/**
 * Texte der KI-Übersicht ("Meine Übersicht", /meine-uebersicht/).
 * Enthält auch den Navigations- und Profil-Schnellzugriff des Features.
 */
export const dashboard = {
  "nav.myOverview": "Meine Ansichten",

  "profile.quickDashboardLabel": "Meine Ansichten",
  "profile.quickDashboardDescription": "Ihre persönlichen Ansichten nach Wunsch",

  "dash.title": "Meine Ansichten",
  "dash.metaDescription":
    "Erstellen Sie Ihre persönliche Ansicht: Dashboard oder Immobilienliste — beschrieben in eigenen Worten, zusammengestellt aus allen Daten.",
  "dash.authTitle": "Ihre persönlichen Ansichten",
  "dash.authText":
    "Melden Sie sich an, um sich Dashboards und Immobilienlisten nach Ihren Wünschen zusammenstellen zu lassen.",

  "dash.createTitle": "Erstellen Sie Ihre persönliche Ansicht",
  "dash.createText":
    "Wählen Sie die Art der Ansicht und beschreiben Sie in eigenen Worten, wonach Sie suchen und welche Werte Sie sehen möchten.",
  "dash.typeDashboard": "Dashboard",
  "dash.typeDashboardHint": "Kennzahlen, Listen, Karte und Diagramme frei kombiniert",
  "dash.typeList": "Immobilienliste",
  "dash.typeListHint": "Eine große Trefferliste mit genau Ihren Werten",
  "dash.createPlaceholder":
    "z. B.: Ich suche ein Haus im Bezirk Vöcklabruck bis 400.000 €. Zeig mir zuerst die neuesten Angebote, dazu eine Karte und wie viele neue Inserate dazukommen.",
  "dash.createPlaceholderList":
    "z. B.: Alle Grundstücke unter 200.000 €, ohne Fotos, mit Titel, Preis und Grundfläche — günstigste zuerst.",
  "dash.createSubmit": "Ansicht erstellen",
  "dash.createHintMin": "Bitte beschreiben Sie kurz, wonach Sie suchen.",
  "dash.examplesLabel": "Beispiele:",
  "dash.example1": "Häuser bis 300.000 € mit Karte",
  "dash.example2": "Nur Grundstücke, günstigste zuerst",
  "dash.example3": "Was ist neu diese Woche?",

  "dash.listLabel": "Ihre Ansichten",
  "dash.newButton": "Neue Ansicht",
  "dash.badgeList": "Liste",
  "dash.badgeDashboard": "Dashboard",

  "dash.progressQueued": "Ihre Ansicht wird vorbereitet …",
  "dash.progressInProgress": "Ihre Ansicht wird zusammengestellt — das kann bis zu einer Minute dauern.",
  "dash.progressRefine": "Ihre Ansicht wird überarbeitet …",
  "dash.progressTimeout": "Das dauert gerade länger als üblich. Sie können die Seite später erneut öffnen.",

  "dash.failedTitle": "Das hat leider nicht geklappt",
  "dash.retryHint": "Formulieren Sie Ihren Wunsch unten neu — die Ansicht wird dann neu aufgebaut.",

  "dash.refinePlaceholder": "Was soll anders sein? z. B. „Nur Privatanbieter, Karte größer“",
  "dash.refineSubmit": "Anpassen",
  "dash.revert": "Letzte Änderung rückgängig",
  "dash.rename": "Umbenennen",
  "dash.renameSave": "Speichern",
  "dash.delete": "Löschen",
  "dash.deleteConfirm": "Wirklich löschen?",

  "dash.unsupportedTitle": "Das kann Ihre Ansicht noch nicht:",
  "dash.widgetError": "Dieser Bereich konnte gerade nicht geladen werden.",
  "dash.widgetEmpty": "Aktuell keine passenden Inserate.",
  "dash.mapTotal": "{count} Treffer auf der Karte",
  "dash.mapNone": "Keine Treffer mit Kartenposition.",
  "dash.listTotal": "{count} Treffer insgesamt",
  "dash.showAll": "Alle ansehen",
  "dash.sessionExpired": "Ihre Anmeldung ist abgelaufen — bitte melden Sie sich neu an.",
  "dash.genericError": "Das hat gerade nicht funktioniert. Bitte versuchen Sie es erneut.",

  "dash.detailAria": "Inserats-Detailansicht",
  "dash.detailClose": "Schließen",
  "dash.detailFullLink": "Zur vollständigen Anzeige",
  "dash.detailLoadError": "Das Inserat konnte gerade nicht geladen werden.",
  "dash.detailDescriptionTitle": "Beschreibung",
  "dash.detailFeaturesTitle": "Ausstattung",
  "dash.detailContactTitle": "Kontakt",
} as const;
