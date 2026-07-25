/**
 * Kartenansicht der Immobiliensuche: Typen und Query-Helfer fuer
 * GET /api/properties/map-pins. Der Endpoint nimmt dieselben Filter-Parameter
 * wie die Listen-Suche (gemeinsame Filterlogik im Backend) - der Karten-Query
 * wird deshalb direkt aus dem Listen-Query abgeleitet statt separat gebaut.
 *
 * Client-sicher: keine Server-Imports.
 */

export type ApiMapPin = {
  Id: string;
  Latitude: number;
  Longitude: number;
  /** true = bewusst ungenaue Lage (Ortszentrum + serverseitige Streuung) */
  IsApproximate: boolean;
  Type: "House" | "Land" | "Foreclosure" | number;
  SellerType: string | number;
  Price: number | string;
  Title: string;
  City: string;
  PostalCode: string;
  MunicipalityId: string;
  ImageUrl?: string | null;
  AuctionDate?: string | null;
};

export type ApiMapPinsResponse = {
  Pins?: ApiMapPin[] | null;
  Total?: number;
  WithoutCoordinates?: number;
  Truncated?: boolean;
};

/**
 * Listen-Query (buildPropertySearchQuery) → Karten-Query: Paging und Sortierung
 * betreffen nur die Liste, alle Filter bleiben identisch. So koennen Karte und
 * Liste nie auseinanderlaufen.
 */
export function buildMapPinsQuery(searchQuery: string): string {
  const params = new URLSearchParams(searchQuery);
  ["Page", "PageSize", "SortBy", "SortDescending"].forEach((key) => params.delete(key));
  return params.toString();
}
