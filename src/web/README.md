# Heimatplatz Web

Astro-Web-App unter `src/web`, vorbereitet als spaeterer Ersatz fuer die Uno-WASM-Webauslieferung. Die mobilen Uno-Ziele bleiben separat in `src/uno`. Die Web-App soll funktional und visuell dem Uno-WASM-Flow folgen: Headerfilter, Kartenraster, Details, Auth, Favoriten, Blockieren, Inserieren und Nutzerbereiche.

## Stack

- Astro 6, static output
- Tailwind CSS 4 via `@tailwindcss/vite`
- Starwind UI fuer Astro-native Komponenten
- `@astrojs/sitemap`, dynamisches `robots.txt`, `llms.txt`, Canonicals, Open Graph, Breadcrumbs und JSON-LD
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
PUBLIC_API_BASE_URL=https://heimatplatz-api.azurewebsites.net
```
