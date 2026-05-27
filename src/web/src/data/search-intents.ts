import type { PropertyEntry } from "@/features/properties/format";
import type { PropertyType } from "@/features/properties/types";

export type SearchIntent = {
  slug: string;
  title: string;
  h1: string;
  description: string;
  canonicalPath: string;
  presetType?: "apartment" | "house" | "land" | "foreclosure" | "";
  presetSeller?: "private" | "agent" | "court" | "";
  staticTypes?: PropertyType[];
  staticSellerTypes?: Array<PropertyEntry["data"]["sellerType"]>;
  keywords: string[];
  marketNotes: string[];
  relatedRegionSlugs: string[];
};

export const searchIntents: SearchIntent[] = [
  {
    slug: "haus-kaufen-oberoesterreich",
    title: "Haus kaufen Oberösterreich",
    h1: "Haus kaufen in Oberösterreich",
    description:
      "Haeuser in Oberösterreich suchen: Einfamilienhaus, Reihenhaus und Wohnhaus in Linz, Wels, Steyr, Linz-Land, Salzkammergut und Innviertel.",
    canonicalPath: "/immobilien/haus-kaufen-oberoesterreich/",
    presetType: "house",
    staticTypes: ["house"],
    keywords: ["Haus kaufen Oberösterreich", "Einfamilienhaus Oberösterreich", "Reihenhaus Wels", "Haus Linz-Land"],
    marketNotes: [
      "Haus-Suchen in Oberösterreich sind stark regional: Zentralraum, Salzkammergut, Innviertel und Muehlviertel brauchen eigene Einstiegspunkte.",
      "Käufer vergleichen Lage, Grundstück, Sanierungszustand, Pendelzeit und laufende Kosten besonders genau.",
    ],
    relatedRegionSlugs: ["linz", "linz-land", "wels", "wels-land", "voecklabruck", "gmunden"],
  },
  {
    slug: "wohnung-kaufen-oberoesterreich",
    title: "Wohnung kaufen Oberösterreich",
    h1: "Wohnung kaufen in Oberösterreich",
    description:
      "Wohnungen in Oberösterreich finden: Eigentumswohnungen, Stadtwohnungen und kompakte Wohnobjekte in Linz, Wels, Steyr, Voecklabruck und weiteren Bezirken.",
    canonicalPath: "/immobilien/wohnung-kaufen-oberoesterreich/",
    presetType: "apartment",
    staticTypes: ["apartment"],
    keywords: ["Wohnung kaufen Oberösterreich", "Eigentumswohnung Linz", "Wohnung Wels", "Wohnung Steyr"],
    marketNotes: [
      "Wohnungssuchen konzentrieren sich besonders auf Staedte, Pendlerlagen und leistbare Einstiegsobjekte.",
      "Da das Backend Wohnungen aktuell in der Wohnobjekt-Kategorie fuehrt, bleibt die Seite suchmaschinenfreundlich und erweitert Live-Treffer aus dieser Kategorie.",
    ],
    relatedRegionSlugs: ["linz", "wels", "steyr", "voecklabruck", "gmunden", "perg"],
  },
  {
    slug: "grundstueck-kaufen-oberoesterreich",
    title: "Grundstück kaufen Oberösterreich",
    h1: "Grundstueck kaufen in Oberösterreich",
    description:
      "Grundstuecke und Baugrund in Oberösterreich suchen: Parzellen, Wohnbaugrund und Entwicklungsflaechen in Gmunden, Linz-Land, Perg, Wels-Land und weiteren Bezirken.",
    canonicalPath: "/immobilien/grundstueck-kaufen-oberoesterreich/",
    presetType: "land",
    staticTypes: ["land"],
    keywords: ["Grundstueck kaufen Oberösterreich", "Baugrund Oberösterreich", "Grund Gmunden", "Grundstueck Linz-Land"],
    marketNotes: [
      "Bei Grundstuecken sind Gemeinde, Widmung, Erschliessung und Pendlerlage besonders wichtig.",
      "Viele Suchende vergleichen Baugrund im Zentralraum mit ruhigeren Lagen im Muehlviertel, Innviertel und Salzkammergut.",
    ],
    relatedRegionSlugs: ["gmunden", "linz-land", "wels-land", "perg", "grieskirchen", "rohrbach"],
  },
  {
    slug: "immobilien-privat-oberoesterreich",
    title: "Immobilien privat Oberösterreich",
    h1: "Immobilien von privat in Oberösterreich",
    description:
      "Private Immobilienangebote in Oberösterreich suchen: Haeuser, Wohnungen und Grundstuecke von privaten Anbietern in Linz, Wels, Steyr und den Bezirken.",
    canonicalPath: "/immobilien/immobilien-privat-oberoesterreich/",
    presetSeller: "private",
    staticSellerTypes: ["private"],
    keywords: ["Immobilien privat Oberösterreich", "Haus privat kaufen Oberösterreich", "Wohnung privat Linz", "Privatanbieter Immobilien"],
    marketNotes: [
      "Privatangebote sind fuer Kaeufer attraktiv, wenn Preis, Kontaktweg und Objektzustand klar beschrieben sind.",
      "Besonders in Linz, Wels, Steyr und den Umlandgemeinden werden private Angebote direkt mit Maklerangeboten verglichen.",
    ],
    relatedRegionSlugs: ["linz", "wels", "steyr", "linz-land", "voecklabruck", "ried-im-innkreis"],
  },
  {
    slug: "immobilien-makler-oberoesterreich",
    title: "Makler Immobilien Oberösterreich",
    h1: "Immobilien von Maklern in Oberösterreich",
    description:
      "Maklerangebote in Oberösterreich suchen: Immobilien von gewerblichen Anbietern, Agenturen und Portalen in den wichtigsten Regionen und Bezirken.",
    canonicalPath: "/immobilien/immobilien-makler-oberoesterreich/",
    presetSeller: "agent",
    staticSellerTypes: ["agent"],
    keywords: ["Makler Immobilien Oberösterreich", "Immobilienmakler Linz", "Maklerangebote Wels", "Immobilien Agentur Oberösterreich"],
    marketNotes: [
      "Makler- und Agenturangebote decken viele Neubau-, Bestands- und Anlageobjekte in Oberösterreich ab.",
      "Für Käufer sind klare Anbieterangaben, Lage, Preis, Fläche und Kontaktmöglichkeit entscheidend.",
    ],
    relatedRegionSlugs: ["linz", "wels", "steyr", "gmunden", "linz-land", "wels-land"],
  },
];

export function getSearchIntent(slug: string) {
  const intent = searchIntents.find((item) => item.slug === slug);
  if (!intent) throw new Error(`Unknown search intent: ${slug}`);
  return intent;
}

export function filterPropertiesForSearchIntent(property: PropertyEntry, intent: SearchIntent) {
  const typeMatches = !intent.staticTypes || intent.staticTypes.includes(property.data.type);
  const sellerMatches = !intent.staticSellerTypes || intent.staticSellerTypes.includes(property.data.sellerType);
  return typeMatches && sellerMatches;
}
