# Heimatplatz.Api.Features.PropertyImport

## Zweck

Dieses Feature ermoeglicht den Batch-Import von Properties aus externen Quellen wie Immobilienportalen (Willhaben, ImmoScout24, etc.).

## Hauptkomponenten

### Handlers

- **ImportPropertiesHandler** - Verarbeitet Batch-Import Requests mit Upsert-Logik

## Funktionsweise

### Upsert-Logik

1. Fuer jede Property im Batch:
   - Suche existierende Property via `(SourceName, SourceId)`
   - Wenn gefunden und `UpdateExisting=true` -> Update
   - Wenn gefunden und `UpdateExisting=false` -> Skip
   - Wenn nicht gefunden -> Create

### Automatisch gesetzte Felder

Diese Felder werden beim Import NICHT aus dem Request uebernommen:

| Feld | Wann | Wert |
|------|------|------|
| `Id` | Create | Neue GUID |
| `CreatedAt` | Create | Aktueller Zeitstempel |
| `UpdatedAt` | Update | Aktueller Zeitstempel |
| `UserId` | Create | Authentifizierter User |
| `ContactInfo.Source` | Immer | `ContactSource.Import` |

## API Endpoint

```
POST /api/import/properties
Content-Type: application/json
Authorization: Bearer {token}

{
  "properties": [...],
  "updateExisting": true
}
```

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Properties` - Property Entity, ContactInfo
- `Heimatplatz.Api.Features.Auth` - User Authentication
- `Heimatplatz.Api.Core.Data` - DbContext
