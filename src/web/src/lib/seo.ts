// Nur der Typ - api.ts ist serverseitig (fetch/TTL-Cache), "import type" wird beim Build entfernt
import type { ContactInfo } from "@/features/legal/api";
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

/**
 * Kontaktangaben fuer schema.org aus den gepflegten Daten - nur gesetzte Felder, damit
 * keine leeren Properties im JSON-LD landen (Google wertet die als Fehler).
 */
function contactProperties(contact?: ContactInfo): StructuredData {
  if (!contact) return {};

  const properties: StructuredData = {};

  if (contact.email) properties.email = contact.email;
  if (contact.phone) properties.telephone = contact.phone;

  if (contact.street && contact.city) {
    properties.address = {
      "@type": "PostalAddress",
      streetAddress: contact.street,
      postalCode: contact.postalCode,
      addressLocality: contact.city,
      addressCountry: "AT",
    };
  }

  if (contact.socialLinks.length > 0) {
    properties.sameAs = contact.socialLinks.map((link) => link.url);
  }

  return properties;
}

export function organizationSchema(contact?: ContactInfo): StructuredData {
  return {
    "@context": "https://schema.org",
    "@type": "Organization",
    name: SITE.name,
    url: SITE.url,
    logo: getAssetUrl("/favicon.svg"),
    ...contactProperties(contact),
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
      // Die Suche laeuft auf der Startseite - /immobilien/ hat keine Index-Route
      target: `${SITE.url}/?q={search_term_string}`,
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

export function realEstateAgentSchema(contact?: ContactInfo): StructuredData {
  return {
    "@context": "https://schema.org",
    "@type": "RealEstateAgent",
    name: SITE.name,
    url: SITE.url,
    ...contactProperties(contact),
    areaServed: {
      "@type": "AdministrativeArea",
      name: "Oberösterreich",
    },
    knowsAbout: [
      "Immobilien in Oberösterreich",
      "Haus kaufen in Linz",
      "Grundstück kaufen in Wels",
      "Zwangsversteigerungen Oberösterreich",
    ],
  };
}
