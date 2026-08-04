/**
 * Serverseitiger Zugriff auf GET /api/partners fuer die oeffentliche /partner/-Seite.
 *
 * Muster wie features/legal/api.ts: TTL-Cache (10 Min, /intern/partner/ invalidiert
 * nach Mutationen), PascalCase/camelCase-tolerante Normalisierung, Fallback ist die
 * leere Liste - die Seite zeigt dann Hero + Werde-Partner-CTA statt einer Fehlerbox.
 */
import { getServerApiBaseUrl } from "@/lib/server/api-base";
import { cached, TTL } from "@/lib/server/ttl-cache";

export const PARTNERS_CACHE_KEY = "partners:list";

/** Kategorie-Konstanten aus PartnerCategories (API-Contracts) */
export const PARTNER_CATEGORY_BROKER = "Broker";
export const PARTNER_CATEGORY_DATA_SOURCE = "DataSource";

export type Partner = {
  id: string;
  name: string;
  category: string;
  description: string;
  websiteUrl: string;
  logoUrl: string;
  region: string;
  partnerSinceYear: number | null;
  sellerName: string;
  activeListingCount: number;
};

type RawRecord = Record<string, unknown>;

function isRecord(value: unknown): value is RawRecord {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value);
}

function getValue(record: RawRecord, key: string) {
  return record[key] ?? record[`${key.charAt(0).toLowerCase()}${key.slice(1)}`];
}

function readString(record: RawRecord, key: string) {
  const value = getValue(record, key);
  return typeof value === "string" ? value.trim() : "";
}

function readNumber(record: RawRecord, key: string) {
  const value = Number(getValue(record, key));
  return Number.isFinite(value) ? value : null;
}

function normalizePartner(record: RawRecord): Partner | null {
  const name = readString(record, "Name");
  if (!name) return null;

  return {
    id: readString(record, "Id"),
    name,
    category: readString(record, "Category"),
    description: readString(record, "Description"),
    websiteUrl: readString(record, "WebsiteUrl"),
    logoUrl: readString(record, "LogoUrl"),
    region: readString(record, "Region"),
    partnerSinceYear: readNumber(record, "PartnerSinceYear"),
    sellerName: readString(record, "SellerName"),
    activeListingCount: readNumber(record, "ActiveListingCount") ?? 0,
  };
}

export function fetchPartners(): Promise<Partner[]> {
  return cached(PARTNERS_CACHE_KEY, TTL.partners, async () => {
    try {
      const response = await fetch(new URL("/api/partners", getServerApiBaseUrl()), {
        headers: { Accept: "application/json" },
      });
      if (!response.ok) throw new Error(`API ${response.status}`);

      const payload = (await response.json()) as unknown;
      const partners = isRecord(payload) ? getValue(payload, "Partners") : null;
      if (!Array.isArray(partners)) return [];

      return partners
        .map((entry) => (isRecord(entry) ? normalizePartner(entry) : null))
        .filter((partner): partner is Partner => Boolean(partner));
    } catch (error) {
      console.warn("[Heimatplatz] Partners could not be loaded", error);
      return [];
    }
  });
}
