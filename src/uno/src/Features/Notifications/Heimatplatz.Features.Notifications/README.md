# Heimatplatz.Features.Notifications

Push-Benachrichtigungen für neue Immobilien mit Standortfilterung.

## Übersicht

Dieses Feature ermöglicht es Benutzern:
- Push-Benachrichtigungen für neue Immobilien zu erhalten
- Standortfilter zu konfigurieren (nur Benachrichtigungen für ausgewählte Orte)
- Benachrichtigungseinstellungen zu verwalten
- Device-Tokens automatisch zu registrieren

## Architektur

### Push-Provider

**Native Provider** (Standard)
- iOS: Apple Push Notification Service (APNs)
- Android: Firebase Cloud Messaging (FCM)
- Windows: Windows Push Notification Services (WNS)
- macOS: Apple Push Notification Service (APNs)

Der Native Provider nutzt die plattformspezifischen Push-Services ohne zusätzliche externe Abhängigkeiten.

### Komponenten

#### Services

**`INotificationService`** / **`NotificationService`**
- API-Client für Benachrichtigungseinstellungen
- `GetPreferencesAsync()` - Lädt Benutzereinstellungen
- `UpdatePreferencesAsync()` - Aktualisiert Standortfilter und aktiviert/deaktiviert Push
- `RegisterDeviceAsync()` - Registriert Device-Token beim Backend

**`IPushNotificationInitializer`** / **`PushNotificationInitializer`**
- Initialisiert Shiny.Push beim App-Start
- `InitializeAsync()` - Fordert Push-Permissions an
- `GetStatusAsync()` - Prüft aktuellen Permission-Status

**`PushNotificationDelegate`** (implementiert `IPushDelegate`)
- Handler für Shiny.Push Events:
  - `OnEntry()` - User tippt auf Notification (Navigation zu Immobilie)
  - `OnReceived()` - Notification im Vordergrund empfangen
  - `OnNewToken()` - Neuer Device-Token → automatische Registrierung beim API
  - `OnUnRegistered()` - Push deregistriert

#### Presentation

**`NotificationSettingsPage`** / **`NotificationSettingsViewModel`**
- UI für Benachrichtigungseinstellungen
- MVUX-Pattern mit `IState<NotificationPreferenceDto>`
- Features:
  - Toggle für Benachrichtigungen aktivieren/deaktivieren
  - Standorte hinzufügen/entfernen
  - Location Chips mit Remove-Button
  - Empty State für keine Standorte

### Models

**`NotificationPreferenceDto`**
```csharp
public record NotificationPreferenceDto(
    bool IsEnabled,
    List<string> Locations
);
```

## Verwendung

### Benachrichtigungseinstellungen öffnen

Navigation über das Profil-Menü auf der HomePage:
```xaml
<MenuFlyoutItem Text="Benachrichtigungen"
                uen:Navigation.Request="NotificationSettings" />
```

### Programmgesteuerte Push-Initialisierung

```csharp
var pushInitializer = serviceProvider.GetService<IPushNotificationInitializer>();
await pushInitializer.InitializeAsync();
```

### Permission-Status prüfen

```csharp
var status = await pushInitializer.GetStatusAsync();
if (status == AccessState.Available)
{
    // Push ist verfügbar
}
```

## API-Integration

Das Feature kommuniziert mit folgenden API-Endpoints:

### GET `/api/notifications/preferences`
Lädt Benachrichtigungseinstellungen des aktuellen Users.

**Response:**
```json
{
  "isEnabled": true,
  "locations": ["Linz", "Wels", "Gmunden"]
}
```

### PUT `/api/notifications/preferences`
Aktualisiert Benachrichtigungseinstellungen.

**Request:**
```json
{
  "isEnabled": true,
  "locations": ["Linz", "Wels"]
}
```

### POST `/api/notifications/device`
Registriert Device-Token für Push-Benachrichtigungen.

**Request:**
```json
{
  "deviceToken": "fX3k9mP...",
  "platform": "iOS"
}
```

## Push-Notification-Workflow

