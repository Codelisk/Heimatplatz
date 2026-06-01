# Heimatplatz.Api.Features.Auth

Auth Feature der API - Benutzerregistrierung und Authentifizierung.

## Inhalt

### Data/Entities
- `User`: Benutzer-Entity mit Vorname, Nachname, Email, PasswordHash

### Data/Configurations
- `UserConfiguration`: EF Core Konfiguration fuer User Entity

### Handlers
- `RegisterHandler`: Verarbeitet Benutzerregistrierung
- `LoginHandler` / `RefreshTokenHandler`: Login und Token-Erneuerung
- `DeleteAccountHandler`: Loescht das Konto des authentifizierten Benutzers vollstaendig (siehe unten)

### Services
- `IPasswordHasher` / `PasswordHasher`: Passwort-Hashing mit BCrypt

## API Endpoints

| Method | Route | OperationId | Beschreibung |
|--------|-------|-------------|--------------|
| POST | /api/auth/register | Register | Neuen Benutzer registrieren |
| POST | /api/auth/login | Login | Anmelden, Tokens erhalten |
| POST | /api/auth/refresh | RefreshToken | Access-Token erneuern |
| DELETE | /api/auth/account | DeleteAccount | Eigenes Konto endgueltig loeschen (Auth erforderlich) |

## Konto-Loeschung (Apple Guideline 5.1.1(v) / DSGVO Art. 17)

`DeleteAccountHandler` loescht den authentifizierten Benutzer (per JWT `sub`-Claim) und
**alle** zugehoerigen Daten unwiderruflich - innerhalb einer Transaktion und in
FK-sicherer Reihenfolge (explizites `ExecuteDelete`, kein Verlass auf DB-Cascade).

Die Loeschung ist **entkoppelt** ueber das Contributor-Pattern `IUserDataEraser`
(definiert in `Heimatplatz.Api.Shared`, Namespace `Heimatplatz.Api.Cleanup`):

- Jedes Feature, das benutzerbezogene Daten haelt, registriert einen eigenen Eraser
  (z.B. `PropertiesUserDataEraser`, `NotificationsUserDataEraser`) per
  `services.AddScoped<IUserDataEraser, ...>()` in seinem `Add{Feature}Feature()`.
- `DeleteAccountHandler` ruft alle Eraser sortiert nach `Order` auf, loescht danach die
  Auth-eigenen Daten (`RefreshToken`, `UserRole`, `UserFilterPreferences`) und zuletzt den `User`.
- Das Auth-Feature kennt damit **keine** Entities anderer Features.

## Verwendung

```csharp
services.AddAuthFeature();
```
