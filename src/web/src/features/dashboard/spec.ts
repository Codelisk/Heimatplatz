/**
 * Typen und tolerante Guards der KI-Übersicht ("Meine Übersicht").
 *
 * Spiegelt die C#-Contracts aus Heimatplatz.Api.Features.Dashboards.Contracts 1:1
 * (PascalCase wie alle API-Antworten). Client-safe: KEINE Server-Imports — die
 * Datei wird ausschließlich vom Browser-Island der Seite /meine-uebersicht/
 * gebundelt (Muster search-query.ts).
 *
 * Tolerant-Reader-Regel: unbekannte Widget-Kinds werden übersprungen, unbekannte
 * Felder ignoriert — API und Web dürfen unabhängig deployen.
 */

export interface DashboardSummary {
  Id: string;
  Title: string;
  Status: DashboardStatusValue;
  CreatedAt: string;
  UpdatedAt: string | null;
  ViewType?: string | null;
}

/** Ansichts-Typ normalisieren; alles Unbekannte gilt als Dashboard */
export function normalizeViewType(value: string | null | undefined): "dashboard" | "list" {
  return value?.toLowerCase() === "list" ? "list" : "dashboard";
}

export interface DashboardResponse {
  Id: string;
  Title: string;
  Status: DashboardStatusValue;
  Error: string | null;
  Definition: DashboardDefinition | null;
  CanRevert: boolean;
  GenerationRequestedAt: string | null;
  GenerationCompletedAt: string | null;
  ViewType?: string | null;
}

export interface DashboardDefinition {
  SchemaVersion: number;
  Title: string;
  Intro?: string | null;
  Widgets: DashboardWidget[];
  Detail?: DashboardDetailSpec | null;
  UnsupportedWishes?: string[] | null;
}

/** Personalisierte Detail-Overlay: Sektions-Reihenfolge + Fakten-Felder */
export interface DashboardDetailSpec {
  Sections?: string[] | null;
  Fields?: string[] | null;
}

export interface DashboardWidget {
  Id: string;
  Kind: string;
  Size?: string | null;
  Title?: string | null;
  Query?: DashboardPropertyQuery | null;
  Options?: DashboardWidgetOptions | null;
}

export interface DashboardPropertyQuery {
  Types?: string[] | null;
  Locations?: string[] | null;
  PriceMin?: number | null;
  PriceMax?: number | null;
  Limit?: number | null;
  Sort?: string | null;
}

export interface DashboardWidgetOptions {
  Variant?: string | null;
  Tiles?: string[] | null;
  Text?: string | null;
  Chart?: string | null;
  /** Geordnete Feld-Schlüssel aus dem Feld-Katalog (übersteuert das Variant-Preset) */
  Fields?: string[] | null;
}

export interface WidgetData {
  WidgetId: string;
  Kind: string;
  Success: boolean;
  Error: string | null;
  PropertyList?: { Properties: ApiPropertyItem[]; Total: number; Page?: number; PageSize?: number } | null;
  StatRow?: { Tiles: { Key: string; Label: string; Value: string }[] } | null;
  Map?: { Pins: MapPin[]; Total: number; WithoutCoordinates: number; Truncated: boolean } | null;
  TextNote?: { Text: string } | null;
  Chart?: {
    ChartKind: string;
    ImageLight: string;
    ImageDark: string;
    AltText: string;
    Caption: string | null;
  } | null;
}

/** Reduzierte Sicht auf PropertyListItemDto — nur die von den Widgets gerenderten Felder */
export interface ApiPropertyItem {
  Id: string;
  Title: string;
  Address: string;
  City: string;
  PostalCode: string;
  Price: number;
  LivingAreaM2: number | null;
  PlotAreaM2: number | null;
  Rooms: number | null;
  Type: number | string;
  SellerType: number | string;
  SellerName: string;
  ImageUrls: string[];
  CreatedAt: string;
  AuctionDate: string | null;
}

export interface MapPin {
  Id: string;
  Latitude: number;
  Longitude: number;
  IsApproximate: boolean;
  Type: number | string;
  Price: number;
  Title: string;
  City: string;
}

/** Enums kommen je nach Serializer als Zahl oder Name — beides normalisieren */
export type DashboardStatusValue = number | string;

export type DashboardStatus = "none" | "queued" | "inProgress" | "finished" | "failed";

export function normalizeStatus(value: DashboardStatusValue | undefined | null): DashboardStatus {
  switch (typeof value === "string" ? value.toLowerCase() : value) {
    case 1:
    case "queued":
      return "queued";
    case 2:
    case "inprogress":
      return "inProgress";
    case 3:
    case "finished":
      return "finished";
    case 4:
    case "failed":
      return "failed";
    default:
      return "none";
  }
}

export type PropertyTypeSlug = "house" | "land" | "foreclosure" | "unknown";

export function normalizePropertyType(value: number | string | undefined | null): PropertyTypeSlug {
  switch (typeof value === "string" ? value.toLowerCase() : value) {
    case 1:
    case "house":
      return "house";
    case 2:
    case "land":
      return "land";
    case 3:
    case "foreclosure":
      return "foreclosure";
    default:
      return "unknown";
  }
}

