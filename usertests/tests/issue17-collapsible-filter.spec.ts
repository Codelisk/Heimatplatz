import { test, expect } from "@playwright/test";

/**
 * Issue #17: Collapsible Mobile Filter
 *
 * Tests cover:
 * 1. Properties API supports filtering by type (used by mobile filter chips)
 * 2. Properties API supports filtering by city (used by OrtPicker)
 * 3. Multiple filter combinations return correct results
 * 4. Filter results count is consistent
 *
 * Note: The collapsible UI behavior (expand/collapse toggle) is a pure
 * frontend feature in the Uno XAML app and cannot be tested via API.
 * These tests validate the filter backend that powers the mobile filter bar.
 */

const API_BASE = "http://localhost:5292";

interface PropertyListItem {
  Id: string;
  Title: string;
  Address: string;
  City: string;
  Price: number;
  LivingAreaM2: number | null;
  PlotAreaM2: number | null;
  Rooms: number | null;
  Type: string;
  SellerType: string;
  SellerName: string;
  ImageUrls: string[];
  CreatedAt: string;
  InquiryType: string;
}

async function getAllProperties(
  params?: Record<string, string>
): Promise<{ Properties: PropertyListItem[]; Total: number }> {
  const query = params ? "?" + new URLSearchParams(params).toString() : "";
  const response = await fetch(`${API_BASE}/api/properties/${query}`);
  if (!response.ok) {
    throw new Error(
      `Get properties failed: ${response.status} ${await response.text()}`
    );
  }
  return response.json();
}

test.describe("Issue #17: Filter functionality for collapsible mobile filter", () => {
  test("Properties endpoint returns results without filters", async () => {
    const result = await getAllProperties({ Take: "50" });

    expect(result).toHaveProperty("Properties");
    expect(Array.isArray(result.Properties)).toBe(true);
    expect(result.Properties.length).toBeGreaterThan(0);
  });

  test("Filter by type House returns only houses", async () => {
    const result = await getAllProperties({ Type: "1", Take: "50" });

    expect(result.Properties.length).toBeGreaterThan(0);
    for (const p of result.Properties) {
      expect(p.Type).toBe("House");
    }
  });

  test("Filter by type Land returns only land properties", async () => {
    const result = await getAllProperties({ Type: "2", Take: "50" });

    expect(result.Properties.length).toBeGreaterThan(0);
    for (const p of result.Properties) {
      expect(p.Type).toBe("Land");
    }
  });

  test("Filter by type Foreclosure returns only foreclosures", async () => {
    const result = await getAllProperties({ Type: "3", Take: "50" });

    // Foreclosures may or may not exist in seed data
    for (const p of result.Properties) {
      expect(p.Type).toBe("Foreclosure");
    }
  });

  test("Filter by city returns matching results", async () => {
    // First get all to find a valid city
    const all = await getAllProperties({ Take: "50" });
    expect(all.Properties.length).toBeGreaterThan(0);

    const city = all.Properties[0].City;
    const filtered = await getAllProperties({ City: city, Take: "50" });

    expect(filtered.Properties.length).toBeGreaterThan(0);
    for (const p of filtered.Properties) {
      expect(p.City).toBe(city);
    }
  });

  test("Combined filter by type and city returns correct results", async () => {
    // Get all houses
    const houses = await getAllProperties({ Type: "1", Take: "50" });
    if (houses.Properties.length === 0) return;

    const city = houses.Properties[0].City;
    const filtered = await getAllProperties({
      Type: "1",
      City: city,
      Take: "50",
    });

    for (const p of filtered.Properties) {
      expect(p.Type).toBe("House");
      expect(p.City).toBe(city);
    }
  });

  test("Filtered result count is less than or equal to unfiltered", async () => {
    const all = await getAllProperties({ Take: "100" });
    const housesOnly = await getAllProperties({ Type: "1", Take: "100" });

    expect(housesOnly.Properties.length).toBeLessThanOrEqual(
      all.Properties.length
    );
  });

  test("Each property has required fields for card display", async () => {
    const result = await getAllProperties({ Take: "10" });

    for (const p of result.Properties) {
      expect(p.Id).toBeTruthy();
      expect(p.Title).toBeTruthy();
      expect(p.City).toBeTruthy();
      expect(p.Price).toBeGreaterThan(0);
      expect(["House", "Land", "Foreclosure"]).toContain(p.Type);
      expect(["Private", "Broker", "Portal"]).toContain(p.SellerType);
      expect(p.CreatedAt).toBeTruthy();
    }
  });
});
