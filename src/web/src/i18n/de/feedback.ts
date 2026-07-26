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
    "Sagen Sie uns, was Ihnen an Heimatplatz fehlt, was Sie stört oder was Ihnen gefällt.",
  "feedback.title": "Feedback & Wünsche",
  "feedback.intro":
    "Was fehlt Ihnen, was stört Sie, was gefällt Ihnen? Wir lesen jede Nachricht und antworten Ihnen direkt hier - in der App bekommen Sie zusätzlich eine Benachrichtigung.",
  "feedback.authTitle": "Feedback nur mit Konto",
  "feedback.authDescription":
    "Melden Sie sich an, um uns Feedback zu schicken und unsere Antworten zu erhalten.",

  // Formular
  "feedback.categoryLegend": "Worum geht es?",
  "feedback.bodyLabel": "Ihre Nachricht",
  "feedback.bodyPlaceholder":
    "Beschreiben Sie Ihr Anliegen - je konkreter, desto besser können wir helfen.",
  "feedback.photosLabel": "Bilder (optional)",
  "feedback.photosHint": "Screenshots oder Fotos helfen uns, Ihr Anliegen zu verstehen.",
  "feedback.photosAdd": "Bilder auswählen",
  "feedback.photoRemove": "Bild entfernen",
  "feedback.photosMax": "Maximal {count} Bilder pro Nachricht.",
  "feedback.submit": "Feedback absenden",
  "feedback.submitting": "Wird gesendet …",
  "feedback.uploadingPhotos": "Bild {current} von {total} wird hochgeladen …",
  "feedback.submitSuccess":
    "Danke für Ihr Feedback! Sie finden die Anfrage unten unter \"Meine Anfragen\".",
  "feedback.errorEmpty": "Bitte geben Sie eine Nachricht ein oder hängen Sie ein Bild an.",
  "feedback.errorGeneric": "Senden fehlgeschlagen: {error}",

  // Kategorien (Auswahlkarten)
  "feedback.categoryIdea": "Wunsch / Idee",
  "feedback.categoryIdeaHint": "Etwas fehlt Ihnen oder würde Heimatplatz besser machen",
  "feedback.categoryProblem": "Problem melden",
  "feedback.categoryProblemHint": "Etwas funktioniert nicht so, wie es soll",
  "feedback.categoryQuestion": "Frage",
  "feedback.categoryQuestionHint": "Sie kommen an einer Stelle nicht weiter",
  "feedback.categoryPraise": "Lob",
  "feedback.categoryPraiseHint": "Ihnen gefällt etwas besonders gut",
  "feedback.categoryOther": "Sonstiges",
  "feedback.categoryOtherHint": "Alles, was sonst nirgends reinpasst",

  // Status (Nutzer-Sicht)
  "feedback.statusOpen": "Offen",
  "feedback.statusInProgress": "In Arbeit",
  "feedback.statusAnswered": "Beantwortet",
  "feedback.statusClosed": "Abgeschlossen",

  // Meine Anfragen
  "feedback.listTitle": "Meine Anfragen",
  "feedback.listEmpty": "Noch keine Anfragen - Ihr erstes Feedback landet hier.",
  "feedback.listLoadFailed": "Anfragen konnten nicht geladen werden: {error}",
  "feedback.listUnread": "Neue Antwort",
  "feedback.listMessageCount": "{count} Nachrichten",
  "feedback.renameAction": "Umbenennen",
  "feedback.renamePrompt": "Neuer Titel für diese Anfrage",
  "feedback.renameFailed": "Umbenennen fehlgeschlagen: {error}",

  // Verlauf (/feedback/anfrage)
  "feedback.threadMetaTitle": "Anfrage",
  "feedback.threadBack": "Zurück zu meinen Anfragen",
  "feedback.threadNotFound":
    "Diese Anfrage wurde nicht gefunden. Sie gehört möglicherweise zu einem anderen Konto.",
  "feedback.threadLoadFailed": "Verlauf konnte nicht geladen werden: {error}",
  "feedback.threadYou": "Sie",
  "feedback.threadTeam": "Heimatplatz-Team",
  "feedback.threadVoiceMessage": "Sprachnachricht",
  "feedback.threadImageAlt": "Anhang {index}",
  "feedback.replyLabel": "Antwort schreiben",
  "feedback.replyPlaceholder": "Ihre Antwort …",
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
