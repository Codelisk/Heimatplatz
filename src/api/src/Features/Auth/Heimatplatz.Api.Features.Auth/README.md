# Heimatplatz.Api.Features.Auth

Auth Feature der API - Benutzerregistrierung und Authentifizierung.

## Inhalt

### Data/Entities
- `User`: Benutzer-Entity mit Vorname, Nachname, Email, PasswordHash

### Data/Configurations
- `UserConfiguration`: EF Core Konfiguration fuer User Entity

### Handlers
- `RegisterHandler`: Verarbeitet Benutzerregistrierung

### Services
- `IPasswordHasher` / `PasswordHasher`: Passwort-Hashing mit BCrypt

## API Endpoints

| Method | Route | OperationId | Beschreibung |
|--------|-------|-------------|--------------|
| POST | /api/auth/register | Register | Neuen Benutzer registrieren |

## Verwendung

```csharp
services.AddAuthFeature();
```
