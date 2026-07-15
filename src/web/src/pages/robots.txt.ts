import type { APIRoute } from "astro";

const getRobotsTxt = (sitemapUrl: URL) => `User-agent: *
Allow: /

Sitemap: ${sitemapUrl.href}
`;

export const GET: APIRoute = ({ site }) => {
  // Dynamische SSR-Sitemap (src/pages/sitemap.xml.ts) statt @astrojs/sitemap-Index
  const sitemapUrl = new URL("sitemap.xml", site);

  return new Response(getRobotsTxt(sitemapUrl), {
    headers: {
      "content-type": "text/plain; charset=utf-8",
    },
  });
};
