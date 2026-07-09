import type { APIRoute } from "astro";
import { SITE } from "@/config/site";

const routes = [
  "/",
  "/zwangsversteigerungen/",
  "/datenschutz/",
  "/impressum/",
];

const body = `# ${SITE.name}

> Immobilien-App für Oberösterreich: Häuser, Wohnungen, Grundstücke und gerichtliche Zwangsversteigerungen mit Filtern, Favoriten und Push-Benachrichtigungen.

## Öffentliche Seiten

${routes.map((route) => `- ${new URL(route, SITE.url).toString()}`).join("\n")}

## Inhaltlicher Fokus

- Immobilien suchen und filtern (Bezirk, Typ, Anbieter, Zeitraum, Sortierung).
- Zwangsversteigerungen mit Termin, Gericht, Schätzwert und Mindestgebot.
- Rechtliche Anbieterinformationen: Datenschutz und Impressum.
`;

export const GET: APIRoute = () =>
  new Response(body, {
    headers: {
      "content-type": "text/plain; charset=utf-8",
    },
  });
