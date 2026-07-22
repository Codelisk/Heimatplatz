# Heimatplatz.Api.Features.Legal

Feature fuer rechtliche Dokumente (Datenschutz, Impressum) **und die Kontakt-Stammdaten**
der Plattform. Dieses Feature ist die einzige Quelle fuer Firmenname, Adresse, E-Mail und
Telefonnummer - Web, MAUI und die Marketing-Signatur holen sie hier ab.

## Architektur

### Entity: LegalSettings

Speichert rechtliche Dokumente mit JSON-Feldern fuer Flexibilitaet:

| Feld | Typ | Beschreibung |
|------|-----|--------------|
| SettingType | string | `PrivacyPolicy`, `Imprint`, `Contact` (siehe `LegalSettingTypes`) |
| ResponsiblePartyJson | string | JSON mit Verantwortlichen-/Firmen-/Kontaktdaten |
| SectionsJson | string | JSON-Array mit Dokumentabschnitten (bei `Contact` leer) |
| Version | string | Versionsnummer |
| EffectiveDate | DateTimeOffset | Gueltig ab |
| IsActive | bool | Aktive Version |

### Die drei Datensaetze

- **`Imprint`** - Pflichtangaben nach ECG §5 / UGB §14 (`ImprintPartyDto`). Die Quelle fuer
  Firma, Adresse, E-Mail, Telefon und Website.
- **`PrivacyPolicy`** - Verantwortlicher + Rechtstext-Abschnitte (`ResponsiblePartyDto`).
- **`Contact`** - nur Ergaenzungen und Overrides (`ContactSettingsDto`): Support-Adresse,
  Erreichbarkeit, Social-Profile, optional abweichende E-Mail/Telefon/Website. Alle Felder
  optional; leer bedeutet "Impressum-Wert verwenden".

`GET /api/legal/contact` fuehrt Impressum und Contact zu `ContactInfoDto` zusammen
(`ContactInfoFactory`). Die Frontends muessen daraus nichts mehr ableiten:

- `SupportEmail` ist bereits aufgeloest und nie leer
- `PhoneLink` ist fertig fuer `href="tel:..."` normalisiert (`PhoneNumberFormatter`)
- nicht gepflegte Angaben kommen als `null` - Frontends blenden die Zeile aus

### API Endpoints

| Methode | Route | Schutz | Zweck |
|---------|-------|--------|-------|
| GET | `/api/legal/privacy-policy` | oeffentlich | Datenschutzerklaerung |
| GET | `/api/legal/imprint` | oeffentlich | Impressum |
| GET | `/api/legal/contact` | oeffentlich | Kontaktdaten fuer Footer, JSON-LD, App |
| POST | `/api/admin/legal/contact` | `X-Admin-Key` | Kontakt-Zusatzfelder aendern |
| POST | `/api/admin/legal/imprint` | `X-Admin-Key` | Impressum-Stammdaten aendern |

Die Schreib-Endpoints nutzen den `IAdminAccessGuard` des Admin-Features (fail-closed: ohne
konfigurierten `Admin:ApiKey` ausserhalb von Development gesperrt) und liegen bewusst unter
`/api/admin/*`, damit zusaetzlich die Caddy-IP-Sperre greift. Fachliche Fehler kommen als
`Success = false` mit `Error`-Text, nicht als Exception.

## Verwendung

### Registrierung

```csharp
services.AddLegalFeature();
```

### Daten aendern

Ueber **`/intern/kontakt`** im Web - wirkt sofort, ohne Deploy und ohne Migration.

Die Seite fuellt beide Formulare mit dem aktuellen Stand vor und ersetzt den jeweiligen
Datensatz vollstaendig; ein leeres Feld loescht die Angabe also bewusst. Nach dem Speichern
verwirft `/intern/kontakt/_shared.ts` die SSR-Caches (`legal:contact`, `legal:imprint`,
`legal:privacy-policy`) - sonst zeigt das Web bis zu 10 Minuten den alten Stand.

Frueher ging das nur direkt in der Datenbank bzw. per SQL-`REPLACE`-Migration (siehe
`RemoveDoorNumberFromLegalAddress`) - das ist nicht mehr noetig.

## Seeding

`LegalSettingsSeeder` (Order 5, `IsDemoData = false`) legt alle drei Datensaetze an, falls
sie fehlen. Die Firmenwerte stehen **ausschliesslich** in `Data/CompanyMasterData.cs`.

Der `Contact`-Datensatz startet bewusst leer - alles faellt damit auf das Impressum zurueck.

> **Nicht duplizieren:** Es gab frueher drei weitere Kopien derselben Firmendaten (ein
> On-Demand-Seed im `GetPrivacyPolicyHandler`, die Migration `20260403000000_SeedLegalSettings`
> und die Web-Fallbacks). Sie waren auseinandergelaufen - die Telefonnummer war je nach
> Befuellungsweg gesetzt oder `null`. Der Handler-Seed ist entfernt; im Web gibt es nur noch
> `src/web/src/config/company.ts` als Notfall-Fallback bei nicht erreichbarer API.

## Telefonnummern

Immer international pflegen (`+43 664 1234567`). `PhoneNumberFormatter` entfernt Trennzeichen,
normalisiert `0043` zu `+43` und wirft die optionale `(0)` weg (bei `+43 (0)664 ...` muss sie
beim internationalen Waehlen entfallen). Eine fuehrende `0` ohne Laendervorwahl wird bewusst
**nicht** ergaenzt - eine geratene Vorwahl waehlt beim Nutzer eine fremde Nummer.
