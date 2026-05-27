// @ts-check
import sitemap from '@astrojs/sitemap';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'astro/config';

const site = process.env.PUBLIC_SITE_URL ?? 'https://heimatplatz.at';
const noIndexRoutes = [
  '/anmelden/',
  '/registrieren/',
  '/favoriten/',
  '/blockiert/',
  '/meine-immobilien/',
  '/benachrichtigungen/',
  '/filter-einstellungen/',
  '/profil/',
  '/immobilien/live/',
  '/immobilien/bearbeiten/',
];

// https://astro.build/config
export default defineConfig({
  site,
  output: 'static',
  prefetch: true,
  devToolbar: {
    enabled: false,
  },
  integrations: [
    sitemap({
      changefreq: 'daily',
      priority: 0.7,
      filter: (page) => !noIndexRoutes.some((route) => page.endsWith(route)),
    }),
  ],
  vite: {
    plugins: [tailwindcss()],
  },
});
