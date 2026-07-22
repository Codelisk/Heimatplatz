/**
 * Feedback-Bereich (/feedback): Anfrage erstellen, "Meine Anfragen", Verlauf mit
 * Team-Antworten. Key-Präfix: "feedback."
 * Kategorie-/Status-Labels kommen aus feedback-labels.ts (Enum-Name -> t()-Key-Switch,
 * gleiche Konvention wie marketing-labels.ts).
 */
export const feedback = {
  // Seite /feedback
  "feedback.metaTitle": "Feedback & Wünsche",
  "feedback.metaDescription":
    "Sag uns, was dir an Heimatplatz fehlt, was dich stört oder was dir gefällt.",
  "feedback.title": "Feedback & Wünsche",
  "feedback.intro":
    "Was fehlt dir, was stört dich, was gefällt dir? Wir lesen jede Nachricht und antworten dir direkt hier - in der App bekommst du zusätzlich eine Benachrichtigung.",
  "feedback.authTitle": "Feedback nur mit Konto",
  "feedback.authDescription":
    "Melde dich an, um uns Feedback zu schicken und unsere Antworten zu erhalten.",

  // Formular
  "feedback.categoryLegend": "Worum geht es?",
  "feedback.subjectLabel": "Betreff (optional)",
  "feedback.subjectPlaceholder": "Kurz zusammengefasst …",
  "feedback.bodyLabel": "Deine Nachricht",
  "feedback.bodyPlaceholder":
    "Beschreib dein Anliegen - je konkreter, desto besser können wir helfen.",
  "feedback.photosLabel": "Bilder (optional)",
  "feedback.photosHint": "Screenshots oder Fotos helfen uns, dein Anliegen zu verstehen.",
  "feedback.photosAdd": "Bilder auswählen",
  "feedback.photoRemove": "Bild entfernen",
  "feedback.photosMax": "Maximal {count} Bilder pro Nachricht.",
  "feedback.submit": "Feedback absenden",
  "feedback.submitting": "Wird gesendet …",
  "feedback.uploadingPhotos": "Bild {current} von {total} wird hochgeladen …",
  "feedback.submitSuccess":
    "Danke für dein Feedback! Du findest die Anfrage unten unter \"Meine Anfragen\".",
  "feedback.errorEmpty": "Bitte gib eine Nachricht ein oder häng ein Bild an.",
  "feedback.errorGeneric": "Senden fehlgeschlagen: {error}",

  // Kategorien (Auswahlkarten)
  "feedback.categoryIdea": "Wunsch / Idee",
  "feedback.categoryIdeaHint": "Etwas fehlt dir oder würde Heimatplatz besser machen",
  "feedback.categoryProblem": "Problem melden",
  "feedback.categoryProblemHint": "Etwas funktioniert nicht so, wie es soll",
  "feedback.categoryQuestion": "Frage",
  "feedback.categoryQuestionHint": "Du kommst an einer Stelle nicht weiter",
  "feedback.categoryPraise": "Lob",
  "feedback.categoryPraiseHint": "Dir gefällt etwas besonders gut",
  "feedback.categoryOther": "Sonstiges",
  "feedback.categoryOtherHint": "Alles, was sonst nirgends reinpasst",

  // Status (Nutzer-Sicht)
  "feedback.statusOpen": "Offen",
  "feedback.statusInProgress": "In Arbeit",
  "feedback.statusAnswered": "Beantwortet",
  "feedback.statusClosed": "Abgeschlossen",

  // Meine Anfragen
  "feedback.listTitle": "Meine Anfragen",
  "feedback.listEmpty": "Noch keine Anfragen - dein erstes Feedback landet hier.",
  "feedback.listLoadFailed": "Anfragen konnten nicht geladen werden: {error}",
  "feedback.listUnread": "Neue Antwort",
  "feedback.listMessageCount": "{count} Nachrichten",

  // Verlauf (/feedback/anfrage)
  "feedback.threadMetaTitle": "Anfrage",
  "feedback.threadBack": "Zurück zu meinen Anfragen",
  "feedback.threadNotFound":
    "Diese Anfrage wurde nicht gefunden. Sie gehört möglicherweise zu einem anderen Konto.",
  "feedback.threadLoadFailed": "Verlauf konnte nicht geladen werden: {error}",
  "feedback.threadYou": "Du",
  "feedback.threadTeam": "Heimatplatz-Team",
  "feedback.threadVoiceMessage": "Sprachnachricht",
  "feedback.threadImageAlt": "Anhang {index}",
  "feedback.replyLabel": "Antwort schreiben",
  "feedback.replyPlaceholder": "Deine Antwort …",
  "feedback.replySend": "Senden",
  "feedback.replySuccess": "Antwort gesendet.",

  // Messenger-Eingabezeile (Bilder + Sprachnachricht haengen an der Zeile)
  "feedback.messagePlaceholder": "Nachricht schreiben …",
  "feedback.attachImages": "Bilder anhängen",
  "feedback.attachCamera": "Foto aufnehmen",
  "feedback.recordStart": "Sprachnachricht aufnehmen",
  "feedback.recordStop": "Aufnahme übernehmen",
  "feedback.recordCancel": "Aufnahme verwerfen",
  "feedback.send": "Senden",
  "feedback.voiceAttached": "Sprachnachricht",
  "feedback.voicePlay": "Sprachnachricht anhören",
  "feedback.attachmentRemove": "Anhang entfernen",
  "feedback.micDenied": "Kein Mikrofon-Zugriff - bitte im Browser erlauben.",
  "feedback.recordFailed": "Aufnahme fehlgeschlagen.",
  "feedback.composeHint":
    "Text schreiben, Bilder anhängen oder eine Sprachnachricht aufnehmen - alles direkt in der Zeile.",
} as const;
