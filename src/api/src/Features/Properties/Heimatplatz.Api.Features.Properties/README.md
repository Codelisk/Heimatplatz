# Heimatplatz.Api.Features.Properties

Hauptprojekt fuer das Properties (Immobilien) Feature.

## Inhalt

### Data
- `Property` Entity - Immobilien-Datenmodell
- `PropertyConfiguration` - EF Core Konfiguration
- `PropertySeeder` - Beispieldaten fuer Entwicklung
- `PropertyChange` Entity - Aenderungs-Journal (Created/Updated/Deleted) fuer den Client-Delta-Sync;
  bewusst ohne FK, damit Tombstones geloeschte Immobilien ueberleben
- `PropertyChangeInterceptor` (`ISaveChangesInterceptor`) - erfasst zentral ALLE Immobilien-
  Mutationen (User-Handler, Import, Zwangsversteigerungs-Sync, Seeder) beim SaveChanges und
  schreibt Journal-Zeilen. Kontakt-Aenderungen zaehlen als Update der Immobilie.
  Registriert als `IInterceptor` in `AddPropertiesFeature()`, eingebunden ueber `AddAppData()`.
  Achtung: `ExecuteDelete`/`ExecuteUpdate` umgehen den Interceptor - solche Stellen muessen
  selbst journalieren (siehe `PropertiesUserDataEraser`).

### Handlers
- `GetPropertiesHandler` - Gefilterte Immobilien-Liste
- `GetPropertyByIdHandler` - Einzelne Immobilie
- `GetPropertyChangesHandler` - `GET /api/properties/changes?Since=...`: Delta-Sync fuer
  Client-Caches. Dedupliziert das Journal pro Immobilie (Deleted gewinnt; Created+Updated = Created)
  und liefert fuer Created/Updated die aktuellen `PropertyListItemDto`-Daten mit. Ohne `Since`
  oder ausserhalb der 30-Tage-Retention: `FullResyncRequired=true`. Antwort enthaelt `Watermark`
  als naechstes `Since`. SQLite filtert in-memory (DateTimeOffset nicht uebersetzbar), Postgres in SQL.

### Infrastructure
- `PropertyChangeRetentionWorker` - loescht Journal-Eintraege aelter als 30 Tage (taeglich)

### Services
- `PropertiesUserDataEraser` (`IUserDataEraser`) - loescht bei der Konto-Loeschung die Inserate,
  Favoriten und Blockierungen eines Benutzers (via `ExecuteDelete`, journaliert Tombstones manuell).
  Registriert in `AddPropertiesFeature()`.

## Verwendung

```csharp
// In ServiceCollectionExtensions.cs
services.AddPropertiesFeature();
```
