# Heimatplatz.Api.Features.Notifications

Push-Benachrichtigungen Backend für standortbasierte Immobilien-Alerts.

## Übersicht

Dieses Feature implementiert das Backend für Push-Benachrichtigungen mit:
- Event-driven Architecture (PropertyCreatedEvent → Push Notification)
- Standortbasierte Filterung (User erhält nur Notifications für ausgewählte Orte)
- Device-Token Management für iOS/Android/Windows
- JWT-geschützte API-Endpoints

## Architektur

### Event-Driven Flow

```
CreatePropertyHandler
    └─> Property speichern
        └─> PropertyCreatedEvent publishen
            └─> PropertyCreatedEventHandler (automatisch)
                └─> PushNotificationService.SendPropertyNotificationAsync()
                    └─> NotificationPreferences laden (Location-Filter)
                        └─> Matching Users finden
                            └─> Push-Notifications senden
```

### Komponenten

#### Entities

**`NotificationPreference`**
```csharp
public class NotificationPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public string Location { get; set; }  // z.B. "Linz", "Wels"
    public bool IsEnabled { get; set; }
    public User User { get; set; }
}
```
- Speichert pro User mehrere Standorte
- Ein User kann mehrere NotificationPreferences haben (eine pro Standort)
- Index auf `UserId` und `Location` für schnelle Abfragen

**`PushSubscription`**
```csharp
public class PushSubscription : BaseEntity
{
    public Guid UserId { get; set; }
    public string DeviceToken { get; set; }
    public string Platform { get; set; }  // "iOS", "Android", "Windows"
    public DateTimeOffset SubscribedAt { get; set; }
    public User User { get; set; }
}
```
- Speichert Device-Tokens für Push-Delivery
- Ein User kann mehrere Devices haben
- Index auf `UserId` für schnelle Abfragen

#### Services

**`IPushNotificationService`** / **`PushNotificationService`**

Hauptservice für Push-Benachrichtigungen:

```csharp
Task SendPropertyNotificationAsync(
    Guid propertyId,
    string title,
    string city,
    decimal price,
    CancellationToken cancellationToken
);
```

Workflow:
1. Findet alle NotificationPreferences mit matching Location
2. Lädt PushSubscriptions für diese Users
3. Erstellt Push-Notification Payload
4. Sendet an alle Device-Tokens (iOS/Android/Windows)

#### Handlers

**`PropertyCreatedEventHandler`**

Reagiert auf `PropertyCreatedEvent`:
```csharp
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class PropertyCreatedEventHandler(
    IPushNotificationService pushNotificationService,
    ILogger<PropertyCreatedEventHandler> logger)
    : IEventHandler<PropertyCreatedEvent>
{
    public async Task Handle(PropertyCreatedEvent @event, ...)
    {
        await pushNotificationService.SendPropertyNotificationAsync(
            @event.PropertyId,
            @event.Title,
            @event.City,
            @event.Price,
            cancellationToken
        );
    }
}
```

**Request Handlers**:
- `GetNotificationPreferencesHandler` - GET Preferences
- `UpdateNotificationPreferencesHandler` - PUT Preferences
- `RegisterDeviceHandler` - POST Device Registration

## API Endpoints

### GET `/api/notifications/preferences`

Lädt Benachrichtigungseinstellungen des eingeloggten Users.

**Headers:**
```
Authorization: Bearer <JWT-Token>
```

**Response:** `200 OK`
```json
{
  "isEnabled": true,
  "locations": ["Linz", "Wels", "Gmunden"]
}
```

**Implementierung:**
- Lädt alle NotificationPreferences des Users
- Gruppiert Locations in Liste
- IsEnabled = true wenn mindestens eine Preference aktiv ist

### PUT `/api/notifications/preferences`

Aktualisiert Benachrichtigungseinstellungen.

**Headers:**
```
Authorization: Bearer <JWT-Token>
```

**Request:**
```json
{
  "isEnabled": true,
  "locations": ["Linz", "Wels"]
}
```

**Response:** `200 OK`
```json
{
  "success": true
}
```

**Implementierung:**
- Löscht alle existierenden NotificationPreferences des Users
- Erstellt neue Preferences für jede Location in der Liste
- Transaktional (all-or-nothing)

### POST `/api/notifications/device`

Registriert Device-Token für Push-Notifications.

**Headers:**
```
Authorization: Bearer <JWT-Token>
```

**Request:**
```json
{
  "deviceToken": "fX3k9mP2qY...",
  "platform": "iOS"
}
```

**Response:** `200 OK`
```json
{
  "success": true
}
```

**Implementierung:**
- Prüft ob Token bereits existiert (Update statt Insert)
- Speichert Platform-Info für spätere Push-Delivery
- Ein User kann mehrere Devices haben

## Events

### PropertyCreatedEvent

Published von `CreatePropertyHandler` nach erfolgreichem Property-Insert:

```csharp
public record PropertyCreatedEvent(
    Guid PropertyId,
    string Title,
    string City,
    decimal Price
) : IEvent;
```

**Publishing:**
```csharp
var propertyCreatedEvent = new PropertyCreatedEvent(
    property.Id,
    property.Title,
    property.City,
    property.Price
);
await mediator.Publish(propertyCreatedEvent, cancellationToken);
```

