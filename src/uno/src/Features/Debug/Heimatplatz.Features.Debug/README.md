# Heimatplatz.Features.Debug

## Zweck

Debug-Feature für schnelles Testen und Entwicklung der Heimatplatz-App. Bietet einen Floating-Button mit Schnellzugriff auf häufig benötigte Debug-Funktionen.

**Wichtig:** Dieses Feature ist nur in DEBUG-Builds verfügbar und wird in Release-Builds komplett entfernt.

## Funktionen

### Quick Login
Schneller Login mit vordefinierten Test-Usern ohne manuelles Ausfüllen von Formularen:
- **Buyer** - Nur Käufer-Rolle
- **Seller** - Nur Verkäufer-Rolle
- **Both** - Beide Rollen

### Navigation Shortcuts
Direkter Zugriff auf wichtige App-Seiten:
- Home
- Add Property
- Login Page
- Register Page

### Debug Info
- Anzeige der aktuellen User-Email
- Anzeige der aktiven Rollen
- Token in Zwischenablage kopieren

## Test-User Credentials

Alle Test-User verwenden das Passwort: `Test123!`

| Email | Rollen | Verwendung |
|-------|--------|------------|
| `test.buyer@heimatplatz.dev` | Buyer | Käufer-spezifische Features testen |
| `test.seller@heimatplatz.dev` | Seller | Verkäufer-spezifische Features testen |
| `test.both@heimatplatz.dev` | Buyer + Seller | Features testen die beide Rollen benötigen |

## Verwendung

Das Debug-Overlay erscheint automatisch als Floating-Button in der rechten oberen Ecke der App (nur in DEBUG-Builds).

1. Klicke auf den Debug-Button (⚙️)
2. Wähle eine Quick-Login-Option oder Navigation-Shortcut
3. Die Statusmeldung zeigt den Erfolg der Aktion an

## Technische Details

### Komponenten

- **IDebugAuthService** - Interface für Debug-Authentifizierung
- **DebugAuthService** - Service-Implementierung mit `[Service]` Attribut
- **DebugOverlay** - UI-Control (XAML + Code-Behind)
- **DebugOverlayViewModel** - ViewModel mit Commands

### Dependencies

- `Heimatplatz.Core.ApiClient` - API-Kommunikation
- `Heimatplatz.Features.Auth` - Auth-Service für Token-Verwaltung
- `Shiny.Mediator` - Mediator Pattern für API-Calls
- `UnoFramework` - Navigation

### Conditional Compilation

Alle Debug-Feature-Klassen sind mit `#if DEBUG` / `#endif` umschlossen, sodass sie:
- In Debug-Builds kompiliert und verfügbar sind
- In Release-Builds komplett entfernt werden (kein Code-Overhead)

## Integration

Das Debug-Overlay wird programmatisch im Code-Behind der MainPage/Shell hinzugefügt:

```csharp
#if DEBUG
private void AddDebugOverlay()
{
    var overlay = new DebugOverlay { ... };
    rootGrid.Children.Add(overlay);
}
#endif
```

## Abhängigkeiten

- Heimatplatz.Core.Styles
- Heimatplatz.Core.ApiClient
- Heimatplatz.Shared
- Heimatplatz.Features.Auth
- UnoFramework
- UnoFramework.Contracts