/** Semantische Widget-Größe → Spalten im 12er-Grid (Mobil stapelt ohnehin) */
export function sizeToSpanClass(size: string | null | undefined): string {
  switch (size?.toLowerCase()) {
    case "s":
      return "lg:col-span-4";
    case "m":
      return "lg:col-span-6";
    case "l":
      return "lg:col-span-8";
    default:
      return "lg:col-span-12";
  }
}

/** Preisformat-Kanon "€ 520.000" — bewusst ohne Intl (ICU-NBSP-Falle) */
export function formatDashboardPrice(value: number): string {
  const rounded = Math.round(value);
  return `€ ${rounded.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ".")}`;
}

/** deutsches Kurzdatum dd.MM.yyyy */
export function formatDashboardDate(iso: string | null | undefined): string | null {
  if (!iso) return null;
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return null;
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${pad(date.getDate())}.${pad(date.getMonth() + 1)}.${date.getFullYear()}`;
}

export function propertyTypeLabel(value: number | string | undefined | null): string {
  const type = normalizePropertyType(value);
  return type === "land" ? "Grundstück" : type === "foreclosure" ? "Zwangsversteigerung" : "Haus";
}

/**
 * Der Feld-Katalog aus Frontend-Sicht (Werte kommen aus PropertyListItemDto).
 * EINE Implementierung für Client-Templates UND SSR-Renderer — die Schlüssel
 * müssen zum DashboardFieldCatalog im Backend passen.
 * Bewusst feste Labels statt dynamischer t()-Keys (Kanon marketing-labels).
 */
export const FIELD_LABELS: Record<string, string> = {
  foto: "Foto",
  titel: "Titel",
  preis: "Preis",
  "preis-pro-m2": "Preis pro m²",
  typ: "Objektart",
  wohnflaeche: "Wohnfläche",
  grundflaeche: "Grundfläche",
  zimmer: "Zimmer",
  ort: "Ort",
  strasse: "Straße",
  anbieter: "Anbieter",
  baujahr: "Baujahr",
  "eingestellt-am": "Eingestellt am",
  "zv-termin": "Versteigerungstermin",
};

/** Anzeigewert eines Feldes im LISTEN-Kontext; null = Wert fehlt (Zeile weglassen) */
export function listFieldValue(item: ApiPropertyItem, key: string): string | null {
  switch (key) {
    case "titel":
      return item.Title || null;
    case "preis":
      return formatDashboardPrice(item.Price);
    case "typ":
      return propertyTypeLabel(item.Type);
    case "wohnflaeche":
      return item.LivingAreaM2 ? `${item.LivingAreaM2} m²` : null;
    case "grundflaeche":
      return item.PlotAreaM2 ? `${item.PlotAreaM2} m² Grund` : null;
    case "zimmer":
      return item.Rooms ? `${item.Rooms} Zimmer` : null;
    case "ort":
      return [item.PostalCode, item.City].filter(Boolean).join(" ") || null;
    case "strasse":
      return item.Address?.trim() || null;
    case "anbieter":
      return item.SellerName?.trim() || null;
    case "eingestellt-am":
      return formatDashboardDate(item.CreatedAt);
    case "zv-termin":
      return formatDashboardDate(item.AuctionDate);
    default:
      return null;
  }
}

/** Detail-DTO der Overlay (GET /api/properties/{id}, PropertyDto) */
export interface ApiPropertyDetail extends ApiPropertyItem {
  YearBuilt: number | null;
  Description: string | null;
  Features: string[] | null;
  Contacts: { Name: string | null; Email: string | null; Phone: string | null }[] | null;
  PriceLabel?: string | null;
  PricePerSquareMeter?: number | null;
  PreviewImageUrls?: string[] | null;
}

export const DEFAULT_DETAIL_SECTIONS = ["gallery", "facts", "description", "features", "contact"];
export const DEFAULT_DETAIL_FIELDS = [
  "preis",
  "preis-pro-m2",
  "wohnflaeche",
  "grundflaeche",
  "zimmer",
  "baujahr",
  "ort",
  "strasse",
  "anbieter",
  "eingestellt-am",
];

/** Fakten-Zeile der Detail-Overlay; null = Wert fehlt (Zeile weglassen) */
export function detailFieldRow(detail: ApiPropertyDetail, key: string): { label: string; value: string } | null {
  if (key === "preis")
    return { label: detail.PriceLabel?.trim() || "Preis", value: formatDashboardPrice(detail.Price) };
  if (key === "preis-pro-m2")
    return detail.PricePerSquareMeter
      ? { label: FIELD_LABELS[key], value: `${formatDashboardPrice(detail.PricePerSquareMeter)} / m²` }
      : null;
  if (key === "baujahr")
    return detail.YearBuilt ? { label: FIELD_LABELS[key], value: String(detail.YearBuilt) } : null;
  // Ohne Einheiten-Dopplung zum Label ("Zimmer: 5", nicht "Zimmer: 5 Zimmer")
  if (key === "zimmer")
    return detail.Rooms ? { label: FIELD_LABELS[key], value: String(detail.Rooms) } : null;
  if (key === "grundflaeche")
    return detail.PlotAreaM2 ? { label: FIELD_LABELS[key], value: `${detail.PlotAreaM2} m²` } : null;

  const value = listFieldValue(detail, key);
  return value ? { label: FIELD_LABELS[key] ?? key, value } : null;
}
