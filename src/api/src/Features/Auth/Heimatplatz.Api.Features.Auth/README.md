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
  PasswordHash, SellerType?, CompanyName?, IsAdmin, EmailVerifiedAt?
- `UserActionToken`: Einmal-Token fuer E-Mail-Verifikation und Passwort-Reset
  (nur der SHA-256-Hash liegt in der DB, der Klartext-Token steht ausschliesslich im Mail-Link)

### Data/Configurations
- `UserConfiguration`: EF Core Konfiguration fuer User Entity
- `UserActionTokenConfiguration`: EF Core Konfiguration fuer UserActionToken

### Handlers
- `RegisterHandler`: Registrierung inkl. vollstaendiger serverseitiger Validierung
  (E-Mail-Format/-Normalisierung, Passwort-Policy, Verkaeufer-Angaben) und Auto-Login;
  verschickt die Verifikations-Mail (best effort - Mail-Fehler brechen die Registrierung nicht)
- `LoginHandler` / `RefreshTokenHandler`: Login und Token-Erneuerung
- `GetProfileHandler` / `UpdateProfileHandler`: Eigenes Profil lesen/aendern
  (inkl. Verkaeufer werden / Anbietertyp wechseln; Response enthaelt frischen Access Token)
- `ChangePasswordHandler`: Passwort aendern, widerruft alle Refresh Tokens
- `VerifyEmailHandler`: E-Mail-Adresse per Token aus der Verifikations-Mail bestaetigen (anonym)
- `ResendVerificationEmailHandler`: Verifikations-Mail erneut anfordern (Auth erforderlich)
- `ForgotPasswordHandler`: "Passwort vergessen" - verschickt Reset-Mail; Antwort ist immer
  generisch (kein User-Enumeration-Leak)
- `ResetPasswordHandler`: Neues Passwort per Reset-Token setzen, widerruft alle Refresh
  Tokens und bestaetigt die E-Mail implizit mit (Postfach-Besitz nachgewiesen)
- `DeleteAccountHandler`: Loescht das Konto des authentifizierten Benutzers vollstaendig (siehe unten)

### Services
- `IPasswordHasher` / `PasswordHasher`: Passwort-Hashing mit BCrypt
- `ITokenService` / `TokenService`: JWT-Erzeugung; Claims werden direkt aus dem User abgeleitet
- `UserInputValidator`: zentrale Validierung/Normalisierung der Benutzereingaben
- `IAuthEmailService` / `AuthEmailService`: baut und verschickt Verifikations-/Reset-Mails
  (Token-Erzeugung + Links auf das Web-Frontend, via `Heimatplatz.Api.Core.Email`)
- `UserActionTokens`: Token-Erzeugung (256 Bit Zufall, hex) und SHA-256-Hashing,
  Gueltigkeiten (Verifikation 3 Tage, Reset 2 Stunden)

## API Endpoints

| Method | Route | OperationId | Beschreibung |
|--------|-------|-------------|--------------|
| POST | /api/auth/register | Register | Neuen Benutzer registrieren (SellerType optional) |
| POST | /api/auth/login | Login | Anmelden, Tokens erhalten |
| POST | /api/auth/refresh | RefreshToken | Access-Token erneuern |
| GET | /api/auth/profile | GetProfile | Eigenes Profil lesen (Auth erforderlich) |
| PUT | /api/auth/profile | UpdateProfile | Namen/Verkaeufer-Einstellungen aendern (Auth erforderlich) |
| POST | /api/auth/change-password | ChangePassword | Passwort aendern, andere Sessions beenden (Auth erforderlich) |
| POST | /api/auth/verify-email | VerifyEmail | E-Mail-Adresse per Token bestaetigen (anonym) |
| POST | /api/auth/resend-verification | ResendVerificationEmail | Verifikations-Mail erneut senden (Auth erforderlich) |
| POST | /api/auth/forgot-password | ForgotPassword | Passwort-Reset-Mail anfordern (anonym, generische Antwort) |
| POST | /api/auth/reset-password | ResetPassword | Neues Passwort per Reset-Token setzen (anonym) |
| DELETE | /api/auth/account | DeleteAccount | Eigenes Konto endgueltig loeschen (Auth erforderlich) |

Alle vier neuen Endpunkte laufen im strengen Rate-Limit-Bucket (10/min pro IP, siehe
`Program.cs`): forgot-password/resend-verification loesen Mail-Versand aus, verify-email/
reset-password nehmen Einmal-Tokens entgegen.

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
