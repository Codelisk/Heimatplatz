import type { APIRoute } from "astro";
import { SITE } from "@/config/site";
import { fetchForeclosureAuctions } from "@/features/foreclosures/api";
import { isMirroredForeclosure } from "@/features/foreclosures/property-mirror";
import {
  API_PROPERTY_LIST_LIMIT,
  fetchApiProperties,
} from "@/features/properties/live-api";
import { auctionBlock, mdLink, propertyBlock, textResponse } from "@/lib/llms";

/**
 * Vollstaendige Inseratsdaten fuer LLM-Ingestion (llms.txt-Konvention:
 * llms-full.txt traegt den kompletten Inhalt, llms.txt nur die Uebersicht).
 * Ein Abruf liefert alle aktuellen Inserate und Zwangsversteigerungen mit
 * Preis, Flaechen, Anbieter und Beschreibung — dieselben Daten wie die
 * Detailseiten, aber ohne HTML-Ballast.
 */
export const GET: APIRoute = async () => {
  const [allProperties, auctions] = await Promise.all([
    fetchApiProperties({ pageSize: API_PROPERTY_LIST_LIMIT }),
    fetchForeclosureAuctions(),
  ]);
  // Gespiegelte Zwangsversteigerungen stehen unten als Auktion mit allen
  // Edikt-Daten (WEB-B08, siehe property-mirror.ts)
  const properties = allProperties.filter((property) => !isMirroredForeclosure(property));

  const body = `# ${SITE.name} — Aktuelle Inserate (Volltext)

> Vollständige Daten aller aktuellen Immobilien-Inserate und gerichtlichen Zwangsversteigerungen in Oberösterreich. Kompakte Übersicht: ${mdLink("llms.txt", "/llms.txt")}

## Immobilien-Inserate (${properties.length})

${properties.map(propertyBlock).join("\n\n") || "Derzeit keine Inserate verfügbar."}

## Zwangsversteigerungen (${auctions.length})

${auctions.map(auctionBlock).join("\n\n") || "Derzeit keine Zwangsversteigerungen verfügbar."}
`;

  return textResponse(body);
};
