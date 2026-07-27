import { isCourtForeclosure, type ApiProperty } from "@/features/properties/live-api";
import { fetchForeclosureAuctions, getForeclosureAuctionPath } from "./api";

/**
 * Zwangsversteigerungen liegen doppelt vor: als Auktion aus dem Edikt und als
 * davon gespiegelte Property (ForeclosurePropertySyncService), damit die Suche
 * sie ueberhaupt findet. Ohne Kanon ist dasselbe Objekt unter zwei
 * indexierbaren URLs erreichbar - Duplicate Content (WEB-B08).
 *
 * Kanonisch ist `/zwangsversteigerungen/<slug>/`: sprechender Slug,
 * ZV-spezifische Darstellung, und Auktionen ohne Spiegel gibt es ohnehin nur
 * unter dieser Adresse. Die Spiegel-Properties bleiben erreichbar (Suche,
 * Karte, Favoriten verlinken sie), zeigen aber per `canonical` auf die
 * ZV-Seite und stehen nicht mehr in Sitemap und llms.txt.
 */
export function isMirroredForeclosure(property: Pick<ApiProperty, "Type" | "SourceName">) {
  return property.Type === "Foreclosure" && isCourtForeclosure(property);
}

/**
 * Edikt-URL einer gespiegelten Property. Der Sync uebernimmt sie 1:1 aus der
 * Auktion (TypeSpecificData.EdictUrl und Kontakt-OriginalListingUrl) und sie
 * ist damit der einzige im Property-DTO enthaltene Schluessel zur Auktion -
 * SourceId (= ExternalId des Edikts) liefert die API nicht aus.
 */
function getPropertyEdictUrl(property: ApiProperty) {
  const raw = property.TypeSpecificData;
  const data = typeof raw === "string"
    ? (() => {
        try {
          return JSON.parse(raw) as Record<string, unknown>;
        } catch {
          return {};
        }
      })()
    : raw ?? {};

  const edictUrl = (data as Record<string, unknown>).EdictUrl;
  if (typeof edictUrl === "string" && edictUrl) return edictUrl;

  return property.Contacts?.find((contact) => contact.OriginalListingUrl)?.OriginalListingUrl ?? "";
}

/**
 * Kanonischer Pfad einer gespiegelten Zwangsversteigerung, oder null wenn die
 * Property keine ist bzw. die Auktion nicht (mehr) in der Liste steht - dann
 * bleibt die Angebots-URL ihr eigener Kanon.
 */
export async function getForeclosureCanonicalPath(property: ApiProperty) {
  if (!isMirroredForeclosure(property)) return null;

  const edictUrl = getPropertyEdictUrl(property);
  if (!edictUrl) return null;

  const auctions = await fetchForeclosureAuctions();
  const match = auctions.find((auction) => auction.EdictUrl === edictUrl);
  return match ? getForeclosureAuctionPath(match) : null;
}
