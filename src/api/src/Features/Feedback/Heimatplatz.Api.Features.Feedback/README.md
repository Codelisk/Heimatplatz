# Heimatplatz.Api.Features.Feedback

Feedback-Feature: Nutzer melden Wuensche, Probleme, Fragen oder Lob zur App/Plattform -
mit Bild-Anhaengen (Web + App) und Sprachnachrichten (App). Das Team sieht und beantwortet
die Anfragen im Intern-Bereich (`/intern/feedback`); der Nutzer bekommt die Antwort per
Push-Benachrichtigung (Deep-Link `heimatplatz://feedback/{ticketId}`) und sieht den
Verlauf in App und Web.

## Datenmodell

- `FeedbackTicket` - Anfrage: `UserId`, `Category` (Idea/Problem/Question/Praise/Other),
  `Subject` (serverseitig abgeleitet, wenn leer), `Status` (Open/InProgress/Answered/Closed),
  `Source`/`AppVersion` (Diagnose-Kontext), `LastMessageAt` (Sortierung),
  `HasUnreadForTeam`/`HasUnreadForUser` (Badges; werden beim Abrufen des jeweiligen
  Detail-Endpoints zurueckgesetzt)
- `FeedbackMessage` - Nachricht im Verlauf (`Author` User/Team, Body darf bei reinen
  Anhang-Nachrichten leer sein), Cascade-Delete am Ticket
- `FeedbackAttachment` - Anhang (`Kind` Image/Audio, relative `Url`, `ContentType`,
  `FileSizeBytes`, `DurationSeconds` bei Audio), Cascade-Delete an der Nachricht

## Status-Automatik

- Team-Antwort -> `Answered` + `HasUnreadForUser`
- Neue Nutzer-Nachricht auf `Answered`/`Closed` -> zurueck auf `Open` + `HasUnreadForTeam`
- `InProgress`/`Closed` setzt das Team manuell (`POST /api/admin/feedback/status`)

## Anhaenge

Upload einzeln als Base64-JSON (`POST /api/feedback/attachments`, JWT), danach Referenz
per URL in Create/AddMessage. Bilder folgen der Inserats-Pipeline (Original 1:1 +
`{guid}.display.jpg` via `ImageDisplayVariant`, max. 60 MB, JPEG/PNG/WebP); Audio wird
unveraendert gespeichert (max. 25 MB, WAV/M4A/AAC/MP3). Ablage unter
`wwwroot/uploads/feedback` (GUID-Dateinamen). DTOs liefern absolute URLs plus
`ThumbnailUrl` ueber `/api/images/local?w=640` fuer Bilder.

Nicht referenzierte Uploads (Nutzer bricht ab) bleiben als Waisen liegen - bewusst
akzeptiert; bei Bedarf spaeter ein Aufraeum-Job. Referenzen werden gegen
`/uploads/feedback/` + Datei-Existenz validiert (Path-Traversal-Guard im Service).

## Push bei Team-Antwort

`ReplyToFeedbackTicketHandler` publiziert `FeedbackTeamRepliedEvent`
(Notifications.Contracts); der Versand haengt entkoppelt im Notifications-Feature
(`FeedbackTeamRepliedEventHandler` -> `IPushNotificationService.SendFeedbackReplyNotificationAsync`).
Die Zustellung erfolgt an ALLE Geraete des Nutzers und ignoriert bewusst die
Immobilien-Benachrichtigungs-Einstellungen (transaktionale Antwort auf eigene Anfrage).

## Endpoints

Siehe README des Contracts-Projekts. Nutzer-Endpoints unter `/api/feedback` (JWT,
Ownership-Check auf `UserId`), Admin-Endpoints unter `/api/admin/feedback`
(`IAdminAccessGuard`, X-Admin-Key, fail-closed).

## Konto-Loeschung

`FeedbackUserDataEraser` (Order 15) loescht Anfragen, Verlauf und Anhang-Dateien des
Benutzers im Rahmen der zentralen Account-Loeschung.

## Abhaengigkeiten

`Feedback.Contracts`, `Shared` (ApiService, ImageDisplayVariant, IUserDataEraser),
`Core.Data`, `Core.Data.Seeding`, `Features.Admin` (AdminAccessGuard),
`Features.Auth` (User-Entity fuer Intern-Liste), `Features.Notifications.Contracts` (Event).
