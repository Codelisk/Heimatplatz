import type { APIRoute } from "astro";
import { SITE } from "@/config/site";
import { guidePages } from "@/data/guide-pages";
import { getRegionSearchPath, regionSearchIntents } from "@/data/region-search-intents";
import { searchIntents } from "@/data/search-intents";
import { upperAustriaRegions } from "@/data/upper-austria-regions";

const primaryRoutes = [
  "/",
  "/immobilien/",
  "/immobilien/angebote/",
  "/immobilien/oberoesterreich/",
  "/zwangsversteigerungen/",
  "/ratgeber/",
  "/datenschutz/",
  "/impressum/",
];

const searchIntentRoutes = searchIntents.map((intent) => intent.canonicalPath);
const guideRoutes = guidePages.map((guide) => guide.canonicalPath);
const regionRoutes = upperAustriaRegions.map((region) => `/immobilien/oberoesterreich/${region.slug}/`);
const regionIntentRoutes = upperAustriaRegions.flatMap((region) =>
  regionSearchIntents.map((intent) => getRegionSearchPath(region.slug, intent.slug)),
);

function routeList(routes: string[]) {
  return routes.map((route) => `- ${new URL(route, SITE.url).toString()}`).join("\n");
}

const body = `# ${SITE.name}

> Suchmaschinenfreundliche Immobilienplattform fuer Oberösterreich: Haeuser, Wohnungen, Grundstuecke, private Angebote, Maklerangebote und gerichtliche Zwangsversteigerungen.

## Wichtige Oeffentliche Seiten

${routeList(primaryRoutes)}

## Suchintentionen

${routeList(searchIntentRoutes)}

## Ratgeber

${routeList(guideRoutes)}

## Regionen In Oberösterreich

${routeList(regionRoutes)}

## Regionale Suchintentionen

${routeList(regionIntentRoutes)}

## Inhaltlicher Fokus

- Immobilien kaufen in Oberösterreich, Linz, Wels, Steyr und allen Bezirken.
- Aktuelle Angebote mit statischen Detailseiten fuer bessere Auffindbarkeit.
- Zwangsversteigerungen mit Termin, Gericht, Schaetzwert, Mindestgebot und Quellenlink.
- Regionale Landingpages mit lokalen Suchbegriffen und internen Links.
- Ratgeberartikel mit FAQ-Struktur zu Hauskauf, Wohnungskauf, Baugrund, Privatverkauf und Zwangsversteigerungen.
- Rechtliche Anbieterinformationen: Datenschutz und Impressum.
`;

export const GET: APIRoute = () =>
  new Response(body, {
    headers: {
      "content-type": "text/plain; charset=utf-8",
    },
  });
