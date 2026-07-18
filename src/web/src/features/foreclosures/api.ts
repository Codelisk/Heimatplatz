import { t } from "@/i18n";
import { getServerApiBaseUrl } from "@/lib/server/api-base";
import { cached, TTL } from "@/lib/server/ttl-cache";

export type ApiForeclosureAuction = {
  Id: string;
  AuctionDate: string;
  Category: string;
  ObjectDescription: string;
  Status?: string | null;
  Address: string;
  City: string;
  PostalCode: string;
  RegistrationNumber?: string | null;
  CadastralMunicipality?: string | null;
  PlotNumber?: string | null;
  SheetNumber?: string | null;
  TotalArea?: number | string | null;
  BuildingArea?: number | string | null;
  GardenArea?: number | string | null;
  PlotArea?: number | string | null;
  YearBuilt?: number | string | null;
  NumberOfRooms?: number | string | null;
  ZoningDesignation?: string | null;
  BuildingCondition?: string | null;
  EstimatedValue?: number | string | null;
  MinimumBid?: number | string | null;
  ViewingDate?: string | null;
  BiddingDeadline?: string | null;
  OwnershipShare?: string | null;
  CaseNumber?: string | null;
  Court?: string | null;
  EdictUrl?: string | null;
  Notes?: string | null;
  FloorPlanUrl?: string | null;
  SitePlanUrl?: string | null;
  LongAppraisalUrl?: string | null;
  ShortAppraisalUrl?: string | null;
  ImageUrls?: string[];
  CreatedAt: string;
  ExternalId?: string | null;
  State?: string | null;
  IsActive: boolean;
  FirstSeenAt?: string | null;
  LastScrapedAt?: string | null;
  RemovedAt?: string | null;
};

type ForeclosureAuctionResponse = {
  Auctions?: ApiForeclosureAuction[];
  TotalCount?: number;
  Page?: number;
  PageSize?: number;
};

export const FORECLOSURE_BUILD_LIMIT = 128;
const FALLBACK_AUCTION_IMAGE = "/favicon.svg";

// Achtung: die Label fliessen ueber getForeclosureAuctionSlug in URLs ein —
// Wert-Aenderungen in i18n/de/foreclosures.ts aendern also Slugs!
const categoryLabels: Record<string, string> = {
  Einfamilienhaus: t("zv.categoryEinfamilienhaus"),
  Zweifamilienhaus: t("zv.categoryZweifamilienhaus"),
  Mehrfamilienhaus: t("zv.categoryMehrfamilienhaus"),
  Wohnungseigentum: t("zv.categoryWohnungseigentum"),
  GewerblicheLiegenschaft: t("zv.categoryGewerblicheLiegenschaft"),
  Grundstueck: t("zv.categoryGrundstueck"),
  LandUndForstwirtschaft: t("zv.categoryLandUndForstwirtschaft"),
  Sonstiges: t("zv.categorySonstiges"),
};

function slugify(value: string) {
  return value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/ß/g, "ss")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 80);
}

function asNumber(value: number | string | null | undefined) {
  const number = Number(value);
  return Number.isFinite(number) ? number : null;
}

function cleanText(value: string) {
  return value.replace(/\s+/g, " ").trim();
}

function truncateText(value: string, maxLength: number) {
  const text = cleanText(value);
  if (text.length <= maxLength) return text;
  const slice = text.slice(0, maxLength - 1);
  return `${slice.slice(0, slice.lastIndexOf(" ") > 40 ? slice.lastIndexOf(" ") : slice.length)}…`;
}

function isUpperAustriaAuction(auction: ApiForeclosureAuction) {
  if (auction.State === "Oberoesterreich") return true;

  // Some scraped edict rows arrive without a federal state or with a neighboring state,
  // while the postal code/court city is in Upper Austria. Keep those OOE-relevant rows.
  return /^(4|51|52|53)/.test(auction.PostalCode);
}

function getAuctionRelevanceRank(auction: ApiForeclosureAuction) {
  const status = auction.Status?.toLowerCase() ?? "";
  if (status.startsWith("meistbotsverteilung") || status.startsWith("zuschlag")) return 2;
  if (!isValidAuctionDate(auction.AuctionDate)) return 1;
  return 0;
}

function buildForeclosureUrl(pageSize = FORECLOSURE_BUILD_LIMIT) {
  const url = new URL("/api/foreclosure-auctions", getServerApiBaseUrl());
  url.searchParams.set("Page", "1");
  url.searchParams.set("PageSize", String(pageSize));
  url.searchParams.set("IsActive", "true");
  return url;
}

