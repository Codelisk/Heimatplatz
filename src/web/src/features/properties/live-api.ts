import { getServerApiBaseUrl } from "@/lib/server/api-base";
import { cached, TTL } from "@/lib/server/ttl-cache";

export type ApiContact = {
  Name?: string | null;
  Email?: string | null;
  Phone?: string | null;
  OriginalListingUrl?: string | null;
  SourceName?: string | null;
  Type?: string | number | null;
};

export type ApiProperty = {
  Id: string;
  Title: string;
  Address: string;
  MunicipalityId?: string;
  City: string;
  PostalCode: string;
  Price: number | string;
  LivingAreaM2?: number | string | null;
  PlotAreaM2?: number | string | null;
  Rooms?: number | string | null;
  YearBuilt?: number | string | null;
  Type: string;
  SellerType: string | number | null;
  SellerName: string;
  Description?: string | null;
  Features?: string[];
  ImageUrls: string[];
  CreatedAt: string;
  InquiryType?: string | number | null;
  SourceName?: string | null;
  Contacts?: ApiContact[];
  TypeSpecificData?: string | null;
};

type ApiPropertyResponse = {
  Properties?: ApiProperty[];
  Total?: number;
  HasMore?: boolean;
};

type ApiPropertyDetailResponse = {
  Property?: ApiProperty | null;
};

type SearchOptions = {
  page?: number;
  pageSize?: number;
  propertyTypes?: string[];
  sellerTypes?: string[];
};

export const API_PROPERTY_LIST_LIMIT = 96;
const FALLBACK_PROPERTY_IMAGE = "/favicon.svg";

export function getApiPropertyPath(propertyOrId: ApiProperty | string) {
  const id = typeof propertyOrId === "string" ? propertyOrId : propertyOrId.Id;
  return `/immobilien/angebote/${encodeURIComponent(id)}/`;
}

export function getApiPropertyImage(property: ApiProperty) {
  return property.ImageUrls?.[0] || FALLBACK_PROPERTY_IMAGE;
}

/**
 * Dedup key: die Quell-URL im API-Image-Proxy (`/api/images/proxy?url=...`).
 * Aus Zwangsversteigerungen synchronisierte Inserate enthalten im Altbestand
 * dasselbe Bild doppelt (nur Gross-/Kleinschreibung unterschiedlich), daher
 * case-insensitiv auf der dekodierten Quell-URL vergleichen.
 */
function getImageDedupKey(url: string) {
  try {
    const parsed = new URL(url, "https://heimatplatz.at");
    const source = parsed.searchParams.get("url");
    if (source) return decodeURIComponent(source).toLowerCase();
  } catch {
    // Roh-URL als Key behalten
  }
  return url.toLowerCase();
}

export function getApiPropertyImages(property: ApiProperty) {
  const seen = new Set<string>();
  return (property.ImageUrls ?? []).filter((url) => {
    if (!url) return false;
    const key = getImageDedupKey(url);
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}

async function isApiImageReachable(imageUrl: string) {
  if (!imageUrl || imageUrl.startsWith("/") || !imageUrl.includes("/api/images/proxy")) return true;

  let probeUrl = imageUrl;
  try {
    probeUrl = new URL(imageUrl).searchParams.get("url") ?? imageUrl;
  } catch {
    probeUrl = imageUrl;
  }

  return cached(`img-reachable:${probeUrl}`, TTL.images, async () => {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 2500);

    try {
      let response = await fetch(probeUrl, { method: "HEAD", signal: controller.signal });
      if (response.status === 405) {
        response = await fetch(probeUrl, {
          headers: { Range: "bytes=0-0" },
          signal: controller.signal,
        });
        await response.body?.cancel();
      }
      return response.ok;
    } catch {
      return true;
    } finally {
      clearTimeout(timeout);
    }
  });
}

export async function getVerifiedApiPropertyImage(property: ApiProperty) {
  const imageUrl = getApiPropertyImage(property);
  return await isApiImageReachable(imageUrl) ? imageUrl : FALLBACK_PROPERTY_IMAGE;
}

async function withVerifiedPrimaryImage(property: ApiProperty) {
  const imageUrl = getApiPropertyImage(property);
  if (await isApiImageReachable(imageUrl)) return property;

  return {
    ...property,
    ImageUrls: [FALLBACK_PROPERTY_IMAGE, ...(property.ImageUrls ?? []).slice(1)],
  };
}

export function isApiApartmentCandidate(property: ApiProperty) {
  if (property.Type !== "House") return false;

  const searchText = [
    property.Title,
    property.Description ?? "",
    property.Features?.join(" ") ?? "",
  ].join(" ").toLowerCase();

  return Boolean(property.LivingAreaM2 && !property.PlotAreaM2)
    || searchText.includes("wohnung")
    || searchText.includes("eigentumswohnung")
    || searchText.includes("apartment");
}

export function getApiPropertyTypeLabel(type: string, property?: ApiProperty) {
  if (property && isApiApartmentCandidate(property)) return "Wohnung";
  if (type === "Land") return "Grund";
  if (type === "Foreclosure") return "Zwangsversteigerung";
  return "Haus";
}

export function getApiPropertyTypeSearchValue(type: string, property?: ApiProperty) {
  if (property && isApiApartmentCandidate(property)) return "apartment";
  if (type === "Land") return "land";
  if (type === "Foreclosure") return "foreclosure";
  return "house";
}

export function getApiSellerSearchValue(sellerType: string | number | null) {
  if (sellerType === "Private" || sellerType === 1) return "private";
  // Gewerbliche Anbieter: Makler (2) und Hausverwaltung (3)
  if (sellerType === "Broker" || sellerType === 2) return "agent";
  if (sellerType === "PropertyManager" || sellerType === 3) return "agent";
  return "court";
}

export function getApiSellerLabel(sellerType: string | number | null) {
  if (sellerType === "PropertyManager" || sellerType === 3) return "Verwaltung";
  if (getApiSellerSearchValue(sellerType) === "private") return "Privat";
  if (getApiSellerSearchValue(sellerType) === "agent") return "Makler";
  return "Portal";
}

export function formatApiPrice(value: number | string | null | undefined) {
  const number = Number(value);
  if (!Number.isFinite(number) || number <= 0) return "Preis offen";
  // Branchenuebliche Schreibweise ("€ 365.000") statt "365 Tsd. EUR"
  return new Intl.NumberFormat("de-AT", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 0,
  }).format(number);
}

