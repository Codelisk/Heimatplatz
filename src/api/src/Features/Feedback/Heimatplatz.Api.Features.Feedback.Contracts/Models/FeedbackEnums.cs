namespace Heimatplatz.Api.Features.Feedback.Contracts.Models;

/// <summary>
/// Kategorie einer Feedback-Anfrage. Serialisiert per globalem JsonStringEnumConverter
/// als Text ("Idea" statt 0) - Web vergleicht Strings.
/// </summary>
public enum FeedbackCategory
{
    /// <summary>Wunsch oder Idee fuer die App</summary>
    Idea = 0,

    /// <summary>Problem oder Fehler</summary>
    Problem = 1,

    /// <summary>Frage zur Nutzung</summary>
    Question = 2,

    /// <summary>Lob</summary>
    Praise = 3,

    /// <summary>Sonstiges Anliegen</summary>
    Other = 4
}

/// <summary>
/// Bearbeitungsstatus einer Feedback-Anfrage.
/// Automatische Uebergaenge: Team-Antwort setzt -> Answered, neue Nutzer-Nachricht
/// auf Answered/Closed setzt -> Open (wieder in der Eingangsliste sichtbar).
/// </summary>
public enum FeedbackTicketStatus
{
    Open = 0,
    InProgress = 1,
    Answered = 2,
    Closed = 3
}

/// <summary>Absender einer Nachricht im Feedback-Verlauf.</summary>
public enum FeedbackAuthor
{
    User = 0,
    Team = 1
}

/// <summary>Art eines Anhangs an einer Feedback-Nachricht.</summary>
public enum FeedbackAttachmentKind
{
    Image = 0,

    /// <summary>Sprachnachricht (nur MAUI-App)</summary>
    Audio = 1
}

/// <summary>Plattform, von der die Anfrage abgeschickt wurde (Diagnose-Metadaten).</summary>
public enum FeedbackSource
{
    Unknown = 0,
    Web = 1,
    Android = 2,
    Ios = 3,
    Windows = 4
}
