import { t } from "@/i18n";
import { formatApiDate, formatApiPriceLong, getApiPropertyTypeLabel, type ApiProperty } from "./live-api";

export type DetailItem = {
  label: string;
  value: string;
  /** Optionaler externer Link - der Renderer gibt den Wert dann als <a> aus */
  href?: string;
};

export type PropertyDetailSection = {
  title: string;
  items: DetailItem[];
};

type TypeSpecificData = Record<string, unknown>;

// Anzeigetitel der Sektionen dienen zugleich als Map-Schlüssel — Reihenfolge hier
const SECTIONS = {
  basics: t("detail.sectionBasics"),
  areas: t("detail.sectionAreas"),
  building: t("detail.sectionBuilding"),
  equipment: t("detail.sectionEquipment"),
  plot: t("detail.sectionPlot"),
  auction: t("detail.sectionAuction"),
  costs: t("detail.sectionCosts"),
} as const;

const SECTION_ORDER = [
  SECTIONS.basics,
  SECTIONS.areas,
  SECTIONS.building,
  SECTIONS.equipment,
  SECTIONS.plot,
  SECTIONS.auction,
  SECTIONS.costs,
] as const;

function readTypeSpecificData(property: Pick<ApiProperty, "TypeSpecificData">): TypeSpecificData {
  if (!property.TypeSpecificData) return {};
  if (typeof property.TypeSpecificData === "object") return property.TypeSpecificData as TypeSpecificData;

  try {
    const parsed = JSON.parse(property.TypeSpecificData);
    return parsed && typeof parsed === "object" ? parsed as TypeSpecificData : {};
  } catch {
    return {};
  }
}

function scalar(data: TypeSpecificData, key: string) {
  const value = data[key];
  if (value === null || value === undefined || value === "") return "";
  return String(value);
}

function numberValue(value: unknown) {
  const number = Number(value);
  return Number.isFinite(number) && number > 0 ? number : null;
}

function positiveText(value: unknown) {
  const number = numberValue(value);
  return number ? String(number) : "";
}

function boolValue(value: unknown) {
  if (typeof value === "boolean") return value;
  if (typeof value === "string") return value.toLowerCase() === "true";
  return null;
}

function add(sectionMap: Map<string, DetailItem[]>, section: string, label: string, value: string | number | null | undefined) {
  if (value === null || value === undefined || value === "") return;
  const items = sectionMap.get(section) ?? [];
  items.push({ label, value: String(value) });
  sectionMap.set(section, items);
}

function formatArea(value: unknown) {
  const number = numberValue(value);
  if (!number) return "";
  return `${new Intl.NumberFormat("de-AT", { maximumFractionDigits: 0 }).format(number)} m²`;
}

function formatMoney(value: unknown) {
  const number = numberValue(value);
  if (!number) return "";
  return new Intl.NumberFormat("de-AT", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 0,
  }).format(number);
}

