import { t } from "@/i18n";
import { getServerApiBaseUrl } from "@/lib/server/api-base";
import { cached, TTL } from "@/lib/server/ttl-cache";

export type ApiContact = {
  Name?: string | null;
  Email?: string | null;
  Phone?: string | null;
  OriginalListingUrl?: string | null;
  SourceName?: string | null;
  Type?: string | number | null;
  Source?: string | number | null;
  DisplayOrder?: number | null;
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
  TypeSpecificData?: string | Record<string, unknown> | null;
};

export type ApiPropertyResponse = {
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
  if (property && isApiApartmentCandidate(property)) return t("card.typeApartment");
  if (type === "Land") return t("card.typeLand");
  if (type === "Foreclosure") return t("card.typeForeclosure");
  return t("card.typeHouse");
}

export function getApiPropertyTypeSearchValue(type: string, property?: ApiProperty) {
  if (property && isApiApartmentCandidate(property)) return "apartment";
  if (type === "Land") return "land";
  if (type === "Foreclosure") return "foreclosure";
  return "house";
}

export function getApiSellerSearchValue(sellerType: string | number | null) {
  // Gewerbliche Anbieter: Makler (2) und Hausverwaltung (3)
  if (sellerType === "Broker" || sellerType === 2) return "agent";
  if (sellerType === "PropertyManager" || sellerType === 3) return "agent";
  return "private";
}

export function getApiSellerLabel(sellerType: string | number | null) {
  if (sellerType === "PropertyManager" || sellerType === 3) return t("card.sellerManager");
  if (getApiSellerSearchValue(sellerType) === "agent") return t("card.sellerAgent");
  return t("card.sellerPrivate");
}

// Kanonische Formatierung lebt in format.ts (client-sicher, keine
// Server-Abhaengigkeiten); hier nur re-exportiert fuer bestehende SSR-Importe.
export { formatApiDate, formatApiPrice, formatApiPriceLong, getApiLocationLine, getApiStreetLine } from "./format";

export function getApiAreaValue(property: ApiProperty) {
  return Number(property.PlotAreaM2 ?? property.LivingAreaM2 ?? 0);
}

export function getApiAreaLabel(property: ApiProperty) {
  if (property.PlotAreaM2) return t("card.plotAreaValue", { area: String(property.PlotAreaM2) });
  if (property.LivingAreaM2) return t("card.livingAreaValue", { area: String(property.LivingAreaM2) });
  return t("card.areaOpen");
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
      numberOfRooms: property.Rooms ? Number(property.Rooms) : undefined,
      yearBuilt: property.YearBuilt ? Number(property.YearBuilt) : undefined,
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

export type ApiPropertySearchResult = {
  properties: ApiProperty[];
  total: number;
  hasMore: boolean;
};

/**
 * Suche fuer die Startseite: nimmt den fertigen Query-String aus
 * buildPropertySearchQuery (search-query.ts) und liefert zusaetzlich
 * Total/HasMore fuer serverseitiges Paging.
 */
async function fetchApiPropertySearchUncached(query: string): Promise<ApiPropertySearchResult> {
  try {
    const response = await fetch(new URL(`/api/properties?${query}`, getServerApiBaseUrl()));
    if (!response.ok) throw new Error(`API ${response.status}`);
    const payload = await response.json() as ApiPropertyResponse;
    const properties = await Promise.all((payload.Properties ?? []).map(withVerifiedPrimaryImage));
    return { properties, total: payload.Total ?? properties.length, hasMore: payload.HasMore ?? false };
  } catch (error) {
    console.warn("[Heimatplatz] API property search failed", error);
    return { properties: [], total: 0, hasMore: false };
  }
}

export function fetchApiPropertySearch(query: string) {
  return cached(`property-search:${query}`, TTL.properties, () => fetchApiPropertySearchUncached(query));
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
  return cached(`property:${id}`, TTL.propertyDetail, () => fetchApiPropertyByIdUncached(id));
}

export type ApiPropertyTypeOption = {
  Value: string;
  Label: string;
};

type ApiPropertyTypesResponse = {
  Types?: ApiPropertyTypeOption[];
};

/**
 * Notfall-Fallback, falls die API beim SSR nicht erreichbar ist - die Quelle
 * der Wahrheit ist GET /api/properties/types (PropertyType-Enum im Backend).
 */
const FALLBACK_PROPERTY_TYPES: ApiPropertyTypeOption[] = [
  { Value: "House", Label: "Haus" },
  { Value: "Land", Label: "Grundstück" },
  { Value: "Foreclosure", Label: "Zwangsversteigerung" },
];

async function fetchApiPropertyTypesUncached(): Promise<ApiPropertyTypeOption[]> {
  try {
    const response = await fetch(new URL("/api/properties/types", getServerApiBaseUrl()));
    if (!response.ok) throw new Error(`API ${response.status}`);
    const payload = await response.json() as ApiPropertyTypesResponse;
    return payload.Types?.length ? payload.Types : FALLBACK_PROPERTY_TYPES;
  } catch (error) {
    console.warn("[Heimatplatz] API property types could not be loaded", error);
    return FALLBACK_PROPERTY_TYPES;
  }
}

export function fetchApiPropertyTypes() {
  return cached("property-types", TTL.locations, () => fetchApiPropertyTypesUncached());
}
