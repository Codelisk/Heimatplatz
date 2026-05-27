import { SITE } from "@/config/site";

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

export const API_PROPERTY_BUILD_LIMIT = 96;
const FALLBACK_PROPERTY_IMAGE = "/og/heimatplatz-default.svg";
const apiPropertyCache = new Map<string, Promise<ApiProperty[]>>();
const apiPropertyDetailCache = new Map<string, Promise<ApiProperty | null>>();
const apiImageReachabilityCache = new Map<string, Promise<boolean>>();

export function getApiPropertyPath(propertyOrId: ApiProperty | string) {
  const id = typeof propertyOrId === "string" ? propertyOrId : propertyOrId.Id;
  return `/immobilien/angebote/${encodeURIComponent(id)}/`;
}

export function getApiPropertyImage(property: ApiProperty) {
  return property.ImageUrls?.[0] || FALLBACK_PROPERTY_IMAGE;
}

async function isApiImageReachable(imageUrl: string) {
  if (!imageUrl || imageUrl.startsWith("/") || !imageUrl.includes("/api/images/proxy")) return true;

  let probeUrl = imageUrl;
  try {
    probeUrl = new URL(imageUrl).searchParams.get("url") ?? imageUrl;
  } catch {
    probeUrl = imageUrl;
  }

  if (!apiImageReachabilityCache.has(probeUrl)) {
    apiImageReachabilityCache.set(probeUrl, (async () => {
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
    })());
  }

  return apiImageReachabilityCache.get(probeUrl) as Promise<boolean>;
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
  if (sellerType === "Broker" || sellerType === 2 || sellerType === 3) return "agent";
  return "court";
}

export function getApiSellerLabel(sellerType: string | number | null) {
  if (getApiSellerSearchValue(sellerType) === "private") return "Privat";
  if (getApiSellerSearchValue(sellerType) === "agent") return "Makler";
  return "Portal";
}

export function formatApiPrice(value: number | string | null | undefined) {
  const number = Number(value);
  if (!Number.isFinite(number) || number <= 0) return "Preis offen";
  if (number >= 1_000_000) return `${(number / 1_000_000).toLocaleString("de-AT", { maximumFractionDigits: 1 })} Mio. EUR`;
  if (number >= 1000) return `${Math.round(number / 1000).toLocaleString("de-AT")} Tsd. EUR`;
  return `${number.toLocaleString("de-AT")} EUR`;
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

export function getApiAreaValue(property: ApiProperty) {
  return Number(property.PlotAreaM2 ?? property.LivingAreaM2 ?? 0);
}

export function getApiAreaLabel(property: ApiProperty) {
  if (property.PlotAreaM2) return `${property.PlotAreaM2} m2 Grund`;
  if (property.LivingAreaM2) return `${property.LivingAreaM2} m2 Wfl`;
  return "Flaeche offen";
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
  const url = new URL("/api/properties", SITE.apiBaseUrl);
  url.searchParams.set("Page", String(options.page ?? 0));
  url.searchParams.set("PageSize", String(options.pageSize ?? API_PROPERTY_BUILD_LIMIT));
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
    console.warn("[Heimatplatz] API properties could not be pre-rendered", error);
    return [];
  }
}

export function fetchApiProperties(options: SearchOptions = {}) {
  const key = JSON.stringify(options);
  if (!apiPropertyCache.has(key)) {
    apiPropertyCache.set(key, fetchApiPropertiesUncached(options));
  }

  return apiPropertyCache.get(key) as Promise<ApiProperty[]>;
}

async function fetchApiPropertyByIdUncached(id: string) {
  try {
    const response = await fetch(new URL(`/api/properties/${encodeURIComponent(id)}`, SITE.apiBaseUrl));
    if (!response.ok) throw new Error(`API ${response.status}`);
    const payload = await response.json() as ApiPropertyDetailResponse;
    return payload.Property ? await withVerifiedPrimaryImage(payload.Property) : null;
  } catch (error) {
    console.warn(`[Heimatplatz] API property ${id} could not be pre-rendered`, error);
    return null;
  }
}

export function fetchApiPropertyById(id: string) {
  if (!apiPropertyDetailCache.has(id)) {
    apiPropertyDetailCache.set(id, fetchApiPropertyByIdUncached(id));
  }

  return apiPropertyDetailCache.get(id) as Promise<ApiProperty | null>;
}