function formatDateTime(value: unknown) {
  if (!value || typeof value !== "string") return "";
  const date = new Date(value);
  if (!Number.isFinite(date.valueOf())) return "";
  return new Intl.DateTimeFormat("de-AT", {
    timeZone: "Europe/Vienna",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

function formatBool(value: unknown) {
  const bool = boolValue(value);
  if (bool === null) return "";
  return bool ? t("detail.yes") : t("detail.no");
}

function formatCondition(value: unknown) {
  const labels: Record<string, string> = {
    LikeNew: t("detail.conditionLikeNew"),
    Good: t("detail.conditionGood"),
    Average: t("detail.conditionAverage"),
    NeedsRenovation: t("detail.conditionNeedsRenovation"),
  };
  const key = scalar({ value }, "value");
  return labels[key] ?? key;
}

function formatZoning(value: unknown) {
  const labels: Record<string, string> = {
    Residential: t("detail.zoningResidential"),
    Commercial: t("detail.zoningCommercial"),
    Industrial: t("detail.zoningIndustrial"),
    Agricultural: t("detail.zoningAgricultural"),
    Mixed: t("detail.zoningMixed"),
  };
  const key = scalar({ value }, "value");
  return labels[key] ?? key;
}

function formatSoilQuality(value: unknown) {
  const labels: Record<string, string> = {
    High: t("detail.soilHigh"),
    Medium: t("detail.soilMedium"),
    Low: t("detail.soilLow"),
  };
  const key = scalar({ value }, "value");
  return labels[key] ?? key;
}

function formatLegalStatus(value: unknown) {
  const labels: Record<string, string> = {
    Pending: t("detail.statusPending"),
    Scheduled: t("detail.statusScheduled"),
    InProgress: t("detail.statusInProgress"),
    Completed: t("detail.statusCompleted"),
    Cancelled: t("detail.statusCancelled"),
  };
  const key = scalar({ value }, "value");
  return labels[key] ?? key;
}

/**
 * Preis-Kachel mit typrichtigem Label: Bei Zwangsversteigerungen wird Price beim Sync
 * aus MinimumBid ?? EstimatedValue befuellt - einen Kaufpreis gibt es dort nicht.
 */
export function getApiPriceFact(property: ApiProperty): DetailItem {
  if (property.Type !== "Foreclosure") {
    return { label: t("detail.labelPurchasePrice"), value: formatApiPriceLong(property.Price) };
  }

  const data = readTypeSpecificData(property);
  const minimumBid = numberValue(data.MinimumBid);
  const estimatedValue = numberValue(data.EstimatedValue);
  // TypeSpecificData ist bei Zwangsversteigerungen die fachliche Quelle. Alte
  // Seed-Datensätze konnten in Price noch den Marktwert tragen und zeigten
  // dadurch oben einen anderen Betrag als im Versteigerungsabschnitt.
  const price = minimumBid ?? estimatedValue ?? numberValue(property.Price);
  const label = minimumBid
    ? t("detail.labelMinimumBid")
    : estimatedValue
      ? t("detail.labelEstimatedValue")
      : t("detail.labelMinimumBid");
  return { label, value: price ? formatMoney(price) : t("property.priceOpen") };
}

export function getApiPropertyDetailSections(property: ApiProperty): PropertyDetailSection[] {
  const data = readTypeSpecificData(property);
  const sections = new Map<string, DetailItem[]>();
  const isForeclosure = property.Type === "Foreclosure";
  const priceFact = getApiPriceFact(property);

  add(sections, SECTIONS.basics, t("detail.labelPropertyType"), getApiPropertyTypeLabel(property.Type));
  add(sections, SECTIONS.basics, priceFact.label, priceFact.value);
  add(sections, SECTIONS.basics, t("detail.labelPostalCode"), property.PostalCode);
  add(sections, SECTIONS.basics, t("detail.labelCity"), property.City);
  add(sections, SECTIONS.basics, t("detail.labelAddress"), property.Address);

  // Bei Zwangsversteigerungen tragen die Kernfelder Edikt-Semantik (LivingAreaM2 = bebaute
  // Flaeche, PlotAreaM2 = Gesamtflaeche): als "Wohnflaeche"/"Grundstuecksflaeche" waeren die
  // Werte falsch beschriftet und stuenden doppelt neben "Gesamtflaeche"/"Bebaute Flaeche".
  add(sections, SECTIONS.areas, t("detail.labelLivingArea"), formatArea(data.LivingAreaInSquareMeters) || (isForeclosure ? "" : formatArea(property.LivingAreaM2)));
  add(sections, SECTIONS.areas, t("detail.labelPlotArea"), formatArea(data.PlotSizeInSquareMeters)
    || (isForeclosure && numberValue(data.TotalArea) ? "" : formatArea(property.PlotAreaM2)));
  add(sections, SECTIONS.areas, t("detail.labelTotalArea"), formatArea(data.TotalArea));
  add(sections, SECTIONS.areas, t("detail.labelBuildingArea"), formatArea(data.BuildingArea));

  add(sections, SECTIONS.building, t("detail.labelRooms"), positiveText(data.TotalRooms) || positiveText(data.NumberOfRooms) || positiveText(property.Rooms));
  add(sections, SECTIONS.building, t("detail.labelBedrooms"), positiveText(data.Bedrooms));
  add(sections, SECTIONS.building, t("detail.labelBathrooms"), positiveText(data.Bathrooms));
  add(sections, SECTIONS.building, t("detail.labelFloors"), positiveText(data.Floors));
  add(sections, SECTIONS.building, t("detail.labelYearBuilt"), scalar(data, "YearBuilt") || (property.YearBuilt ? String(property.YearBuilt) : ""));
  add(sections, SECTIONS.building, t("detail.labelCondition"), formatCondition(data.Condition));
  add(sections, SECTIONS.building, t("detail.labelApartmentFloor"), positiveText(data.ApartmentFloor));
  add(sections, SECTIONS.building, t("detail.labelBuildingCondition"), scalar(data, "BuildingCondition"));

  if (boolValue(data.HasGarage) === true) add(sections, SECTIONS.equipment, t("detail.labelGarage"), t("detail.yes"));
  if (boolValue(data.HasGarden) === true) add(sections, SECTIONS.equipment, t("detail.labelGarden"), t("detail.yes"));
  if (boolValue(data.HasBasement) === true) add(sections, SECTIONS.equipment, t("detail.labelBasement"), t("detail.yes"));
  if (boolValue(data.HasElevator) === true) add(sections, SECTIONS.equipment, t("detail.labelElevator"), t("detail.yes"));

  add(sections, SECTIONS.plot, t("detail.labelZoning"), formatZoning(data.Zoning) || scalar(data, "ZoningDesignation"));
  add(sections, SECTIONS.plot, t("detail.labelBuildingRights"), formatBool(data.HasBuildingRights));
  add(sections, SECTIONS.plot, t("detail.labelBuildable"), formatBool(data.IsBuildable));
  add(sections, SECTIONS.plot, t("detail.labelUtilities"), formatBool(data.HasUtilities));
  add(sections, SECTIONS.plot, t("detail.labelSoilQuality"), formatSoilQuality(data.SoilQuality));
  add(sections, SECTIONS.plot, t("detail.labelCadastralMunicipality"), scalar(data, "CadastralMunicipality"));
  add(sections, SECTIONS.plot, t("detail.labelPlotNumber"), scalar(data, "PlotNumber"));
  add(sections, SECTIONS.plot, t("detail.labelRegistrationNumber"), scalar(data, "RegistrationNumber"));

  add(sections, SECTIONS.auction, t("detail.labelCourt"), scalar(data, "CourtName"));
  add(sections, SECTIONS.auction, t("detail.labelFileNumber"), scalar(data, "FileNumber"));
  add(sections, SECTIONS.auction, t("detail.labelAuctionDate"), formatDateTime(data.AuctionDate));
  add(sections, SECTIONS.auction, t("detail.labelMinimumBid"), formatMoney(data.MinimumBid));
  add(sections, SECTIONS.auction, t("detail.labelEstimatedValue"), formatMoney(data.EstimatedValue));
  add(sections, SECTIONS.auction, t("detail.labelStatus"), formatLegalStatus(data.Status));
  add(sections, SECTIONS.auction, t("detail.labelViewingDate"), formatDateTime(data.ViewingDate));
  add(sections, SECTIONS.auction, t("detail.labelBiddingDeadline"), formatDateTime(data.BiddingDeadline));
  add(sections, SECTIONS.auction, t("detail.labelOwnershipShare"), scalar(data, "OwnershipShare"));

  // Im Editor eingetragene Edikt-URL als nutzbaren Link ausgeben (WEB-015) -
  // Gericht-synchronisierte ZV verlinken das Edikt zusaetzlich ueber die Quellen-Karte
  const edictUrl = scalar(data, "EdictUrl");
  if (/^https?:\/\//.test(edictUrl)) {
    const items = sections.get(SECTIONS.auction) ?? [];
    items.push({ label: t("detail.labelEdictUrl"), value: t("detail.openEdict"), href: edictUrl });
    sections.set(SECTIONS.auction, items);
  }

  // Bei Zwangsversteigerungen waere das Mindestgebot / bebaute Flaeche - kein Kaufpreis pro m²
  const livingArea = numberValue(property.LivingAreaM2) ?? numberValue(data.LivingAreaInSquareMeters);
  const price = numberValue(property.Price);
  if (!isForeclosure && livingArea && price) {
    add(sections, SECTIONS.costs, t("detail.labelPricePerM2"), formatMoney(price / livingArea));
  }
  add(sections, SECTIONS.basics, t("detail.labelCreatedAt"), formatApiDate(property.CreatedAt));

  return SECTION_ORDER
    .map((title) => ({ title, items: sections.get(title) ?? [] }))
    .filter((section) => section.items.length > 0);
}
