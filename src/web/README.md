# Heimatplatz Web

Astro-Web-App unter `src/web`. Die mobilen/Desktop-Ziele leben separat in `src/maui` (.NET MAUI). Die Web-App soll funktional und visuell dem MAUI-Flow folgen: Headerfilter, Kartenraster, Details, Auth, Favoriten, Blockieren, Inserieren und Nutzerbereiche.

## Stack

- Astro 7, vollstaendig SSR (`output: 'server'` + `@astrojs/node` standalone; laeuft produktiv als Node-Container am Hetzner-Server, siehe `deploy/hetzner/README.md`)
- Tailwind CSS 4 via `@tailwindcss/vite`
- Starwind UI fuer Astro-native Komponenten
- Dynamische `sitemap.xml`, `robots.txt`, `llms.txt`, Canonicals, Open Graph, Breadcrumbs und JSON-LD
- Serverseitige API-Fetches mit kurzer TTL gecacht (`src/lib/server/ttl-cache.ts`); Basis-URL zur Laufzeit via `API_BASE_URL_SERVER` (`src/lib/server/api-base.ts`)
- Regionale SEO-Seiten fuer Oberösterreich und alle 18 Bezirke/Statutarstaedte
- Backend-first: API-Integration laeuft ueber `src/lib/api/client.ts` und browserseitige Flows fuer Auth, Favoriten, Blockieren, Inserieren und Praeferenzen

## Struktur

```text
src/web/
├── src/components/          # Layout, Feature-Komponenten, Starwind UI
├── src/config/site.ts       # zentrale Site-/SEO-Konfiguration
├── src/content.config.ts    # Content Collections
├── src/data/                # Demo-/Build-Time-Daten
├── src/features/            # fachliche Web-Feature-Slices
├── src/layouts/BaseLayout.astro
└── src/pages/               # file-based routes
```

## Commands

```sh
npm install
npm run dev
npm run check
npm run build
npm run validate
```

## Environment

Kopiere die benoetigten Werte aus `.env.example`, wenn lokal andere URLs verwendet werden:

```sh
PUBLIC_SITE_URL=https://heimatplatz.at
PUBLIC_API_BASE_URL=https://api.heimatplatz.at
```