## Database Schema

### NotificationPreferences Table

```sql
CREATE TABLE NotificationPreferences (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Location TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE INDEX IX_NotificationPreferences_UserId ON NotificationPreferences(UserId);
CREATE INDEX IX_NotificationPreferences_Location ON NotificationPreferences(Location);
```

### PushSubscriptions Table

```sql
CREATE TABLE PushSubscriptions (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    DeviceToken TEXT NOT NULL,
    Platform TEXT NOT NULL,
    SubscribedAt TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE INDEX IX_PushSubscriptions_UserId ON PushSubscriptions(UserId);
CREATE UNIQUE INDEX IX_PushSubscriptions_DeviceToken ON PushSubscriptions(DeviceToken);
```

## Seeding

**`NotificationsSeeder`** erstellt Test-Daten:

```csharp
public class NotificationsSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 30; // Nach UserSeeder (20)

    public async Task SeedAsync(CancellationToken ct)
    {
        // Erstellt NotificationPreferences für 5 Test-User
        // Locations: Linz, Wels, Gmunden, Bad Ischl, Steyr, etc.
    }
}
```

**Registrierung:**
```csharp
services.AddSeeder<NotificationsSeeder>();
```

## Dependencies

- `Shiny.Push` (4.0.0-beta-0095) - Push-Notification Library
- `Shiny.Mediator` - Event-Handling
- `Microsoft.EntityFrameworkCore` - Database
- `Heimatplatz.Api.Core.Data` - DbContext, BaseEntity
- `Heimatplatz.Api.Features.Auth` - User Entity, JWT

## Configuration

### Startup Registration

In `Heimatplatz.Api.Core.Startup/ServiceCollectionExtensions.cs`:

```csharp
using Heimatplatz.Api.Features.Notifications.Configuration;

public static IServiceCollection AddApiServices(...)
{
    services.AddShinyServiceRegistry();  // [Service] Attribute
    services.AddShinyMediator();
    services.AddAppData(configuration);
    services.AddNotificationsFeature();  // ← Notifications Feature
    return services;
}
```

### Mediator Endpoints

```csharp
using Heimatplatz.Api.Features.Notifications;

app.MapShinyMediatorEndpoints();
Heimatplatz.Api.Features.Notifications.MediatorEndpoints
    .MapGeneratedMediatorEndpoints(app);
```

## Testing

### Manuelles Testing

1. **Device registrieren:**
```bash
curl -X POST https://localhost:7133/api/notifications/device \
  -H "Authorization: Bearer <JWT>" \
  -H "Content-Type: application/json" \
  -d '{"deviceToken":"test-token-123","platform":"iOS"}'
```

2. **Preferences setzen:**
```bash
curl -X PUT https://localhost:7133/api/notifications/preferences \
  -H "Authorization: Bearer <JWT>" \
  -H "Content-Type: application/json" \
  -d '{"isEnabled":true,"locations":["Linz","Wels"]}'
```

3. **Neue Immobilie erstellen:**
```bash
curl -X POST https://localhost:7133/api/properties \
  -H "Authorization: Bearer <JWT>" \
  -H "Content-Type: application/json" \
  -d '{"title":"Einfamilienhaus","city":"Linz",...}'
```

4. **Logs prüfen:**
```
[INF] PropertyCreatedEvent published for property {PropertyId}
[INF] Sending push notifications for new property in Linz
[INF] Found 3 users with matching location preferences
[INF] Sending push notification to device token: test-token-123
```

## Troubleshooting

### Notifications werden nicht gesendet

**Prüfen:**
- PropertyCreatedEvent wird published? (Log: "PropertyCreatedEvent published")
- PropertyCreatedEventHandler wird aufgerufen? (Log: "Sending push notifications")
- NotificationPreferences existieren für die Stadt?
  ```sql
  SELECT * FROM NotificationPreferences WHERE Location = 'Linz' AND IsEnabled = 1;
  ```
- PushSubscriptions existieren?
  ```sql
  SELECT * FROM PushSubscriptions WHERE UserId IN (...);
  ```

### Duplicate DeviceToken Error

Device-Token bereits registriert → kein Problem, wird geupdated.

### User erhält keine Notifications

**Prüfen:**
1. NotificationPreferences für diese Stadt? `GET /api/notifications/preferences`
2. IsEnabled = true?
3. DeviceToken registriert? Prüfe PushSubscriptions Tabelle
4. PropertyCreatedEvent hat richtige City?

## Bekannte Limitierungen

- Keine Rich Notifications (Bilder) aktuell
- Keine Notification-Statistiken (Delivered/Opened)
- Keine Retry-Logic bei fehlgeschlagenen Sends
- Keine Rate-Limiting für Push-Sends

## Weitere Entwicklung

### TODO
- [ ] Rich Notifications mit Property-Bild
- [ ] Notification-Templates
- [ ] Delivery-Status Tracking
- [ ] Retry-Queue für fehlgeschlagene Sends
- [ ] Rate-Limiting / Throttling
- [ ] Admin-Dashboard für Notification-Statistiken
- [ ] A/B Testing für Notification-Content
