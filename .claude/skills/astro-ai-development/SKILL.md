---
name: astro-ai-development
description: Build and modify the Heimatplatz Astro web app in src/web using Astro 6, Tailwind 4, Starwind UI, content collections, SEO metadata, sitemap/robots/llms, and backend-first API integration. Use when working on .astro files, Astro routes/layouts, content.config.ts, Starwind components, Tailwind styles, SEO/AEO, or AI-assisted Astro development workflow.
---

# Astro AI Development

## Workflow

1. Work from `src/web` for the Astro app; keep `src/api` as the business-logic source and `src/maui` for mobile/desktop concerns.
2. Check current Astro APIs before making framework-level changes. Prefer the Astro Docs MCP server; if unavailable, use official Astro docs.
3. The app runs fully server-side rendered (`output: 'server'` + `@astrojs/node`, seit 15.7.2026): pages render per request, data comes from the API at request time (TTL-cached via `src/lib/server/ttl-cache.ts`). Server-side fetches use `getServerApiBaseUrl()` (`src/lib/server/api-base.ts`, runtime override `API_BASE_URL_SERVER`); client scripts keep using `PUBLIC_API_BASE_URL`. Do not add `getStaticPaths` to new dynamic routes; handle missing data with a 404 `Response` and keep the dynamic sitemap (`src/pages/sitemap.xml.ts`) in sync when adding indexable routes.
4. Build SEO at the layout/route layer: title, description, canonical URL, Open Graph, JSON-LD, `robots.txt`, `llms.txt`, and sitemap must stay coherent.
5. Use Starwind UI components from `@/components/starwind/*` for common controls, then compose domain components under `src/components` or `src/features`.
6. Keep data-loading decisions explicit: build-time content collections for indexable content, API client calls for live app workflows.

## Project Conventions

- Keep shared site metadata in `src/config/site.ts`.
- Put global page chrome and metadata in `src/layouts/BaseLayout.astro`.
- Put typed API access in `src/lib/api`.
- Put domain slices in `src/features/{feature}`.
- Put route files in `src/pages` and let Astro file-based routing expose URLs.
- Avoid client JavaScript unless interactivity is required; prefer native forms and server-rendered HTML for crawlable content.

## AI Context

Read `references/astro-current-stack.md` when you need source links, MCP setup, Starwind AI references, or package/version context. Do not paste large external docs into prompts; use MCP/search narrowly.

## Verification

Run these from `src/web` after meaningful changes:

```sh
npm run check
npm run build
```

For UI changes, also run the dev server and inspect desktop and mobile viewports before considering the work done.
