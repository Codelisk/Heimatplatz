# Heimatplatz.Api.Features.SrealListings.Contracts

Contracts-Projekt fuer das SrealListings Feature. Enthaelt Request/Response DTOs und Enums fuer die Mediator-basierte Kommunikation.

## Inhalt

- **SrealObjectType** — Enum: House, Land, Vacation
- **TriggerSrealSyncRequest** — Fire-and-forget Sync-Trigger
- **GetSrealSyncStatusRequest** — Status-Abfrage (LastSync, Counts)
- **GetSrealListingsRequest** — Paginierte Abfrage der Rohdaten

## Abhaengigkeiten

- Shiny.Mediator.Contracts
- ForeclosureAuctions.Contracts (AustrianState Enum)
