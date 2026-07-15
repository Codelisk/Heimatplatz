// @ts-check
import node from '@astrojs/node';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'astro/config';

const site = process.env.PUBLIC_SITE_URL ?? 'https://heimatplatz.at';

// https://astro.build/config
export default defineConfig({
  site,
  // Vollstaendiges SSR: alle Seiten werden pro Request gerendert (Immobilien-Daten
  // sind damit immer aktuell, kein 6h-Rebuild mehr noetig). Die Sitemap wird
  // dynamisch ueber src/pages/sitemap.xml.ts erzeugt (@astrojs/sitemap kann nur
  // vorgerenderte Seiten auflisten).
  output: 'server',
  adapter: node({ mode: 'standalone' }),
  prefetch: true,
  devToolbar: {
    enabled: false,
  },
  vite: {
    plugins: [tailwindcss()],
  },
});
