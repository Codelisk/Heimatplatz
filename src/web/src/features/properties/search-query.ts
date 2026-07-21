/**
 * Gemeinsamer Query-Builder fuer die Immobilien-Suche: SSR (index.astro) und
 * Client (PropertySearchApp) bauen daraus IDENTISCHE Query-Strings fuer
 * GET /api/properties - der Client vergleicht seinen Initial-Query mit dem
 * SSR-Query und spart sich den Refetch, wenn beide uebereinstimmen.
 *
 * Client-sicher: keine Server-Imports (ttl-cache, api-base).
 */

export const SEARCH_PAGE_SIZE = 24;

export const ALL_TYPES = ["house", "land", "foreclosure"] as const;
/**
 * Standard-Typauswahl ohne URL-Parameter und ohne gespeicherte Praeferenzen:
 * Zwangsversteigerungen sind standardmaessig deaktiviert und muessen aktiv
 * dazugewaehlt werden (dann steht "foreclosure" explizit in URL/Praeferenzen).
 */
export const DEFAULT_TYPES = ["house", "land"] as const;
export const ALL_SELLERS = ["private", "agent"] as const;

export type OrtSlugMap = Record<string, string[]>;

export type PropertySearchState = {
  q: string;
  /** Teilmenge von ALL_TYPES; leer oder vollstaendig = kein Filter */
  types: string[];
  /** Teilmenge von ALL_SELLERS; leer oder vollstaendig = kein Filter */
  sellers: string[];
  /** OrtPicker-Slugs (Gemeinde oder Bezirk), Aufloesung via OrtSlugMap */
  ortSlugs: string[];
  /** "", "day", "week", "month", "year" */
  age: string;
  /** Sort-Modus des UI-Selects (newest, price-asc, ...) */
  sort: string;
  page: number;
};

const SORT_MAP: Record<string, [string, boolean]> = {
  newest: ["CreatedAt", true],
  oldest: ["CreatedAt", false],
  "price-asc": ["Price", false],
  "price-desc": ["Price", true],
  "area-desc": ["PlotArea", true],
  "area-asc": ["PlotArea", false],
  "postal-asc": ["PostalCode", false],
  "postal-desc": ["PostalCode", true],
};

/** Alte URLs/Preferences kennen noch "apartment" - die API nur House/Land/Foreclosure. */
export function normalizeTypeValue(value: string) {
  return value === "apartment" ? "house" : value;
}

export function resolveOrtSlugs(slugs: string[], map: OrtSlugMap): string[] {
  const ids = new Set<string>();
  const knownSlugs = Object.keys(map);
  for (const slug of slugs) {
    if (!slug) continue;
    let matches = map[slug];
    if (!matches) {
      // Legacy-Slugs (alte SEO-Slugs wie "linz-urfahr", gespeicherte Ortsnamen):
      // per Praefix/Suffix auf einen bekannten Slug abbilden, sonst ignorieren.
      const fallback = knownSlugs.find(
        (known) => slug.endsWith(`-${known}`) || known.endsWith(`-${slug}`) || slug.startsWith(`${known}-`),
      );
      if (fallback) matches = map[fallback];
    }
    matches?.forEach((id) => ids.add(id));
  }
  return Array.from(ids);
}

function getCreatedAfter(age: string, now: number): string | null {
  const DAY = 24 * 60 * 60 * 1000;
  const spans: Record<string, number> = { day: DAY, week: 7 * DAY, month: 30 * DAY, year: 365 * DAY };
  const span = spans[age];
  if (!span) return null;
  // Auf volle Stunde runden: stabile SSR-Cache-Keys und identische
  // Query-Strings zwischen SSR-Render und Client-Init.
  const HOUR = 60 * 60 * 1000;
  const cutoff = Math.floor((now - span) / HOUR) * HOUR;
  return new Date(cutoff).toISOString();
}

export function buildPropertySearchQuery(state: PropertySearchState, ortSlugMap: OrtSlugMap): string {
  const params = new URLSearchParams();
  params.set("Page", String(Math.max(0, Math.floor(state.page) || 0)));
  params.set("PageSize", String(SEARCH_PAGE_SIZE));

  const [sortBy, sortDescending] = SORT_MAP[state.sort] ?? SORT_MAP.newest;
  params.set("SortBy", sortBy);
  params.set("SortDescending", String(sortDescending));

  const searchText = state.q.trim();
  if (searchText) params.set("SearchText", searchText);

  const types = [...new Set(state.types.map(normalizeTypeValue))].filter((type) =>
    (ALL_TYPES as readonly string[]).includes(type));
  if (types.length > 0 && types.length < ALL_TYPES.length) {
    const apiTypes = ALL_TYPES
      .filter((type) => types.includes(type))
      .map((type) => (type === "house" ? "House" : type === "land" ? "Land" : "Foreclosure"));
    params.set("PropertyTypesJson", JSON.stringify(apiTypes));
  }

  const sellers = [...new Set(state.sellers)].filter((seller) =>
    (ALL_SELLERS as readonly string[]).includes(seller));
  if (sellers.length > 0 && sellers.length < ALL_SELLERS.length) {
    const apiSellers: string[] = [];
    if (sellers.includes("private")) apiSellers.push("Private");
    if (sellers.includes("agent")) apiSellers.push("Broker", "PropertyManager");
    params.set("SellerTypesJson", JSON.stringify(apiSellers));
  }

  const municipalityIds = resolveOrtSlugs(state.ortSlugs, ortSlugMap);
  if (municipalityIds.length > 0) {
    params.set("MunicipalityIdsJson", JSON.stringify(municipalityIds));
  }

  const createdAfter = getCreatedAfter(state.age, Date.now());
  if (createdAfter) params.set("CreatedAfter", createdAfter);

  return params.toString();
}
