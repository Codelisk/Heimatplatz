# Heimatplatz.Api.Features.AiListing.Contracts

Request/Response-DTOs fuer das AiListing-Feature: KI-gestuetzte Erstellung von
Immobilien-Inseraten aus Fotos, Videos und diktiertem Text.

## Zweck

Der primaere Weg zur Inseratserstellung in der Mobile-App: Der Nutzer laedt Medien
hoch und diktiert eine Beschreibung; die KI extrahiert daraus die Inseratsfelder.
Preis, Adresse, Gemeinde und Verkaeuferdaten bleiben bewusst manuelle Eingaben.

## Contracts

| Contract | Endpoint | Beschreibung |
|----------|----------|--------------|
| `UploadListingMediaRequest` | `POST /api/ai-listings/media` | Fotos/Videos als Base64 hochladen, liefert URLs |
| `StartListingAnalysisRequest` | `POST /api/ai-listings` | Startet asynchrone KI-Analyse, liefert `AnalysisId` |
| `GetListingAnalysisRequest` | `GET /api/ai-listings/{AnalysisId}` | Status-Polling (`Queued` → `InProgress` → `Finished`/`Failed`) |

## Modelle

- `ExtractedListingData` — von der KI befuellte Felder: Titel, Beschreibung, Typ,
  Zimmer, Wohnflaeche, Grundstuecksflaeche, Baujahr, Ausstattung (Features), Summary.
- `ListingAnalysisStatus` — Job-Lebenszyklus (`Queued`, `InProgress`, `Finished`, `Failed`).
- `Base64MediaData` — Base64-Upload-Daten (Foto oder Video).

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Properties.Contracts` (fuer `PropertyType`)
- `Shiny.Mediator.Contracts`
