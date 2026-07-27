import type { APIRoute } from "astro";
import { SITE } from "@/config/site";
import { fetchForeclosureAuctions } from "@/features/foreclosures/api";
import { isMirroredForeclosure } from "@/features/foreclosures/property-mirror";
import { fetchApiProperties } from "@/features/properties/live-api";
import { auctionLine, mdLink, propertyLine, textResponse } from "@/lib/llms";

/**
 * Kompakte, LLM-freundliche Uebersicht nach llms.txt-Spezifikation (llmstxt.org).
 * Dynamisch: verlinkt die aktuellen Inserate und Zwangsversteigerungen direkt,
 * damit AI-Suchen (ChatGPT, Claude, Perplexity, ...) konkrete Angebote mit
 * URL zitieren koennen statt nur die Startseite.
 */
const LISTING_PREVIEW_LIMIT = 20;
const AUCTION_PREVIEW_LIMIT = 10;

export const GET: APIRoute = async () => {
  const [allProperties, auctions] = await Promise.all([
    fetchApiProperties({ pageSize: LISTING_PREVIEW_LIMIT }),
    fetchForeclosureAuctions(),
  ]);
  // Gespiegelte Zwangsversteigerungen stehen unten als Auktion mit allen
  // Edikt-Daten - unter "Inserate" waeren sie dasselbe Objekt ein zweites Mal
  // (WEB-B08, siehe property-mirror.ts)
  const properties = allProperties.filter((property) => !isMirroredForeclosure(property));

  const body = `# ${SITE.name}

> Immobilienportal für Oberösterreich (Österreich): Häuser und Grundstücke von privaten Anbietern, Maklern, Bauträgern und Verwaltungen sowie gerichtliche Zwangsversteigerungen — mit Filtern nach Bezirk und Typ, Favoriten und Push-Benachrichtigungen.

Heimatplatz ist eine deutschsprachige Web-App (de-AT). Zwangsversteigerungen basieren auf öffentlichen gerichtlichen Edikten und enthalten Termin, Gericht, Schätzwert und Mindestgebot. Alle Detailseiten liefern strukturierte Daten (schema.org: Offer, Residence, Event, BreadcrumbList).

## Einstieg

- ${mdLink("Startseite und Immobiliensuche", "/")}: Suche mit Filtern nach Bezirk, Immobilientyp, Anbieter, Zeitraum und Sortierung.
- ${mdLink("Immobilie inserieren", "/inserieren/")}: Inserat mit Fotos, Adresse, Preis und Kontaktdaten erstellen.
- ${mdLink("Impressum", "/impressum/")}: Betreiber- und Kontaktangaben.
- ${mdLink("Datenschutz", "/datenschutz/")}: Datenschutzerklärung.

## Aktuelle Inserate

${properties.map(propertyLine).join("\n") || "- Derzeit keine Inserate verfügbar."}

## Aktuelle Zwangsversteigerungen

${auctions.slice(0, AUCTION_PREVIEW_LIMIT).map(auctionLine).join("\n") || "- Derzeit keine Zwangsversteigerungen verfügbar."}

## Maschinenlesbare Ressourcen

- ${mdLink("sitemap.xml", "/sitemap.xml")}: alle indexierbaren URLs inklusive sämtlicher Detailseiten.
- ${mdLink("llms-full.txt", "/llms-full.txt")}: vollständige Daten aller aktuellen Inserate und Zwangsversteigerungen als Markdown.
`;

  return textResponse(body);
};