```
1. App startet
   └─> InitializePushNotificationsAsync()
       └─> RequestAccess() (Permissions anfragen)
           └─> OnNewToken(token) (automatisch)
               └─> RegisterDeviceAsync(token, platform)
                   └─> API: POST /api/notifications/device

2. Neue Immobilie wird erstellt (Backend)
   └─> PropertyCreatedEvent published
       └─> PropertyCreatedEventHandler
           └─> PushNotificationService.SendPropertyNotificationAsync()
               └─> Prüft NotificationPreferences (Location-Match)
                   └─> Sendet Push an alle Device-Tokens

3. User erhält Notification
   ├─> App im Vordergrund: OnReceived()
   └─> User tippt: OnEntry() → Navigation zu Immobilie
```

## Konfiguration

### Startup-Registrierung

In `ServiceCollectionExtensions.cs`:
```csharp
public static IServiceCollection AddNotificationsFeature(this IServiceCollection services)
{
    // Shiny.Push mit Native Provider
    services.AddPush<PushNotificationDelegate>();

    return services;
}
```

### App-Startup

In `App.xaml.cs`:
```csharp
protected async override void OnLaunched(LaunchActivatedEventArgs args)
{
    // ... Host Build ...

    Host = await builder.NavigateAsync<Shell>();

    // Push-Initialisierung nach App-Start
    _ = InitializePushNotificationsAsync();
}
```

## Plattform-Spezifische Anforderungen

### iOS / macOS
- **entitlements.plist** mit Push-Capability erforderlich
- **Provisioning Profile** muss Push unterstützen
- ⚠️ Push funktioniert **nicht** im iOS Simulator

### Android
- Firebase Cloud Messaging wird automatisch verwendet
- Keine zusätzliche Konfiguration erforderlich (Native Provider)
- `IntentFilter` für `Shiny.ShinyPushIntents.NotificationClickAction` ist automatisch registriert

### Windows
- Windows Push Notification Services (WNS)
- Keine zusätzliche Konfiguration erforderlich

## Dependencies

- `Shiny.Push` (4.0.0-beta-0095)
- `Shiny.Core` (4.0.0-beta-0095)
- `Heimatplatz.Features.Auth.Contracts` (für Authentication)
- `Heimatplatz.Core.ApiClient` (für HTTP-Client)

## Testing

### Manuelles Testing

1. App starten
2. Push-Permission erlauben
3. Einloggen
4. Profil-Menü → "Benachrichtigungen"
5. Standorte hinzufügen (z.B. "Linz", "Wels")
6. Im Backend: Neue Immobilie in "Linz" erstellen
7. Push-Benachrichtigung sollte auf dem Device ankommen
8. Auf Notification tippen → App öffnet (Navigation TBD)

### Logs prüfen

Push-Events werden geloggt:
```
[INF] Initializing push notifications...
[INF] Push notifications enabled. Token: fX3k9mP...
[INF] New push token received: fX3k9mP...
[INF] Device token registered successfully with API
[INF] Push notification received: Neue Immobilie in Linz - Einfamilienhaus...
```

## Troubleshooting

### "Push notifications are not supported on this platform"
- iOS Simulator unterstützt keine Push
- Teste auf echtem Gerät

### "Push notification permission denied by user"
- User hat Permissions abgelehnt
- In System-Einstellungen Permissions aktivieren

### "Failed to register device token with API"
- API nicht erreichbar
- Authentication-Token ungültig
- Netzwerkverbindung prüfen

### Token wird nicht empfangen
- Provisioning Profile prüfen (iOS)
- Google Services Konfiguration prüfen (Android - falls nicht Native)
- Logs auf Fehler prüfen

## Bekannte Limitierungen

- Navigation zu Immobilien-Detail bei Tap ist noch nicht implementiert
- Notification-Sound/Badge-Konfiguration noch nicht verfügbar
- Rich Notifications (Bilder) noch nicht unterstützt

## Weitere Entwicklung

### TODO
- [ ] Navigation zu Property-Detail bei OnEntry()
- [ ] Rich Notifications mit Immobilien-Bild
- [ ] Notification-Sound konfigurierbar
- [ ] Badge-Count Management
- [ ] Notification History im UI
- [ ] Push-Statistiken (gesendet/empfangen/geöffnet)
