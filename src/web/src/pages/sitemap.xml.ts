import type { APIRoute } from "astro";
import {
  fetchForeclosureAuctions,
  getForeclosureAuctionPath,
} from "@/features/foreclosures/api";
import {
  API_PROPERTY_LIST_LIMIT,
  fetchApiProperties,
  getApiPropertyPath,
} from "@/features/properties/live-api";

/**
 * Dynamische Sitemap (SSR-Ersatz fuer @astrojs/sitemap, das nur vorgerenderte
 * Seiten auflisten kann): indexierbare statische Routen plus die aktuellen
 * Immobilien- und Zwangsversteigerungs-Detailseiten direkt aus der API.
 * App-Seiten (Anmelden, Favoriten, Profil, Debug usw.) sind noindex und
 * gehoeren nicht in die Sitemap.
 */
const staticRoutes = ["/", "/inserieren/", "/datenschutz/", "/impressum/"];

function urlEntry(loc: string, lastmod?: string) {
  const lastmodTag = lastmod ? `<lastmod>${lastmod}</lastmod>` : "";
  return `<url><loc>${loc}</loc>${lastmodTag}<changefreq>daily</changefreq><priority>0.7</priority></url>`;
}

function toLastmod(value: string | null | undefined) {
  if (!value) return undefined;
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? undefined : date.toISOString();
}

export const GET: APIRoute = async ({ site }) => {
  const [properties, auctions] = await Promise.all([
    fetchApiProperties({ pageSize: API_PROPERTY_LIST_LIMIT }),
    fetchForeclosureAuctions(),
  ]);

  const entries = [
    ...staticRoutes.map((route) => urlEntry(new URL(route, site).toString())),
    ...properties.map((property) =>
      urlEntry(new URL(getApiPropertyPath(property), site).toString(), toLastmod(property.CreatedAt))),
    ...auctions.map((auction) =>
      urlEntry(new URL(getForeclosureAuctionPath(auction), site).toString(), toLastmod(auction.CreatedAt))),
  ];

  const body = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${entries.join("\n")}
</urlset>
`;

  return new Response(body, {
    headers: {
      "content-type": "application/xml; charset=utf-8",
      "cache-control": "public, max-age=3600",
    },
  });
};
