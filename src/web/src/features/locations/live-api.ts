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
  try {
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

    return regions
      .filter((region) => region.localities.length > 0)
      .sort((a, b) => a.name.localeCompare(b.name, "de-AT"));
  } catch (error) {
    console.warn("[Heimatplatz] API locations could not be loaded", error);
    return [];
  }
}

export function fetchOrtRegions() {
  return cached("ort-regions", TTL.locations, fetchOrtRegionsUncached);
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
