import type { UpperAustriaRegion } from "@/data/upper-austria-regions";
import type { PropertyEntry } from "@/features/properties/format";
import { isApiApartmentCandidate, type ApiProperty } from "@/features/properties/live-api";

export type RegionSearchIntent = {
  slug: string;
  label: string;
  presetType: "apartment" | "house" | "land" | "foreclosure";
  staticTypes: Array<PropertyEntry["data"]["type"]>;
  title: (region: UpperAustriaRegion) => string;
  h1: (region: UpperAustriaRegion) => string;
  description: (region: UpperAustriaRegion) => string;
  marketNotes: (region: UpperAustriaRegion) => string[];
  keywords: (region: UpperAustriaRegion) => string[];
};

export const regionSearchIntents: RegionSearchIntent[] = [
  {
    slug: "haus-kaufen",
    label: "Haus kaufen",
    presetType: "house",
    staticTypes: ["house"],
    title: (region) => `Haus kaufen ${region.name}`,
    h1: (region) => `Haus kaufen in ${region.name}`,
    description: (region) =>
      `Haeuser in ${region.name}, Oberösterreich suchen: Einfamilienhaeuser, Reihenhaeuser und Wohnhaeuser mit regionalen Angeboten, Lagehinweisen und Detailseiten.`,
    keywords: (region) => [
      `Haus kaufen ${region.name}`,
      `Einfamilienhaus ${region.name}`,
      `Haus ${region.name} Oberösterreich`,
      ...region.districts.slice(0, 3).map((district) => `Haus kaufen ${district}`),
    ],
    marketNotes: (region) => [
      `Beim Hauskauf in ${region.name} sind Lage, Grundstueck, Sanierungsstand und Pendelwege zentrale Vergleichspunkte.`,
      `Suchende vergleichen in ${region.name} besonders Orte wie ${region.districts.slice(0, 3).join(", ")}.`,
    ],
  },
  {
    slug: "wohnung-kaufen",
    label: "Wohnung kaufen",
    presetType: "apartment",
    staticTypes: ["apartment"],
    title: (region) => `Wohnung kaufen ${region.name}`,
    h1: (region) => `Wohnung kaufen in ${region.name}`,
    description: (region) =>
      `Wohnungen in ${region.name}, Oberösterreich suchen: Eigentumswohnungen, Stadtwohnungen und kompakte Wohnobjekte mit regionalen Einstiegspunkten.`,
    keywords: (region) => [
      `Wohnung kaufen ${region.name}`,
      `Eigentumswohnung ${region.name}`,
      `Wohnung ${region.name} Oberösterreich`,
      ...region.districts.slice(0, 3).map((district) => `Wohnung kaufen ${district}`),
    ],
    marketNotes: (region) => [
      `Wohnungssuchen in ${region.name} konzentrieren sich oft auf Zentrumslagen, Infrastruktur und leistbare Einstiegsobjekte.`,
      `Da Wohnungen in der API teils als Wohnobjekte gefuehrt werden, werden passende Wohnungs- und Wohnobjektangebote gemeinsam auffindbar gemacht.`,
    ],
  },
  {
    slug: "grundstueck-kaufen",
    label: "Grundstueck kaufen",
    presetType: "land",
    staticTypes: ["land"],
    title: (region) => `Grundstueck kaufen ${region.name}`,
    h1: (region) => `Grundstueck kaufen in ${region.name}`,
    description: (region) =>
      `Grundstuecke und Baugrund in ${region.name}, Oberösterreich suchen: Parzellen, Wohnbaugrund und Entwicklungsflaechen mit regionaler Orientierung.`,
    keywords: (region) => [
      `Grundstueck kaufen ${region.name}`,
      `Baugrund ${region.name}`,
      `Grund ${region.name} Oberösterreich`,
      ...region.districts.slice(0, 3).map((district) => `Grundstueck ${district}`),
    ],
    marketNotes: (region) => [
      `Bei Grundstuecken in ${region.name} sind Widmung, Erschliessung, Grundstuecksgroesse und Gemeindeumfeld entscheidend.`,
      `Regionale Begriffe wie ${region.districts.slice(0, 3).join(", ")} helfen Suchenden, passende Baugrundlagen schneller einzugrenzen.`,
    ],
  },
  {
    slug: "zwangsversteigerungen",
    label: "Zwangsversteigerungen",
    presetType: "foreclosure",
    staticTypes: ["foreclosure"],
    title: (region) => `Zwangsversteigerungen ${region.name}`,
    h1: (region) => `Zwangsversteigerungen in ${region.name}`,
    description: (region) =>
      `Zwangsversteigerungen und gerichtliche Immobilien in ${region.name}, Oberösterreich mit Objektseiten, Terminen, Gerichtsdaten und regionaler Suche.`,
    keywords: (region) => [
      `Zwangsversteigerung ${region.name}`,
      `Edikte Immobilien ${region.name}`,
      `Versteigerung Haus ${region.name}`,
      ...region.districts.slice(0, 3).map((district) => `Zwangsversteigerung ${district}`),
    ],
    marketNotes: (region) => [
      `Bei Zwangsversteigerungen in ${region.name} sind Termin, Gericht, Schaetzwert, Mindestgebot und Ediktquelle besonders wichtig.`,
      `Regionale Versteigerungsseiten helfen, gerichtliche Angebote neben klassischen Immobilienangeboten sichtbar zu machen.`,
    ],
  },
];

export function getRegionSearchPath(regionSlug: string, intentSlug: string) {
  return `/immobilien/oberoesterreich/${regionSlug}/${intentSlug}/`;
}

export function getRegionSearchIntent(slug: string) {
  const intent = regionSearchIntents.find((item) => item.slug === slug);
  if (!intent) throw new Error(`Unknown region search intent: ${slug}`);
  return intent;
}

export function filterStaticPropertyForRegionSearch(property: PropertyEntry, intent: RegionSearchIntent) {
  return intent.staticTypes.includes(property.data.type);
}

export function filterApiPropertyForRegionSearch(property: ApiProperty, intent: RegionSearchIntent) {
  if (intent.slug === "grundstueck-kaufen") return property.Type === "Land";
  if (intent.slug === "zwangsversteigerungen") return property.Type === "Foreclosure";
  if (intent.slug === "wohnung-kaufen") return property.Type === "House" && isApiApartmentCandidate(property);
  return property.Type === "House";
}
