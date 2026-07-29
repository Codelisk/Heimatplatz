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
- `GetPropertyMapPinsHandler` - `GET /api/properties/map-pins`: alle Treffer der aktuellen
  Filter als leichte Pins fuer die Kartenansicht im Web (gleiche Filter-Parameter wie die
  Listen-Suche, ohne Paging/Sortierung, Deckel 500 Pins). Nutzt `PropertyQueryFilters`,
  damit Karte und Liste nie auseinanderlaufen. Anzeige-Logik: `Property.LocationDisplay`
  (Anbieter-Wahl Genau/Ungefaehr/Verborgen) x `IsLocationExact` (Geocoding-Qualitaet) -
  punktgenau NUR wenn beides passt, Verborgen erscheint nie, alles andere wird
  deterministisch gestreut (`ApplyPrivacyJitter`).
- `GeocodePropertyPreviewHandler` - `POST /api/properties/geocode-preview` (RequireSeller):
  Live-Kartenvorschau im Inserats-Editor, loest die eingegebene Anschrift on-the-fly auf
  (fehlertolerant, durch die Geocoder-Drossel ratenbegrenzt).
- `GetPropertyByIdHandler` - Einzelne Immobilie
- `GetPropertyChangesHandler` - `GET /api/properties/changes?Since=...`: Delta-Sync fuer
  Client-Caches. Dedupliziert das Journal pro Immobilie (Deleted gewinnt; Created+Updated = Created)
  und liefert fuer Created/Updated die aktuellen `PropertyListItemDto`-Daten mit. Ohne `Since`
  oder ausserhalb der 30-Tage-Retention: `FullResyncRequired=true`. Antwort enthaelt `Watermark`
  als naechstes `Since`. SQLite filtert in-memory (DateTimeOffset nicht uebersetzbar), Postgres in SQL.
  `Since` wird tolerant gelesen (`TryParseSince`): Aeltere MAUI-Staende senden das Zonen-Plus
  unkodiert, es kommt als Leerzeichen an - ohne diese Reparatur bekaemen sie bei **jedem**
  Sync `FullResyncRequired=true` (Delta-Sync tot, App zeigt dauerhaft "Neue Inserate").

### Infrastructure
- `PropertyChangeRetentionWorker` - loescht Journal-Eintraege aelter als 30 Tage (taeglich)

### Services
- `PropertiesUserDataEraser` (`IUserDataEraser`) - loescht bei der Konto-Loeschung die Inserate,
  Favoriten und Blockierungen eines Benutzers (via `ExecuteDelete`, journaliert Tombstones manuell).
  Registriert in `AddPropertiesFeature()`.
- `PropertyQueryFilters` - gemeinsame Filterlogik von Listen-Suche und Kartenansicht
  (Typ/Anbieter/Gemeinden/Alter/Preis/Flaeche/Zimmer/Volltext/Blockiert-Ausschluss).
- `IPropertyGeocoder` / `NominatimPropertyGeocoder` - Adresse -> WGS84 (`Property.Latitude/
  Longitude/IsLocationExact`). Opt-in ueber `Geocoding:Enabled` (ohne Konfiguration keine
  externen Requests - Tests/CI bleiben offline), prozessweit auf 1 Request/Sekunde gedrosselt
  (Nominatim-Policy), fehlertolerant (null statt Exception). Laeuft beim Anlegen/Bearbeiten,
  im ZV-Sync (gedeckelt pro Lauf) und im Admin-Backfill `POST /api/admin/properties/geocode`.
  Seed-Inserate bekommen ihre Koordinaten direkt aus `PropertySeeder.SeedCityCoordinates`
  (Backfill fuer Bestands-DBs: `PropertyCoordinateBackfillSeeder`, Order 13).

## Verwendung

```csharp
// In ServiceCollectionExtensions.cs
services.AddPropertiesFeature();
```
