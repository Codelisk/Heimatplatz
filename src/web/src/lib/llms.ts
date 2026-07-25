import { SITE } from "@/config/site";
import {
  type ApiForeclosureAuction,
  formatAuctionArea,
  formatAuctionDate,
  formatAuctionMoney,
  getAuctionDescription,
  getAuctionPrimaryArea,
  getAuctionTitle,
  getForeclosureAuctionPath,
  getForeclosureCategoryLabel,
} from "@/features/foreclosures/api";
import {
  type ApiProperty,
  formatApiDate,
  formatApiPriceLong,
  getApiAreaLabel,
  getApiPropertyPath,
  getApiPropertyTypeLabel,
  getApiSellerLabel,
} from "@/features/properties/live-api";

/**
 * Gemeinsame Bausteine fuer /llms.txt (kompakte Uebersicht) und /llms-full.txt
 * (vollstaendige Inseratsdaten). Format folgt der llms.txt-Spezifikation
 * (llmstxt.org): Markdown mit H1, Blockquote-Zusammenfassung und Linklisten.
 */

export function absoluteUrl(path: string) {
  return new URL(path, SITE.url).toString();
}

// Eckige Klammern wuerden die Markdown-Linksyntax brechen
function mdText(value: string) {
  return value.replace(/\[/g, "(").replace(/\]/g, ")");
}

export function mdLink(title: string, path: string) {
  return `[${mdText(title)}](${absoluteUrl(path)})`;
}

function truncate(value: string, maxLength = 500) {
  const text = value.replace(/\s+/g, " ").trim();
  if (text.length <= maxLength) return text;
  return `${text.slice(0, maxLength).trimEnd()}…`;
}

export function propertyLine(property: ApiProperty) {
  const facts = [
    `${getApiPropertyTypeLabel(property.Type)} in ${property.PostalCode} ${property.City}`,
    formatApiPriceLong(property.Price),
    getApiAreaLabel(property),
  ].join(" · ");
  return `- ${mdLink(property.Title, getApiPropertyPath(property))}: ${facts}`;
}

export function auctionLine(auction: ApiForeclosureAuction) {
  const facts = [
    `Termin ${formatAuctionDate(auction.AuctionDate)}`,
    `Mindestgebot ${formatAuctionMoney(auction.MinimumBid ?? auction.EstimatedValue)}`,
    auction.Court,
  ].filter(Boolean).join(" · ");
  return `- ${mdLink(getAuctionTitle(auction), getForeclosureAuctionPath(auction))}: ${facts}`;
}

function factList(facts: Array<[string, string | null | undefined]>) {
  return facts
    .filter(([, value]) => Boolean(value?.trim()))
    .map(([label, value]) => `- ${label}: ${value}`)
    .join("\n");
}

export function propertyBlock(property: ApiProperty) {
  // Bei Zwangsversteigerungen ist der "Anbieter" das Gericht (wie auf der Detailseite)
  const sellerRole = property.Type === "Foreclosure" ? "Gericht" : getApiSellerLabel(property.SellerType);
  const facts = factList([
    ["URL", absoluteUrl(getApiPropertyPath(property))],
    ["Typ", getApiPropertyTypeLabel(property.Type)],
    ["Ort", `${property.PostalCode} ${property.City}, ${property.Address}`.trim()],
    ["Preis", formatApiPriceLong(property.Price)],
    ["Wohnfläche", property.LivingAreaM2 ? `${property.LivingAreaM2} m²` : null],
    ["Grundfläche", property.PlotAreaM2 ? `${property.PlotAreaM2} m²` : null],
    ["Zimmer", property.Rooms ? `${property.Rooms}` : null],
    ["Baujahr", property.YearBuilt ? `${property.YearBuilt}` : null],
    ["Anbieter", `${property.SellerName} (${sellerRole})`],
    ["Eingestellt", formatApiDate(property.CreatedAt)],
    ["Ausstattung", property.Features?.length ? property.Features.join(", ") : null],
    ["Beschreibung", property.Description ? truncate(property.Description) : null],
  ]);
  return `### ${mdText(property.Title)}\n\n${facts}`;
}

export function auctionBlock(auction: ApiForeclosureAuction) {
  const facts = factList([
    ["URL", absoluteUrl(getForeclosureAuctionPath(auction))],
    ["Kategorie", getForeclosureCategoryLabel(auction.Category)],
    ["Termin", formatAuctionDate(auction.AuctionDate)],
    ["Gericht", auction.Court],
    ["Aktenzeichen", auction.CaseNumber],
    ["Schätzwert", formatAuctionMoney(auction.EstimatedValue)],
    ["Mindestgebot", formatAuctionMoney(auction.MinimumBid)],
    ["Ort", `${auction.PostalCode} ${auction.City}, ${auction.Address}`.trim()],
    ["Fläche", formatAuctionArea(getAuctionPrimaryArea(auction))],
    ["Edikt", auction.EdictUrl],
    ["Beschreibung", truncate(getAuctionDescription(auction))],
  ]);
  return `### ${mdText(getAuctionTitle(auction))}\n\n${facts}`;
}

export function textResponse(body: string) {
  return new Response(body, {
    headers: {
      "content-type": "text/plain; charset=utf-8",
      // Gleiche TTL wie die Sitemap: Inserate aendern sich laufend,
      // eine Stunde Cache reicht fuer Crawler vollkommen.
      "cache-control": "public, max-age=3600",
    },
  });
}
