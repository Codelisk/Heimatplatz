import type { UpperAustriaRegion } from "@/data/upper-austria-regions";
import type { ApiProperty } from "@/features/properties/live-api";

export function slugifyLocation(value: string) {
  return value
    .replace(/ä/g, "ae")
    .replace(/Ä/g, "ae")
    .replace(/ö/g, "oe")
    .replace(/Ö/g, "oe")
    .replace(/ü/g, "ue")
    .replace(/Ü/g, "ue")
    .replace(/ß/g, "ss")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

export function getApiPropertyRegionSlug(
  property: ApiProperty,
  municipalityRegionSlugs: Record<string, string>,
  region: UpperAustriaRegion,
) {
  const municipalityRegion = property.MunicipalityId
    ? municipalityRegionSlugs[property.MunicipalityId.toLowerCase()]
    : "";
  if (municipalityRegion) return municipalityRegion;

  const city = slugifyLocation(property.City);
  if (city === region.slug || city === slugifyLocation(region.name)) return region.slug;
  if (region.districts.some((district) => slugifyLocation(district) === city)) return region.slug;
  return "";
}
