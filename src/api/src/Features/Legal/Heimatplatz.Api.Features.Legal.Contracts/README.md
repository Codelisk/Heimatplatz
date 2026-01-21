# Heimatplatz.Api.Features.Legal.Contracts

Contracts-Projekt fuer das Legal-Feature. Enthaelt DTOs und Mediator-Requests fuer rechtliche Dokumente wie Datenschutzerklaerung, Impressum und AGB.

## Models

- **ResponsiblePartyDto** - Daten des Verantwortlichen (Firma, Adresse, Kontakt)
- **LegalSectionDto** - Ein Abschnitt in einem rechtlichen Dokument
- **PrivacyPolicyDto** - Vollstaendige Datenschutzerklaerung

## Mediator Requests

- **GetPrivacyPolicyRequest** - Holt die aktuelle Datenschutzerklaerung

## Verwendung

```csharp
// Datenschutzerklaerung abrufen
var response = await mediator.Request(new GetPrivacyPolicyRequest());
var privacyPolicy = response.PrivacyPolicy;
```