export function fetchForeclosureAuctions(pageSize = FORECLOSURE_BUILD_LIMIT) {
  return cached(`foreclosures:${pageSize}`, TTL.properties, async () => {
    try {
      const response = await fetch(buildForeclosureUrl(pageSize));
      if (!response.ok) throw new Error(`API ${response.status}`);
      const payload = (await response.json()) as ForeclosureAuctionResponse;
      return (payload.Auctions ?? [])
        .filter(isUpperAustriaAuction)
        .sort((a, b) => {
          const rankDiff = getAuctionRelevanceRank(a) - getAuctionRelevanceRank(b);
          if (rankDiff !== 0) return rankDiff;

          const dateA = isValidAuctionDate(a.AuctionDate) ? new Date(a.AuctionDate).valueOf() : 0;
          const dateB = isValidAuctionDate(b.AuctionDate) ? new Date(b.AuctionDate).valueOf() : 0;
          return dateB - dateA;
        });
    } catch (error) {
      console.warn("[Heimatplatz] Foreclosure auctions could not be loaded", error);
      return [];
    }
  });
}

type ForeclosureAuctionDetailResponse = {
  Auction?: ApiForeclosureAuction | null;
};

/**
 * Einzelne Versteigerung fuer die SSR-Detailseite. Nicht-OOe-Eintraege werden
 * wie in der Liste ausgefiltert (die Seite existierte vorher nur fuer OOe).
 */
export function fetchForeclosureAuctionById(id: string) {
  return cached(`foreclosure:${id}`, TTL.properties, async () => {
    try {
      const response = await fetch(
        new URL(`/api/foreclosure-auctions/${encodeURIComponent(id)}`, getServerApiBaseUrl()),
      );
      if (!response.ok) throw new Error(`API ${response.status}`);
      const payload = (await response.json()) as ForeclosureAuctionDetailResponse;
      const auction = payload.Auction ?? null;
      return auction && isUpperAustriaAuction(auction) ? auction : null;
    } catch (error) {
      console.warn(`[Heimatplatz] Foreclosure auction ${id} could not be loaded`, error);
      return null;
    }
  });
}

export function getForeclosureAuctionSlug(auction: ApiForeclosureAuction) {
  const place = slugify(`${auction.PostalCode} ${auction.City}`);
  const category = slugify(getForeclosureCategoryLabel(auction.Category));
  return `zwangsversteigerung-${place || "oberoesterreich"}-${category || "immobilie"}-${auction.Id}`;
}

export function getForeclosureAuctionPath(auction: ApiForeclosureAuction) {
  return `/zwangsversteigerungen/${getForeclosureAuctionSlug(auction)}/`;
}

export function getAuctionImage(auction: ApiForeclosureAuction) {
  return getAuctionImages(auction)[0] ?? FALLBACK_AUCTION_IMAGE;
}

/**
 * Dedup key for gallery images: the edikte source URL hidden inside the
 * API image proxy (`/api/images/proxy?url=...`). Legacy scrapes stored the
 * same attachment twice with different casing (direct link vs. thumbnail
 * derived URL), so compare case-insensitively on the decoded source URL.
 */
function getImageDedupKey(url: string) {
  try {
    const parsed = new URL(url, "https://heimatplatz.at");
    const source = parsed.searchParams.get("url");
    if (source) return decodeURIComponent(source).toLowerCase();
  } catch {
    // keep raw URL as key
  }
  return url.toLowerCase();
}

