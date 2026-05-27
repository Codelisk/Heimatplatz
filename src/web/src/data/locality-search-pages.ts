import { upperAustriaRegions, type UpperAustriaRegion } from "@/data/upper-austria-regions";
import { slugifyLocation } from "@/features/locations/region-match";

export type LocalitySearchPage = {
  name: string;
  slug: string;
  baseSlug: string;
  regionName: string;
  regionSlug: string;
  title: string;
  h1: string;
  description: string;
  keywords: string[];
  isRegionCenter: boolean;
};

type LocalitySeed = {
  name: string;
  region: UpperAustriaRegion;
  isRegionCenter: boolean;
};

const nonLocalityRegionSlugs = new Set([
  "linz-land",
  "steyr-land",
  "urfahr-umgebung",
  "wels-land",
]);

const seeds = upperAustriaRegions.flatMap((region) => {
  const includeRegionCenter = !nonLocalityRegionSlugs.has(region.slug);
  const names = [...(includeRegionCenter ? [region.name] : []), ...region.districts];
  const seenNames = new Set<string>();

  return names
    .map((name, index) => ({ name, region, isRegionCenter: includeRegionCenter && index === 0 }))
    .filter((seed) => {
      const key = slugifyLocation(seed.name);
      if (!key || seenNames.has(key)) return false;
      seenNames.add(key);
      return true;
    });
});

const baseSlugCounts = seeds.reduce<Record<string, number>>((counts, seed) => {
  const baseSlug = slugifyLocation(seed.name);
  counts[baseSlug] = (counts[baseSlug] ?? 0) + 1;
  return counts;
}, {});

export const localitySearchPages: LocalitySearchPage[] = seeds
  .map((seed: LocalitySeed) => {
    const baseSlug = slugifyLocation(seed.name);
    const slug = baseSlugCounts[baseSlug] > 1 ? `${seed.region.slug}-${baseSlug}` : baseSlug;
    const localityScope = seed.name === seed.region.name ? seed.name : `${seed.name}, ${seed.region.name}`;

    return {
      name: seed.name,
      slug,
      baseSlug,
      regionName: seed.region.name,
      regionSlug: seed.region.slug,
      isRegionCenter: seed.isRegionCenter,
      title: `Immobilien ${seed.name} Oberösterreich`,
      h1: `Immobilien in ${seed.name}`,
      description:
        `Immobilien in ${localityScope}: Haeuser, Wohnungen, Grundstuecke und Zwangsversteigerungen mit regionaler Suche fuer Oberösterreich.`,
      keywords: [
        `Immobilien ${seed.name}`,
        `Haus kaufen ${seed.name}`,
        `Wohnung kaufen ${seed.name}`,
        `Grundstueck ${seed.name}`,
        seed.name === seed.region.name
          ? `${seed.name} Immobilien Oberösterreich`
          : `${seed.name} ${seed.region.name} Immobilien`,
      ],
    };
  })
  .sort((a, b) => a.name.localeCompare(b.name, "de-AT"));

export function getLocalityPath(slug: string) {
  return `/immobilien/orte/${slug}/`;
}

export function getRegionLocalities(regionSlug: string, limit?: number) {
  const localities = localitySearchPages.filter((locality) => locality.regionSlug === regionSlug);
  return typeof limit === "number" ? localities.slice(0, limit) : localities;
}

export function getRelatedLocalities(locality: LocalitySearchPage, limit = 8) {
  return localitySearchPages
    .filter((item) => item.regionSlug === locality.regionSlug && item.slug !== locality.slug)
    .slice(0, limit);
}
