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
}

export interface DashboardDefinition {
  SchemaVersion: number;
  Title: string;
  Intro?: string | null;
  Widgets: DashboardWidget[];
  UnsupportedWishes?: string[] | null;
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
}

export interface WidgetData {
  WidgetId: string;
  Kind: string;
  Success: boolean;
  Error: string | null;
  PropertyList?: { Properties: ApiPropertyItem[]; Total: number } | null;
  StatRow?: { Tiles: { Key: string; Label: string; Value: string }[] } | null;
  Map?: { Pins: MapPin[]; Total: number; WithoutCoordinates: number; Truncated: boolean } | null;
  TextNote?: { Text: string } | null;
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
