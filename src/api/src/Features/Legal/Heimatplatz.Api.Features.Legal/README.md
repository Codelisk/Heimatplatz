# Heimatplatz.Api.Features.Legal

Feature fuer rechtliche Dokumente wie Datenschutzerklaerung, Impressum und AGB.

## Architektur

### Entity: LegalSettings

Speichert rechtliche Dokumente mit JSON-Feldern fuer Flexibilitaet:

| Feld | Typ | Beschreibung |
|------|-----|--------------|
| SettingType | string | "PrivacyPolicy", "Imprint", "Terms" |
| ResponsiblePartyJson | string | JSON mit Verantwortlichen-Daten |
| SectionsJson | string | JSON-Array mit Dokumentabschnitten |
| Version | string | Versionsnummer |
| EffectiveDate | DateTimeOffset | Gueltig ab |
| IsActive | bool | Aktive Version |

### API Endpoints

- `GET /api/legal/privacy-policy` - Holt aktive Datenschutzerklaerung

## Verwendung

### Registrierung

```csharp
services.AddLegalFeature();
```

### Daten aendern

Aktuell koennen Daten nur direkt in der Datenbank geaendert werden.
Fuer Admin-UI kann spaeter ein UpdatePrivacyPolicyHandler hinzugefuegt werden.

## Seeding

Der LegalSettingsSeeder erstellt automatisch eine Standard-Datenschutzerklaerung
beim ersten Start der Anwendung.
