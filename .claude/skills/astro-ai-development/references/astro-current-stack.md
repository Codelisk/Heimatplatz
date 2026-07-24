# Astro Current Stack

Verified on 2026-07-23 against `src/web/package-lock.json`, `astro.config.mjs`, the current source tree, and the Hetzner deployment configuration.

## Locked Stack

- Node.js: `>=22.12.0`
- Astro: `7.1.3`
- `@astrojs/node`: `11.0.2`
- Tailwind CSS and `@tailwindcss/vite`: `4.3.2`
- TypeScript: `6.0.3`
- `@astrojs/check`: `0.9.9`
- Starwind UI: source-owned components tracked in `src/components/starwind`; component revisions are recorded in `starwind.config.json`

Treat `package.json` and `package-lock.json` as authoritative if these versions change.

## Runtime Architecture

- `astro.config.mjs` sets `output: 'server'` and uses `@astrojs/node` in standalone mode.
- Production and test run as Node containers behind Caddy. Pages and server endpoints render on demand.
- Dynamic routes load current records per request and do not use `getStaticPaths()`. Missing records return 404; stale foreclosure slugs redirect permanently to the canonical path.
- Server-side domain adapters live under `src/features/*`. They call the API through `src/lib/server/api-base.ts` and share the bounded in-memory TTL cache in `src/lib/server/ttl-cache.ts`.
- Browser interactions call the public API using the build-injected `PUBLIC_API_BASE_URL`. Server-only helpers and secrets must not enter client bundles.
- `src/pages/sitemap.xml.ts`, `robots.txt.ts`, `llms.txt.ts`, and `llms-full.txt.ts` are dynamic server endpoints. `@astrojs/sitemap` is not installed because it cannot enumerate the live API-backed detail routes.
- Content Collections are not configured, and there is no shared `src/lib/api` client. Do not recreate either without a concrete requirement.

## Project Map

- `src/config/site.ts`: canonical site URL, API URL, locale, default metadata, and branding
- `src/layouts/BaseLayout.astro`: page chrome, metadata, Open Graph, JSON-LD, analytics, header, footer, and shared browser state
- `src/features/{domain}`: typed domain API access, search/query state, formatters, and presentation helpers
- `src/lib/server`: server-only API base URL, TTL cache, admin API, CSRF guard, and Firmenbuch helpers
- `src/lib/seo.ts` and `src/lib/llms.ts`: structured data, canonical URLs, and LLM endpoint formatting
- `src/components/starwind`: installed Starwind source components
- `src/i18n/de`: German user-facing strings
- `src/pages`: pages and server endpoints; underscore-prefixed files are route-local helpers

## Environment Boundaries

- `PUBLIC_SITE_URL`: canonical site URL used by Astro configuration and metadata
- `PUBLIC_API_BASE_URL`: public API URL embedded for browser code and used as the local server fallback
- `API_BASE_URL_SERVER`: runtime-only API URL for SSR, normally the internal Docker address
- `PUBLIC_RYBBIT_SITE_ID`: optional build-time analytics site ID
- `ADMIN_API_KEY`: runtime-only shared key for server-side `/api/admin` calls
- `SYNC_TRIGGER_KEY`: runtime-only key for protected sync operations

Only `PUBLIC_*` values may be read by browser code.

## Current Source Links

- Astro on-demand rendering: https://docs.astro.build/en/guides/on-demand-rendering/
- Astro Node adapter: https://docs.astro.build/en/guides/integrations-guide/node/
- Astro dynamic routing: https://docs.astro.build/en/guides/routing/#on-demand-dynamic-routes
- Astro server endpoints: https://docs.astro.build/en/guides/endpoints/#server-endpoints-api-routes
- Astro environment variables: https://docs.astro.build/en/guides/environment-variables/
- Astro Tailwind styling: https://docs.astro.build/en/guides/styling/#tailwind
- Astro configuration reference: https://docs.astro.build/en/reference/configuration-reference/
- Starwind UI documentation: https://starwind.dev/docs/
- Starwind UI AI reference: https://starwind.dev/llms-full.txt

## AI Tooling

Astro Docs MCP server:

```json
{
  "mcpServers": {
    "astro-docs": {
      "type": "http",
      "url": "https://mcp.docs.astro.build/mcp"
    }
  }
}
```

Codex CLI project config alternative:

```toml
[mcp_servers.astro-docs]
command = "npx"
args = ["-y", "mcp-remote", "https://mcp.docs.astro.build/mcp"]
```

Use Starwind docs narrowly through `https://starwind.dev/llms-full.txt` or the CLI docs command:

```sh
npx starwind@latest docs button card input
```
