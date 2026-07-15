import { formatApiDate, formatApiPriceLong, getApiPropertyTypeLabel, type ApiProperty } from "./live-api";

type DetailItem = {
  label: string;
  value: string;
};

export type PropertyDetailSection = {
  title: string;
  items: DetailItem[];
};

type TypeSpecificData = Record<string, unknown>;

const SECTION_ORDER = [
  "Basisdaten",
  "Flächen",
  "Gebäude",
  "Ausstattung",
  "Grundstück",
  "Versteigerung",
  "Kosten",
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
  return bool ? "Ja" : "Nein";
}

function formatCondition(value: unknown) {
  const labels: Record<string, string> = {
    LikeNew: "Neuwertig",
    Good: "Gut",
    Average: "Durchschnittlich",
    NeedsRenovation: "Sanierungsbedürftig",
  };
  const key = scalar({ value }, "value");
  return labels[key] ?? key;
}

function formatZoning(value: unknown) {
  const labels: Record<string, string> = {
    Residential: "Wohngebiet",
    Commercial: "Gewerbegebiet",
    Industrial: "Industriegebiet",
    Agricultural: "Landwirtschaft",
    Mixed: "Mischgebiet",
  };
  const key = scalar({ value }, "value");
  return labels[key] ?? key;
}

function formatSoilQuality(value: unknown) {
  const labels: Record<string, string> = {
    High: "Hoch",
    Medium: "Mittel",
    Low: "Niedrig",
  };
  const key = scalar({ value }, "value");
  return labels[key] ?? key;
}

function formatLegalStatus(value: unknown) {
  const labels: Record<string, string> = {
    Pending: "Anhängig",
    Scheduled: "Terminiert",
    InProgress: "Laufend",
    Completed: "Abgeschlossen",
    Cancelled: "Aufgehoben",
  };
  const key = scalar({ value }, "value");
  return labels[key] ?? key;
}

export function getApiPropertyDetailSections(property: ApiProperty): PropertyDetailSection[] {
  const data = readTypeSpecificData(property);
  const sections = new Map<string, DetailItem[]>();

  add(sections, "Basisdaten", "Immobilienart", getApiPropertyTypeLabel(property.Type, property));
  add(sections, "Basisdaten", "Kaufpreis", formatApiPriceLong(property.Price));
  add(sections, "Basisdaten", "PLZ", property.PostalCode);
  add(sections, "Basisdaten", "Ort", property.City);
  add(sections, "Basisdaten", "Adresse", property.Address);

  add(sections, "Flächen", "Wohnfläche", formatArea(data.LivingAreaInSquareMeters) || formatArea(property.LivingAreaM2));
  add(sections, "Flächen", "Grundstücksfläche", formatArea(data.PlotSizeInSquareMeters) || formatArea(property.PlotAreaM2));
  add(sections, "Flächen", "Gesamtfläche", formatArea(data.TotalArea));
  add(sections, "Flächen", "Bebaute Fläche", formatArea(data.BuildingArea));

  add(sections, "Gebäude", "Zimmer", positiveText(data.TotalRooms) || positiveText(data.NumberOfRooms) || positiveText(property.Rooms));
  add(sections, "Gebäude", "Schlafzimmer", positiveText(data.Bedrooms));
  add(sections, "Gebäude", "Badezimmer", positiveText(data.Bathrooms));
  add(sections, "Gebäude", "Stockwerke", positiveText(data.Floors));
  add(sections, "Gebäude", "Baujahr", scalar(data, "YearBuilt") || (property.YearBuilt ? String(property.YearBuilt) : ""));
  add(sections, "Gebäude", "Zustand", formatCondition(data.Condition));
  add(sections, "Gebäude", "Etage", positiveText(data.ApartmentFloor));
  add(sections, "Gebäude", "Gebäudezustand", scalar(data, "BuildingCondition"));

  if (boolValue(data.HasGarage) === true) add(sections, "Ausstattung", "Garage", "Ja");
  if (boolValue(data.HasGarden) === true) add(sections, "Ausstattung", "Garten", "Ja");
  if (boolValue(data.HasBasement) === true) add(sections, "Ausstattung", "Keller", "Ja");
  if (boolValue(data.HasElevator) === true) add(sections, "Ausstattung", "Aufzug", "Ja");

  add(sections, "Grundstück", "Widmung", formatZoning(data.Zoning) || scalar(data, "ZoningDesignation"));
  add(sections, "Grundstück", "Baurecht", formatBool(data.HasBuildingRights));
  add(sections, "Grundstück", "Bebaubar", formatBool(data.IsBuildable));
  add(sections, "Grundstück", "Versorgung", formatBool(data.HasUtilities));
  add(sections, "Grundstück", "Bodenqualität", formatSoilQuality(data.SoilQuality));
  add(sections, "Grundstück", "Katastralgemeinde", scalar(data, "CadastralMunicipality"));
  add(sections, "Grundstück", "Grundstücksnummer", scalar(data, "PlotNumber"));
  add(sections, "Grundstück", "Einlagezahl", scalar(data, "RegistrationNumber"));

  add(sections, "Versteigerung", "Gericht", scalar(data, "CourtName"));
  add(sections, "Versteigerung", "Aktenzeichen", scalar(data, "FileNumber"));
  add(sections, "Versteigerung", "Termin", formatDateTime(data.AuctionDate));
  add(sections, "Versteigerung", "Mindestgebot", formatMoney(data.MinimumBid));
  add(sections, "Versteigerung", "Schätzwert", formatMoney(data.EstimatedValue));
  add(sections, "Versteigerung", "Status", formatLegalStatus(data.Status));
  add(sections, "Versteigerung", "Besichtigung", formatDateTime(data.ViewingDate));
  add(sections, "Versteigerung", "Bietfrist", formatDateTime(data.BiddingDeadline));
  add(sections, "Versteigerung", "Eigentumsanteil", scalar(data, "OwnershipShare"));

  const livingArea = numberValue(property.LivingAreaM2) ?? numberValue(data.LivingAreaInSquareMeters);
  const price = numberValue(property.Price);
  if (livingArea && price) {
    add(sections, "Kosten", "Preis / m²", formatMoney(price / livingArea));
  }
  add(sections, "Basisdaten", "Eingestellt am", formatApiDate(property.CreatedAt));

  return SECTION_ORDER
    .map((title) => ({ title, items: sections.get(title) ?? [] }))
    .filter((section) => section.items.length > 0);
}
