# Heimatplatz.Api.Features.AiListing.Contracts

Request/Response-DTOs fuer das AiListing-Feature: Medien-Upload fuer den
Inserat-Wizard und die KI-Generierung der Inserats-Beschreibung.

## Zweck

Der Wizard in der Mobile-App erfasst alle Inseratsdaten manuell; nur die Beschreibung
kann optional aus Stichwoertern/Diktat + Fotos generiert werden. Die Generierung laeuft
als Hintergrund-Job im PropertyDrafts-Feature und ruft dieses Feature in-process auf.

## Contracts

| Contract | Endpoint | Beschreibung |
|----------|----------|--------------|
| `UploadListingMediaRequest` | `POST /api/ai-listings/media` | Fotos (und Alt-Videos) als Base64 hochladen, liefert URLs |
| `GenerateListingDescriptionRequest` | — (in-process, kein HTTP) | Generiert eine Beschreibung aus Eckdaten, Stichwoertern und Foto-URLs; Wortbereich fix im Backend (`AiListing:Description`) |

## Modelle

- `Base64MediaData` — Base64-Upload-Daten (Foto oder Video).

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Properties.Contracts` (fuer `PropertyType`)
- `Shiny.Mediator.Contracts`
