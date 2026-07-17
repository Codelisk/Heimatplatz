// @ts-check
import node from '@astrojs/node';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'astro/config';

const site = process.env.PUBLIC_SITE_URL ?? 'https://heimatplatz.at';
const siteUrl = new URL(site);
const allowedDomain = {
  protocol: siteUrl.protocol.slice(0, -1),
  hostname: siteUrl.hostname,
  ...(siteUrl.port ? { port: siteUrl.port } : {}),
};

// https://astro.build/config
export default defineConfig({
  site,
  // Vollstaendiges SSR: alle Seiten werden pro Request gerendert (Immobilien-Daten
  // sind damit immer aktuell, kein 6h-Rebuild mehr noetig). Die Sitemap wird
  // dynamisch ueber src/pages/sitemap.xml.ts erzeugt (@astrojs/sitemap kann nur
  // vorgerenderte Seiten auflisten).
  output: 'server',
  adapter: node({ mode: 'standalone' }),
  // /immobilien/ und /zwangsversteigerungen/ existieren nicht als Index-Seiten
  // (nur Detailseiten darunter), wurden aber historisch verlinkt (SearchAction,
  // Registrierungs-Redirect) - alte externe Links sollen nicht ins 404 laufen.
  redirects: {
    '/immobilien/': '/',
    '/zwangsversteigerungen/': '/',
  },
  // Der SSR-Server laeuft hinter Caddy. Astro vertraut Forwarded-Host/-Proto nur
  // fuer explizit erlaubte Domains; andernfalls wird der interne HTTP-Origin
  // verwendet und gleich-originige Formular-POSTs werden faelschlich als
  // Cross-Site-Requests abgewiesen. Der Origin-/CSRF-Schutz bleibt aktiv.
  security: {
    allowedDomains: [allowedDomain],
  },
  prefetch: true,
  devToolbar: {
    enabled: false,
  },
  vite: {
    plugins: [tailwindcss()],
  },
});
