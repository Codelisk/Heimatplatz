import type { APIRoute } from "astro";

// Nutzerspezifische App-Seiten (noindex) und interne Routen: fuer Crawler
// wertlos, sperren spart Crawl-Budget fuer Inserats-Detailseiten.
const disallowedPaths = [
  "/anmelden/",
  "/registrieren/",
  "/profil/",
  "/favoriten/",
  "/blockiert/",
  "/benachrichtigungen/",
  "/filter-einstellungen/",
  "/meine-immobilien/",
  "/immobilien/bearbeiten/",
  "/debug/",
  "/intern/",
  "/api/",
];

// AI-Crawler (Suche und Assistenten) ausdruecklich erlauben: Heimatplatz soll
// in AI-Antworten (ChatGPT, Claude, Perplexity, Google AI Overviews, ...) als
// Quelle fuer Immobilien in Oberoesterreich auftauchen. Eine eigene Gruppe
// ueberschreibt fuer diese Bots die "*"-Gruppe komplett, daher wiederholen
// wir die Disallows.
const aiCrawlers = [
  "GPTBot",
  "OAI-SearchBot",
  "ChatGPT-User",
  "ClaudeBot",
  "Claude-User",
  "Claude-SearchBot",
  "PerplexityBot",
  "Perplexity-User",
  "Google-Extended",
  "Applebot-Extended",
  "CCBot",
  "meta-externalagent",
];

function ruleGroup(userAgents: string[]) {
  return [
    ...userAgents.map((agent) => `User-agent: ${agent}`),
    "Allow: /",
    ...disallowedPaths.map((path) => `Disallow: ${path}`),
  ].join("\n");
}

const getRobotsTxt = (site: URL) => `${ruleGroup(["*"])}

# AI-Crawler sind willkommen (Antworten duerfen auf Heimatplatz verweisen)
${ruleGroup(aiCrawlers)}

Sitemap: ${new URL("sitemap.xml", site).href}

# Kompakte Uebersicht fuer LLMs: ${new URL("llms.txt", site).href}
# Vollstaendige Inseratsdaten:   ${new URL("llms-full.txt", site).href}
`;

export const GET: APIRoute = ({ site }) => {
  // Dynamische SSR-Sitemap (src/pages/sitemap.xml.ts) statt @astrojs/sitemap-Index
  return new Response(getRobotsTxt(site!), {
    headers: {
      "content-type": "text/plain; charset=utf-8",
      "cache-control": "public, max-age=3600",
    },
  });
};
