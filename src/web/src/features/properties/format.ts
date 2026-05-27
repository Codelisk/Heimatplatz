import type { CollectionEntry } from "astro:content";

export type PropertyEntry = CollectionEntry<"properties">;

export const PROPERTY_TYPE_LABELS = {
  apartment: "Wohnung",
  house: "Haus",
  land: "Grund",
  foreclosure: "Zwangsversteigerung",
} as const;

export function formatPrice(value: number) {
  if (value >= 1000000) {
    return `${Math.round(value / 100000) / 10} Mio. EUR`;
  }

  return `${Math.round(value / 1000)} Tsd. EUR`;
}

export function formatPriceLong(value: number) {
  return new Intl.NumberFormat("de-AT", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 0,
  }).format(value);
}

export function formatDate(date: Date) {
  return new Intl.DateTimeFormat("de-AT", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(date);
}

export function getAreaLabel(property: PropertyEntry["data"]) {
  if (property.plotArea) {
    return `${property.plotArea} m2 Grund`;
  }

  if (property.livingArea) {
    return `${property.livingArea} m2 Wfl`;
  }

  return "auf Anfrage";
}

export function getPropertyTypeSearchValue(type: PropertyEntry["data"]["type"]) {
  return type;
}

export function getPropertyJsonLd(property: PropertyEntry["data"], url: string) {
  return {
    "@context": "https://schema.org",
    "@type": "Offer",
    name: property.title,
    description: property.description,
    price: property.price,
    priceCurrency: "EUR",
    availability: "https://schema.org/InStock",
    url,
    itemOffered: {
      "@type": "Residence",
      name: property.title,
      image: property.imageUrl,
      address: {
        "@type": "PostalAddress",
        streetAddress: property.address,
        postalCode: property.postalCode,
        addressLocality: property.location,
        addressRegion: "Oberösterreich",
        addressCountry: "AT",
      },
      floorSize: property.livingArea
        ? {
            "@type": "QuantitativeValue",
            value: property.livingArea,
            unitCode: "MTK",
          }
        : undefined,
    },
    seller: {
      "@type": "Organization",
      name: property.sellerName,
    },
  };
}
