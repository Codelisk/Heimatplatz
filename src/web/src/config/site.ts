export const SITE = {
  name: "Heimatplatz",
  url: import.meta.env.PUBLIC_SITE_URL ?? "https://heimatplatz.at",
  apiBaseUrl:
    import.meta.env.PUBLIC_API_BASE_URL ?? "https://heimatplatz-api.azurewebsites.net",
  locale: "de_AT",
  language: "de-AT",
  title: "Heimatplatz - Immobilien in Oberösterreich finden",
  titleTemplate: "%s | Heimatplatz",
  description:
    "Heimatplatz buendelt Haeuser, Wohnungen, Grundstuecke und Zwangsversteigerungen in Oberösterreich in einer schnellen, suchmaschinenfreundlichen Web-App.",
  // PNG statt SVG: Facebook/WhatsApp/LinkedIn rendern keine SVG-OG-Bilder
  defaultImage: "/og/heimatplatz-default.png",
  themeColor: "#0a0a0a",
  keywords: [
    "Immobilien Oberoesterreich",
    "Haus kaufen",
    "Wohnung kaufen",
    "Grundstueck kaufen",
    "Zwangsversteigerung",
    "Heimatplatz",
  ],
} as const;
