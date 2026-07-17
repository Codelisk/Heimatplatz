# Heimatplatz.Api.Features.Auth

Auth Feature der API - Benutzerregistrierung, Authentifizierung und Profilverwaltung.

## Rollenmodell

**Jeder authentifizierte Benutzer ist implizit Kaeufer** (suchen, favorisieren, Filter
speichern - Endpoints verlangen nur `RequiresAuthorization`).

- **Verkaeufer** ist, wer im Profil einen `SellerType` gesetzt hat
  (`Private`, `Broker`, `PropertyManager`). Broker und PropertyManager brauchen einen
  `CompanyName`. Der JWT enthaelt dann `user_role=Seller` und `seller_type=<Typ>`.
- **Admin** (`User.IsAdmin`) ist eine System-Rolle fuer Batch-Import u.ae.
  (`user_role=Admin`, Policy `RequireAdmin`).
- Es gibt keine `UserRole`-Tabelle mehr; die Registrierungsentscheidung ist ueber
  `PUT /api/auth/profile` jederzeit aenderbar.

## Inhalt

### Data/Entities
- `User`: Benutzer-Entity mit FirstName, LastName, Email (normalisiert: lowercase),
  PasswordHash, SellerType?, CompanyName?, IsAdmin

### Data/Configurations
- `UserConfiguration`: EF Core Konfiguration fuer User Entity

### Handlers
- `RegisterHandler`: Registrierung inkl. vollstaendiger serverseitiger Validierung
  (E-Mail-Format/-Normalisierung, Passwort-Policy, Verkaeufer-Angaben) und Auto-Login
- `LoginHandler` / `RefreshTokenHandler`: Login und Token-Erneuerung
- `GetProfileHandler` / `UpdateProfileHandler`: Eigenes Profil lesen/aendern
  (inkl. Verkaeufer werden / Anbietertyp wechseln; Response enthaelt frischen Access Token)
- `ChangePasswordHandler`: Passwort aendern, widerruft alle Refresh Tokens
- `DeleteAccountHandler`: Loescht das Konto des authentifizierten Benutzers vollstaendig (siehe unten)

### Services
- `IPasswordHasher` / `PasswordHasher`: Passwort-Hashing mit BCrypt
- `ITokenService` / `TokenService`: JWT-Erzeugung; Claims werden direkt aus dem User abgeleitet
- `UserInputValidator`: zentrale Validierung/Normalisierung der Benutzereingaben

## API Endpoints

| Method | Route | OperationId | Beschreibung |
|--------|-------|-------------|--------------|
| POST | /api/auth/register | Register | Neuen Benutzer registrieren (SellerType optional) |
| POST | /api/auth/login | Login | Anmelden, Tokens erhalten |
| POST | /api/auth/refresh | RefreshToken | Access-Token erneuern |
| GET | /api/auth/profile | GetProfile | Eigenes Profil lesen (Auth erforderlich) |
| PUT | /api/auth/profile | UpdateProfile | Namen/Verkaeufer-Einstellungen aendern (Auth erforderlich) |
| POST | /api/auth/change-password | ChangePassword | Passwort aendern, andere Sessions beenden (Auth erforderlich) |
| DELETE | /api/auth/account | DeleteAccount | Eigenes Konto endgueltig loeschen (Auth erforderlich) |

## Konto-Loeschung (Apple Guideline 5.1.1(v) / DSGVO Art. 17)

`DeleteAccountHandler` loescht den authentifizierten Benutzer (per JWT `sub`-Claim) und
**alle** zugehoerigen Daten unwiderruflich - innerhalb einer Transaktion und in
FK-sicherer Reihenfolge (explizites `ExecuteDelete`, kein Verlass auf DB-Cascade).
Bewusst ohne Rollen-Policy: jedes authentifizierte Konto muss sich loeschen koennen.

Die Loeschung ist **entkoppelt** ueber das Contributor-Pattern `IUserDataEraser`
(definiert in `Heimatplatz.Api.Shared`, Namespace `Heimatplatz.Api.Cleanup`):

- Jedes Feature, das benutzerbezogene Daten haelt, registriert einen eigenen Eraser
  (z.B. `PropertiesUserDataEraser`, `NotificationsUserDataEraser`) per
  `services.AddScoped<IUserDataEraser, ...>()` in seinem `Add{Feature}Feature()`.
- `DeleteAccountHandler` ruft alle Eraser sortiert nach `Order` auf, loescht danach die
  Auth-eigenen Daten (`RefreshToken`, `UserFilterPreferences`) und zuletzt den `User`.
- Das Auth-Feature kennt damit **keine** Entities anderer Features.

## Verwendung

```csharp
services.AddAuthFeature();
```