export function formatApiPriceLong(value: number | string | null | undefined) {
  const number = Number(value);
  if (!Number.isFinite(number) || number <= 0) return "Preis auf Anfrage";
  return new Intl.NumberFormat("de-AT", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 0,
  }).format(number);
}

export function formatApiDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return "";
  return new Intl.DateTimeFormat("de-AT", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(date);
}

export function getApiAddressLine(property: ApiProperty) {
  const address = (property.Address ?? "").trim();
  const postalCode = (property.PostalCode ?? "").trim();
  if (!address) return [postalCode, property.City].filter(Boolean).join(" ");
  // Adressen aus Importen enthalten oft schon die PLZ ("4742 Pram")
  if (!postalCode || address.startsWith(postalCode)) return address;
  return `${postalCode} ${address}`;
}

export function getApiAreaValue(property: ApiProperty) {
  return Number(property.PlotAreaM2 ?? property.LivingAreaM2 ?? 0);
}

export function getApiAreaLabel(property: ApiProperty) {
  if (property.PlotAreaM2) return `${property.PlotAreaM2} m² Grund`;
  if (property.LivingAreaM2) return `${property.LivingAreaM2} m² Wfl`;
  return "Fläche offen";
}

export function getApiPropertyDescription(property: ApiProperty) {
  return property.Description?.trim()
    || `${getApiPropertyTypeLabel(property.Type, property)} in ${property.PostalCode} ${property.City}, Oberösterreich. ${getApiAreaLabel(property)}. Anbieter: ${property.SellerName}.`;
}

export function getApiPropertyJsonLd(property: ApiProperty, url: string, image = getApiPropertyImage(property)) {
  return {
    "@context": "https://schema.org",
    "@type": "Offer",
    name: property.Title,
    description: getApiPropertyDescription(property),
    price: Number(property.Price) > 0 ? Number(property.Price) : undefined,
    priceCurrency: "EUR",
    availability: "https://schema.org/InStock",
    url,
    itemOffered: {
      "@type": "Residence",
      name: property.Title,
      image,
      address: {
        "@type": "PostalAddress",
        streetAddress: property.Address,
        postalCode: property.PostalCode,
        addressLocality: property.City,
        addressRegion: "Oberösterreich",
        addressCountry: "AT",
      },
      floorSize: property.LivingAreaM2
        ? {
            "@type": "QuantitativeValue",
            value: Number(property.LivingAreaM2),
            unitCode: "MTK",
          }
        : undefined,
    },
    seller: {
      "@type": "Organization",
      name: property.SellerName,
    },
  };
}

function buildSearchUrl(options: SearchOptions) {
  const url = new URL("/api/properties", getServerApiBaseUrl());
  url.searchParams.set("Page", String(options.page ?? 0));
  url.searchParams.set("PageSize", String(options.pageSize ?? API_PROPERTY_LIST_LIMIT));
  url.searchParams.set("SortBy", "CreatedAt");
  url.searchParams.set("SortDescending", "true");
  if (options.propertyTypes?.length) {
    url.searchParams.set("PropertyTypesJson", JSON.stringify(options.propertyTypes));
  }
  if (options.sellerTypes?.length) {
    url.searchParams.set("SellerTypesJson", JSON.stringify(options.sellerTypes));
  }
  return url;
}

async function fetchApiPropertiesUncached(options: SearchOptions = {}) {
  try {
    const response = await fetch(buildSearchUrl(options));
    if (!response.ok) throw new Error(`API ${response.status}`);
    const payload = await response.json() as ApiPropertyResponse;
    return await Promise.all((payload.Properties ?? []).map(withVerifiedPrimaryImage));
  } catch (error) {
    console.warn("[Heimatplatz] API properties could not be loaded", error);
    return [];
  }
}

export function fetchApiProperties(options: SearchOptions = {}) {
  return cached(`properties:${JSON.stringify(options)}`, TTL.properties, () =>
    fetchApiPropertiesUncached(options));
}

async function fetchApiPropertyByIdUncached(id: string) {
  try {
    const response = await fetch(new URL(`/api/properties/${encodeURIComponent(id)}`, getServerApiBaseUrl()));
    if (!response.ok) throw new Error(`API ${response.status}`);
    const payload = await response.json() as ApiPropertyDetailResponse;
    return payload.Property ? await withVerifiedPrimaryImage(payload.Property) : null;
  } catch (error) {
    console.warn(`[Heimatplatz] API property ${id} could not be loaded`, error);
    return null;
  }
}

export function fetchApiPropertyById(id: string) {
  return cached(`property:${id}`, TTL.properties, () => fetchApiPropertyByIdUncached(id));
}
