# Heimatplatz.Api.Features.Feedback.Contracts

Request-/Response-Records und DTOs des Feedback-Features (Nutzer-Anfragen an das
Heimatplatz-Team: Wuensche, Probleme, Fragen, Lob - mit Bild-/Audio-Anhaengen und
Antwort-Verlauf).

## Inhalt

- `Models/FeedbackEnums.cs` - `FeedbackCategory`, `FeedbackTicketStatus`, `FeedbackAuthor`,
  `FeedbackAttachmentKind`, `FeedbackSource` (alle als String serialisiert)
- `Models/FeedbackDtos.cs` - Ticket-/Nachrichten-/Anhang-DTOs fuer Nutzer- und Admin-Sicht
- `Mediator/Requests/` - je ein Record pro Endpoint:

| Request | Endpoint | Auth |
|---------|----------|------|
| `UploadFeedbackAttachmentRequest` | `POST /api/feedback/attachments` | JWT |
| `CreateFeedbackTicketRequest` | `POST /api/feedback` | JWT |
| `GetMyFeedbackTicketsRequest` | `GET /api/feedback` | JWT |
| `GetFeedbackTicketRequest` | `GET /api/feedback/{TicketId}` | JWT |
| `AddFeedbackMessageRequest` | `POST /api/feedback/messages` | JWT |
| `GetAdminFeedbackTicketsRequest` | `GET /api/admin/feedback` | X-Admin-Key |
| `GetAdminFeedbackTicketDetailRequest` | `GET /api/admin/feedback/{Id}` | X-Admin-Key |
| `ReplyToFeedbackTicketRequest` | `POST /api/admin/feedback/reply` | X-Admin-Key |
| `SetFeedbackTicketStatusRequest` | `POST /api/admin/feedback/status` | X-Admin-Key |
| `GetAdminFeedbackStatsRequest` | `GET /api/admin/feedback/stats` | X-Admin-Key |

## Upload-Flow

Anhaenge werden VOR dem Erstellen der Nachricht einzeln als Base64 hochgeladen
(`UploadFeedbackAttachmentRequest`, ein Anhang pro Request). Die zurueckgegebene URL
wird dann als `FeedbackAttachmentInput` in Create/AddMessage referenziert; Art,
Content-Type und Groesse leitet der Server aus der gespeicherten Datei ab.

## Abhaengigkeiten

Nur `Shiny.Mediator.Contracts` - keine Projekt-Referenzen.
