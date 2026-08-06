/**
 * Texte der KI-Übersicht ("Meine Übersicht", /meine-uebersicht/).
 * Enthält auch den Navigations- und Profil-Schnellzugriff des Features.
 */
export const dashboard = {
  "nav.myOverview": "Meine Übersicht",

  "profile.quickDashboardLabel": "Meine Übersicht",
  "profile.quickDashboardDescription": "Ihre persönliche Übersicht nach Wunsch",

  "dash.title": "Meine Übersicht",
  "dash.metaDescription":
    "Ihre persönliche Immobilien-Übersicht: Beschreiben Sie, wonach Sie suchen — Heimatplatz stellt die passende Ansicht für Sie zusammen.",
  "dash.authTitle": "Ihre persönliche Übersicht",
  "dash.authText":
    "Melden Sie sich an, um sich eine Übersicht nach Ihren Wünschen zusammenstellen zu lassen.",

  "dash.createTitle": "Was möchten Sie sehen?",
  "dash.createText":
    "Beschreiben Sie in eigenen Worten, wonach Sie suchen und wie es angezeigt werden soll — daraus wird Ihre persönliche Übersicht zusammengestellt.",
  "dash.createPlaceholder":
    "z. B.: Ich suche ein Haus im Bezirk Vöcklabruck bis 400.000 €. Zeig mir zuerst die neuesten Angebote, dazu eine Karte und wie viele neue Inserate dazukommen.",
  "dash.createSubmit": "Übersicht erstellen",
  "dash.createHintMin": "Bitte beschreiben Sie kurz, wonach Sie suchen.",
  "dash.examplesLabel": "Beispiele:",
  "dash.example1": "Häuser bis 300.000 € mit Karte",
  "dash.example2": "Nur Grundstücke, günstigste zuerst",
  "dash.example3": "Was ist neu diese Woche?",

  "dash.listLabel": "Ihre Übersichten",
  "dash.newButton": "Neue Übersicht",

  "dash.progressQueued": "Ihre Übersicht wird vorbereitet …",
  "dash.progressInProgress": "Ihre Übersicht wird zusammengestellt — das kann bis zu einer Minute dauern.",
  "dash.progressRefine": "Ihre Übersicht wird überarbeitet …",
  "dash.progressTimeout": "Das dauert gerade länger als üblich. Sie können die Seite später erneut öffnen.",

  "dash.failedTitle": "Das hat leider nicht geklappt",
  "dash.retryHint": "Formulieren Sie Ihren Wunsch unten neu — die Übersicht wird dann neu aufgebaut.",

  "dash.refinePlaceholder": "Was soll anders sein? z. B. „Nur Privatanbieter, Karte größer“",
  "dash.refineSubmit": "Anpassen",
  "dash.revert": "Letzte Änderung rückgängig",
  "dash.rename": "Umbenennen",
  "dash.renameSave": "Speichern",
  "dash.delete": "Löschen",
  "dash.deleteConfirm": "Wirklich löschen?",

  "dash.unsupportedTitle": "Das kann Ihre Übersicht noch nicht:",
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
