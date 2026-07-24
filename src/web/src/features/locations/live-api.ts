import { getServerApiBaseUrl } from "@/lib/server/api-base";
import { cached, TTL } from "@/lib/server/ttl-cache";
import { slugifyLocation } from "./region-match";
import type { OrtSlugMap } from "@/features/properties/search-query";

/**
 * Bezirk/Gemeinde-Hierarchie von GET /api/locations fuer den OrtPicker der
 * Immobilien-Suche. Die Slugs identifizieren die Auswahl in URL/localStorage,
 * die MunicipalityIds gehen als MunicipalityIdsJson an die Properties-API -
 * damit filtert der Server (Backend-First), nicht mehr der Client ueber DOM-Karten.
 */

export type OrtLocality = {
  name: string;
  slug: string;
  municipalityIds: string[];
};

export type OrtRegion = {
  name: string;
  slug: string;
  localities: OrtLocality[];
};

type ApiMunicipality = { Id: string; Name: string };
type ApiDistrict = { Name: string; Municipalities?: ApiMunicipality[] | null };
type ApiFederalProvince = { Name: string; Districts?: ApiDistrict[] | null };
type ApiLocationsResponse = { FederalProvinces?: ApiFederalProvince[] | null };

async function fetchOrtRegionsUncached(): Promise<OrtRegion[]> {
  const response = await fetch(new URL("/api/locations", getServerApiBaseUrl()));
  if (!response.ok) throw new Error(`API ${response.status}`);
  const payload = await response.json() as ApiLocationsResponse;

  const districts = (payload.FederalProvinces ?? []).flatMap((province) => province.Districts ?? []);
  const usedLocalitySlugs = new Set<string>();

  const regions = districts.map((district) => {
    const regionSlug = slugifyLocation(district.Name);
    const localities = (district.Municipalities ?? [])
      .map((municipality) => {
        // Gemeindenamen sind in OOe eindeutig - nur bei Kollision Bezirks-Praefix
        const baseSlug = slugifyLocation(municipality.Name);
        const slug = usedLocalitySlugs.has(baseSlug) ? `${regionSlug}-${baseSlug}` : baseSlug;
        usedLocalitySlugs.add(slug);
        return { name: municipality.Name, slug, municipalityIds: [municipality.Id] };
      })
      .sort((a, b) => a.name.localeCompare(b.name, "de-AT"));
    return { name: district.Name, slug: regionSlug, localities };
  });

  const result = regions
    .filter((region) => region.localities.length > 0)
    .sort((a, b) => a.name.localeCompare(b.name, "de-AT"));

  // Leere Liste als Fehler behandeln: der ttl-cache verwirft nur rejected
  // Promises - ein gecachtes [] wuerde den Ortsfilter bis zu TTL.locations
  // (1h) leer lassen, obwohl die API laengst wieder antwortet.
  if (result.length === 0) throw new Error("API locations returned no regions");

  return result;
}

export async function fetchOrtRegions(): Promise<OrtRegion[]> {
  try {
    // Fehler erst NACH dem Cache-Layer fangen: das rejected Promise fliegt aus
    // dem Cache, der naechste Request laedt neu - Aufrufer sehen weiterhin [].
    return await cached("ort-regions", TTL.locations, fetchOrtRegionsUncached);
  } catch (error) {
    console.warn("[Heimatplatz] API locations could not be loaded", error);
    return [];
  }
}

/**
 * Slug -> MunicipalityIds fuer Suche/SSR: Gemeinde-Slugs zeigen auf ihre eine
 * Gemeinde, Bezirks-Slugs (Region-Checkbox bzw. "(alle)"-Option) auf alle
 * Gemeinden des Bezirks.
 */
export function buildOrtSlugMap(regions: OrtRegion[]): OrtSlugMap {
  const map: OrtSlugMap = {};
  for (const region of regions) {
    const regionIds: string[] = [];
    for (const locality of region.localities) {
      map[locality.slug] = locality.municipalityIds;
      regionIds.push(...locality.municipalityIds);
    }
    map[region.slug] = regionIds;
  }
  return map;
}
