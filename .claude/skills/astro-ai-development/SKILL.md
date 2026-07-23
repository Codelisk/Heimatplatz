---
name: astro-ai-development
description: Build, modify, and review the Heimatplatz web app in src/web using Astro 7, full Node SSR, Tailwind CSS 4, source-owned Starwind UI components, German i18n, dynamic SEO/AEO endpoints, and backend-first API integration. Use when working on .astro files, Astro routes or endpoints, layouts, server-side data loading, Starwind components, Tailwind styles, SEO metadata, sitemap/robots/llms, or the Astro development workflow.
---

# Astro AI Development

## Workflow

1. Work from `src/web` for the Astro app; keep `src/api` as the business-logic source and `src/maui` for mobile/desktop concerns.
2. Inspect `package.json` and `astro.config.mjs`, then check current Astro APIs before framework-level changes. Prefer the Astro Docs MCP server; if unavailable, use official Astro docs.
3. Preserve full request-time rendering: `output: 'server'` with `@astrojs/node` in standalone mode runs behind Caddy. Do not add `getStaticPaths()` to on-demand dynamic routes. Return a 404 `Response` for missing records, redirect stale slugs to their canonical URL, and use `prerender = true` only as a deliberate exception.
4. Keep data ownership backend-first. Put server-side domain access in `src/features/*`, use `getServerApiBaseUrl()` and the TTL cache in `src/lib/server`, and invalidate affected cache keys after mutations. Browser flows use `PUBLIC_API_BASE_URL`; never expose `ADMIN_API_KEY`, `SYNC_TRIGGER_KEY`, or other server-only values to client code.
5. Reuse the internal-route safeguards for mutating `/intern` endpoints: call the shared admin API client, reject cross-site requests, validate form and redirect inputs, and use a 303 redirect after successful form posts.
6. Build crawlable HTML and SEO at the layout and route layers. Keep titles, descriptions, canonicals, Open Graph, JSON-LD, `robots.txt`, the dynamic `sitemap.xml`, `llms.txt`, and `llms-full.txt` coherent. Add new public indexable routes to the sitemap; keep account, debug, API, and internal routes out.
7. Use source-owned Starwind components from `@/components/starwind/*` for common controls, then compose domain components under `src/components` and logic under `src/features`.
8. Render the useful initial state on the server and add client JavaScript only for interaction. Keep SSR and browser behavior aligned by sharing query and formatting helpers where possible.

## Project Conventions

- Keep shared site metadata in `src/config/site.ts`.
- Put global page chrome and metadata in `src/layouts/BaseLayout.astro`.
- Put domain API adapters, types, query builders, and formatters in `src/features/{feature}`.
- Put server-only helpers in `src/lib/server`; never import them into browser scripts.
- Put routes and server endpoints in `src/pages`; prefix route-local helper modules with `_`.
- Keep reusable UI under `src/components`, with installed Starwind source under `src/components/starwind`.
- Put German UI strings in `src/i18n/de` and access them through `t()` instead of hardcoding visible copy.
- Use the `@/*` alias for imports from `src/*`.
- Do not assume Content Collections or a shared `src/lib/api` client exist; neither is part of the current app.

## AI Context

Read `references/astro-current-stack.md` when you need exact package versions, runtime architecture, environment-variable boundaries, source links, MCP setup, or the current project map. Use documentation search narrowly instead of copying large external docs into context.

## Verification

Run this from `src/web` after meaningful changes:

```sh
npm run validate
```

For UI changes, run the dev server and inspect the affected route on desktop and mobile in light and dark mode. For route or server changes, also verify expected status codes, redirects, response headers, and production-like environment behavior.
