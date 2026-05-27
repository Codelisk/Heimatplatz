export type PropertyType = "apartment" | "house" | "land" | "foreclosure";

export interface PropertySummary {
  id: string;
  slug: string;
  title: string;
  description: string;
  type: PropertyType;
  typeLabel: string;
  location: string;
  region: string;
  regionSlug: string;
  address: string;
  postalCode: string;
  price: number;
  livingArea?: number;
  plotArea?: number;
  rooms?: number;
  imageUrl: string;
  imageAlt: string;
  sellerType: "private" | "agent" | "bank" | "court";
  sellerLabel: string;
  sellerName: string;
  source: string;
  originalUrl?: string;
  updatedAt: string;
  highlights: string[];
  features: string[];
}
