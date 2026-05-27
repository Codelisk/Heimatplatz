import { SITE } from "@/config/site";

export type StructuredData = Record<string, unknown>;
export type FaqItem = {
  question: string;
  answer: string;
};

export function getPageTitle(title: string) {
  if (title === SITE.name || title === SITE.title) {
    return SITE.title;
  }

  return SITE.titleTemplate.replace("%s", title);
}

export function getCanonicalUrl(path = "/") {
  return new URL(path, SITE.url).toString();
}

export function getAssetUrl(path: string) {
  return new URL(path, SITE.url).toString();
}

export function getRobotsDirective(noindex = false) {
  return noindex ? "noindex,follow" : "index,follow";
}

export function organizationSchema(): StructuredData {
  return {
    "@context": "https://schema.org",
    "@type": "Organization",
    name: SITE.name,
    url: SITE.url,
    logo: getAssetUrl("/favicon.svg"),
  };
}

export function websiteSchema(): StructuredData {
  return {
    "@context": "https://schema.org",
    "@type": "WebSite",
    name: SITE.name,
    url: SITE.url,
    inLanguage: SITE.language,
    potentialAction: {
      "@type": "SearchAction",
      target: `${SITE.url}/immobilien/?q={search_term_string}`,
      "query-input": "required name=search_term_string",
    },
  };
}

export function breadcrumbSchema(items: Array<{ name: string; url: string }>): StructuredData {
  return {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.name,
      item: item.url,
    })),
  };
}

export function faqSchema(items: FaqItem[]): StructuredData {
  return {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    mainEntity: items.map((item) => ({
      "@type": "Question",
      name: item.question,
      acceptedAnswer: {
        "@type": "Answer",
        text: item.answer,
      },
    })),
  };
}

export function realEstateAgentSchema(): StructuredData {
  return {
    "@context": "https://schema.org",
    "@type": "RealEstateAgent",
    name: SITE.name,
    url: SITE.url,
    areaServed: {
      "@type": "AdministrativeArea",
      name: "Oberösterreich",
    },
    knowsAbout: [
      "Immobilien in Oberösterreich",
      "Haus kaufen in Linz",
      "Wohnung kaufen in Wels",
      "Zwangsversteigerungen Oberösterreich",
    ],
  };
}
