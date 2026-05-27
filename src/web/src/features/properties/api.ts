import { apiRequest } from "@/lib/api/client";
import type { PropertySummary } from "./types";

export interface PropertySearchParams {
  query?: string;
  location?: string;
  type?: string;
  maxPrice?: number;
}

export async function searchProperties(params: PropertySearchParams = {}) {
  const searchParams = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") {
      searchParams.set(key, String(value));
    }
  }

  const query = searchParams.toString();
  return apiRequest<PropertySummary[]>(`/api/properties${query ? `?${query}` : ""}`);
}