export function getAuctionImages(auction: ApiForeclosureAuction) {
  const seen = new Set<string>();
  return (auction.ImageUrls ?? []).filter((url) => {
    if (!url) return false;
    const key = getImageDedupKey(url);
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}


export function getForeclosureCategoryLabel(category: string | null | undefined) {
  if (!category) return t("zv.categoryFallback");
  return categoryLabels[category] ?? category;
}

export function isValidAuctionDate(value: string | null | undefined) {
  if (!value) return false;
  const date = new Date(value);
  return Number.isFinite(date.valueOf()) && date.getFullYear() > 1900;
}

export function formatAuctionDate(value: string | null | undefined, fallback = t("zv.dateOpen")) {
  if (!isValidAuctionDate(value)) return fallback;
  return new Intl.DateTimeFormat("de-AT", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value as string));
}

export function formatAuctionDateShort(value: string | null | undefined, fallback = t("zv.dateOpen")) {
  if (!isValidAuctionDate(value)) return fallback;
  return new Intl.DateTimeFormat("de-AT", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(new Date(value as string));
}

export function formatAuctionMoney(value: number | string | null | undefined, fallback = t("zv.notSpecified")) {
  const number = asNumber(value);
  if (!number || number <= 0) return fallback;
  return new Intl.NumberFormat("de-AT", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 0,
  }).format(number);
}

export function formatAuctionArea(value: number | string | null | undefined, fallback = t("zv.notSpecified")) {
  const number = asNumber(value);
  if (!number || number <= 0) return fallback;
  return `${new Intl.NumberFormat("de-AT", { maximumFractionDigits: 0 }).format(number)} m²`;
}

export function getAuctionPrimaryArea(auction: ApiForeclosureAuction) {
  return auction.TotalArea ?? auction.PlotArea ?? auction.BuildingArea ?? null;
}

export function getAuctionPriceLabel(auction: ApiForeclosureAuction) {
  return formatAuctionMoney(auction.MinimumBid ?? auction.EstimatedValue, t("property.priceOpen"));
}

export function getAuctionTitle(auction: ApiForeclosureAuction) {
  return t("zv.titlePattern", {
    category: getForeclosureCategoryLabel(auction.Category),
    postalCode: auction.PostalCode,
    city: auction.City,
  });
}

export function getAuctionDescription(auction: ApiForeclosureAuction) {
  return t("zv.descriptionPattern", {
    object: truncateText(auction.ObjectDescription, 80),
    postalCode: auction.PostalCode,
    city: auction.City,
    price: getAuctionPriceLabel(auction),
    date: formatAuctionDateShort(auction.AuctionDate),
    courtSuffix: auction.Court ? t("zv.descriptionCourtSuffix", { court: auction.Court }) : "",
  });
}

export function getAuctionDocumentLinks(auction: ApiForeclosureAuction) {
  return [
    [t("zv.docEdict"), auction.EdictUrl],
    [t("zv.docFloorPlan"), auction.FloorPlanUrl],
    [t("zv.docSitePlan"), auction.SitePlanUrl],
    [t("zv.docLongAppraisal"), auction.LongAppraisalUrl],
    [t("zv.docShortAppraisal"), auction.ShortAppraisalUrl],
  ].filter((entry): entry is [string, string] => Boolean(entry[1]));
}

export function getAuctionDetailSections(auction: ApiForeclosureAuction) {
  const sections = [
    {
      title: t("zv.sectionAuction"),
      items: [
        [t("zv.labelDate"), formatAuctionDate(auction.AuctionDate)],
        [t("zv.labelEstimatedValue"), formatAuctionMoney(auction.EstimatedValue)],
        [t("zv.labelMinimumBid"), formatAuctionMoney(auction.MinimumBid)],
        [t("zv.labelStatus"), auction.Status],
        [t("zv.labelOwnershipShare"), auction.OwnershipShare],
        [t("zv.labelViewing"), formatAuctionDate(auction.ViewingDate, "")],
        [t("zv.labelBiddingDeadline"), formatAuctionDate(auction.BiddingDeadline, "")],
      ],
    },
    {
      title: t("zv.sectionBasics"),
      items: [
        [t("zv.labelCategory"), getForeclosureCategoryLabel(auction.Category)],
        [t("zv.labelCity"), `${auction.PostalCode} ${auction.City}`],
        [t("zv.labelAddress"), auction.Address],
        [t("zv.labelTotalArea"), formatAuctionArea(auction.TotalArea, "")],
        [t("zv.labelPlot"), formatAuctionArea(auction.PlotArea, "")],
        [t("zv.labelBuildingArea"), formatAuctionArea(auction.BuildingArea, "")],
        [t("zv.labelRooms"), auction.NumberOfRooms ? String(auction.NumberOfRooms) : ""],
        [t("zv.labelYearBuilt"), auction.YearBuilt ? String(auction.YearBuilt) : ""],
        [t("zv.labelCondition"), auction.BuildingCondition],
      ],
    },
    {
      title: t("zv.sectionLegal"),
      items: [
        [t("zv.labelCourt"), auction.Court],
        [t("zv.labelCaseNumber"), auction.CaseNumber],
        [t("zv.labelRegistrationNumber"), auction.RegistrationNumber],
        [t("zv.labelCadastralMunicipality"), auction.CadastralMunicipality],
        [t("zv.labelPlotNumber"), auction.PlotNumber],
        [t("zv.labelSheet"), auction.SheetNumber],
        [t("zv.labelZoning"), auction.ZoningDesignation],
      ],
    },
  ];

  return sections
    .map((section) => ({
      ...section,
      items: section.items.filter(([, value]) => Boolean(value)),
    }))
    .filter((section) => section.items.length > 0);
}

export function getAuctionJsonLd(auction: ApiForeclosureAuction, url: string) {
  const images = getAuctionImages(auction).slice(0, 6);
  return {
    "@context": "https://schema.org",
    "@type": "Event",
    name: getAuctionTitle(auction),
    description: getAuctionDescription(auction),
    url,
    image: images.length > 0 ? images : undefined,
    startDate: isValidAuctionDate(auction.AuctionDate) ? auction.AuctionDate : undefined,
    eventStatus: "https://schema.org/EventScheduled",
    eventAttendanceMode: "https://schema.org/OfflineEventAttendanceMode",
    location: {
      "@type": "Place",
      name: auction.Court ?? auction.Address,
      address: {
        "@type": "PostalAddress",
        streetAddress: auction.Address,
        postalCode: auction.PostalCode,
        addressLocality: auction.City,
        addressRegion: "Oberösterreich",
        addressCountry: "AT",
      },
    },
    offers: {
      "@type": "Offer",
      price: asNumber(auction.MinimumBid ?? auction.EstimatedValue) ?? undefined,
      priceCurrency: "EUR",
      availability: "https://schema.org/InStock",
      url,
    },
    subjectOf: auction.EdictUrl
      ? {
          "@type": "CreativeWork",
          name: "Edikt",
          url: auction.EdictUrl,
        }
      : undefined,
  };
}
