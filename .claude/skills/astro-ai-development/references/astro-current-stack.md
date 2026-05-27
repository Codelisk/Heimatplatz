# Astro Current Stack

Verified on 2026-05-27 for the Heimatplatz Astro migration.

## Source Links

- Astro Build with AI: https://docs.astro.build/en/guides/build-with-ai/
- Astro Sitemap integration: https://docs.astro.build/en/guides/integrations-guide/sitemap/
- Astro Tailwind styling: https://docs.astro.build/en/guides/styling/#tailwind
- Astro content collections: https://docs.astro.build/en/guides/content-collections/
- Astro configuration `site`: https://docs.astro.build/en/reference/configuration-reference/#site
- Starwind UI installation: https://starwind.dev/docs/getting-started/installation/
- Starwind UI AI reference: https://starwind.dev/llms-full.txt
- Starwind UI GitHub: https://github.com/starwind-ui/starwind-ui
- AstroWind GitHub SEO template reference: https://github.com/onwidget/astrowind

## Current Decisions

- Astro is the app framework and remains static-first for public SEO pages.
- Tailwind CSS 4 is configured through `@tailwindcss/vite`, matching current Astro guidance.
- Starwind UI is used as the Astro-native component system because it is designed for Astro v6 and Tailwind 4, installs source-owned components, and publishes LLM/MCP-friendly references.
- `@astrojs/sitemap` is configured because Astro's official integration generates sitemap files from static routes and requires `site`.
- `robots.txt` is generated dynamically so the sitemap URL stays in sync with `astro.config.mjs`.
- `llms.txt` is generated for AI-agent discoverability of the public site structure.

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
