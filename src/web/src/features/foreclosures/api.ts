import { SITE } from "@/config/site";

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

const categoryLabels: Record<string, string> = {
  Einfamilienhaus: "Einfamilienhaus",
  Zweifamilienhaus: "Zweifamilienhaus",
  Mehrfamilienhaus: "Mehrfamilienhaus",
  Wohnungseigentum: "Wohnungseigentum",
  GewerblicheLiegenschaft: "Gewerbliche Liegenschaft",
  Grundstueck: "Grundstück",
  LandUndForstwirtschaft: "Land- und Forstwirtschaft",
  Sonstiges: "Sonstiges",
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
  const url = new URL("/api/foreclosure-auctions", SITE.apiBaseUrl);
  url.searchParams.set("Page", "1");
  url.searchParams.set("PageSize", String(pageSize));
  url.searchParams.set("IsActive", "true");
  return url;
}

export async function fetchForeclosureAuctions(pageSize = FORECLOSURE_BUILD_LIMIT) {
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
    console.warn("[Heimatplatz] Foreclosure auctions could not be pre-rendered", error);
    return [];
  }
}

export function getForeclosureAuctionSlug(auction: ApiForeclosureAuction) {
  const place = slugify(`${auction.PostalCode} ${auction.City}`);
  const category = slugify(getForeclosureCategoryLabel(auction.Category));
  return `zwangsversteigerung-${place || "oberoesterreich"}-${category || "immobilie"}-${auction.Id}`;
}

export function getForeclosureAuctionPath(auction: ApiForeclosureAuction) {
  return `/zwangsversteigerungen/${getForeclosureAuctionSlug(auction)}/`;
}

export function getForeclosureCategoryLabel(category: string | null | undefined) {
  if (!category) return "Zwangsversteigerung";
  return categoryLabels[category] ?? category;
}

export function isValidAuctionDate(value: string | null | undefined) {
  if (!value) return false;
  const date = new Date(value);
  return Number.isFinite(date.valueOf()) && date.getFullYear() > 1900;
}

export function formatAuctionDate(value: string | null | undefined, fallback = "Termin offen") {
  if (!isValidAuctionDate(value)) return fallback;
  return new Intl.DateTimeFormat("de-AT", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value as string));
}

export function formatAuctionDateShort(value: string | null | undefined, fallback = "Termin offen") {
  if (!isValidAuctionDate(value)) return fallback;
  return new Intl.DateTimeFormat("de-AT", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(new Date(value as string));
}

export function formatAuctionMoney(value: number | string | null | undefined, fallback = "Nicht angegeben") {
  const number = asNumber(value);
  if (!number || number <= 0) return fallback;
  return new Intl.NumberFormat("de-AT", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 0,
  }).format(number);
}

export function formatAuctionArea(value: number | string | null | undefined, fallback = "Nicht angegeben") {
  const number = asNumber(value);
  if (!number || number <= 0) return fallback;
  return `${new Intl.NumberFormat("de-AT", { maximumFractionDigits: 0 }).format(number)} m²`;
}

export function getAuctionPrimaryArea(auction: ApiForeclosureAuction) {
  return auction.TotalArea ?? auction.PlotArea ?? auction.BuildingArea ?? null;
}

export function getAuctionPriceLabel(auction: ApiForeclosureAuction) {
  return formatAuctionMoney(auction.MinimumBid ?? auction.EstimatedValue, "Preis offen");
}

export function getAuctionTitle(auction: ApiForeclosureAuction) {
  const category = getForeclosureCategoryLabel(auction.Category);
  return `${category} in ${auction.PostalCode} ${auction.City}`;
}

export function getAuctionDescription(auction: ApiForeclosureAuction) {
  const price = getAuctionPriceLabel(auction);
  const date = formatAuctionDateShort(auction.AuctionDate);
  const court = auction.Court ? ` Gericht: ${auction.Court}.` : "";
  const object = truncateText(auction.ObjectDescription, 80);
  return `${object} in ${auction.PostalCode} ${auction.City}. ${price}. Termin: ${date}.${court}`;
}

export function getAuctionDocumentLinks(auction: ApiForeclosureAuction) {
  return [
    ["Edikt", auction.EdictUrl],
    ["Grundriss", auction.FloorPlanUrl],
    ["Lageplan", auction.SitePlanUrl],
    ["Langschätzung", auction.LongAppraisalUrl],
    ["Kurzschätzung", auction.ShortAppraisalUrl],
  ].filter((entry): entry is [string, string] => Boolean(entry[1]));
}

export function getAuctionDetailSections(auction: ApiForeclosureAuction) {
  const sections = [
    {
      title: "Versteigerung",
      items: [
        ["Termin", formatAuctionDate(auction.AuctionDate)],
        ["Schätzwert", formatAuctionMoney(auction.EstimatedValue)],
        ["Mindestgebot", formatAuctionMoney(auction.MinimumBid)],
        ["Status", auction.Status],
        ["Eigentumsanteil", auction.OwnershipShare],
        ["Besichtigung", formatAuctionDate(auction.ViewingDate, "")],
        ["Gebotsfrist", formatAuctionDate(auction.BiddingDeadline, "")],
      ],
    },
    {
      title: "Basisdaten",
      items: [
        ["Kategorie", getForeclosureCategoryLabel(auction.Category)],
        ["Ort", `${auction.PostalCode} ${auction.City}`],
        ["Adresse", auction.Address],
        ["Gesamtfläche", formatAuctionArea(auction.TotalArea, "")],
        ["Grundstück", formatAuctionArea(auction.PlotArea, "")],
        ["Bebaute Fläche", formatAuctionArea(auction.BuildingArea, "")],
        ["Zimmer", auction.NumberOfRooms ? String(auction.NumberOfRooms) : ""],
        ["Baujahr", auction.YearBuilt ? String(auction.YearBuilt) : ""],
        ["Zustand", auction.BuildingCondition],
      ],
    },
    {
      title: "Rechtliches",
      items: [
        ["Gericht", auction.Court],
        ["Aktenzeichen", auction.CaseNumber],
        ["Einlagezahl", auction.RegistrationNumber],
        ["Katastralgemeinde", auction.CadastralMunicipality],
        ["Grundstücksnummer", auction.PlotNumber],
        ["Blatt", auction.SheetNumber],
        ["Flächenwidmung", auction.ZoningDesignation],
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
  return {
    "@context": "https://schema.org",
    "@type": "Event",
    name: getAuctionTitle(auction),
    description: getAuctionDescription(auction),
    url,
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
