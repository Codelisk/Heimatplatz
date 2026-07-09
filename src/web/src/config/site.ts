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
    "Heimatplatz bündelt Häuser, Wohnungen, Grundstücke und Zwangsversteigerungen in Oberösterreich in einer schnellen, suchmaschinenfreundlichen Web-App.",
  // PNG statt SVG: Facebook/WhatsApp/LinkedIn rendern keine SVG-OG-Bilder
  defaultImage: "/og/heimatplatz-default.png",
  themeColor: "#0a0a0a",
  keywords: [
    "Immobilien Oberösterreich",
    "Haus kaufen",
    "Wohnung kaufen",
    "Grundstück kaufen",
    "Zwangsversteigerung",
    "Heimatplatz",
  ],
} as const;
