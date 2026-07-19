import { test, expect } from "@playwright/test";

/**
 * Issue #17: Collapsible Mobile Filter
 *
 * Validates the current backend contract used by Astro and MAUI:
 * multi-value JSON filters, municipality IDs, combinations and result totals.
 */

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5292";

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

interface Municipality {
  Id: string;
  Name: string;
  PostalCode: string;
}

interface LocationsResponse {
  FederalProvinces: Array<{
    Districts: Array<{
      Municipalities: Municipality[];
    }>;
  }>;
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

let municipalities: Municipality[] | undefined;

async function getMunicipalityId(city: string): Promise<string> {
  if (!municipalities) {
    const response = await fetch(`${API_BASE}/api/locations/`);
    if (!response.ok) {
      throw new Error(
        `Get locations failed: ${response.status} ${await response.text()}`
      );
    }
    const result: LocationsResponse = await response.json();
    municipalities = result.FederalProvinces.flatMap((province) =>
      province.Districts.flatMap((district) => district.Municipalities)
    );
  }

  const municipality = municipalities.find((item) => item.Name === city);
  expect(municipality, `Municipality for ${city}`).toBeTruthy();
  return municipality!.Id;
}

const typeFilter = (type: "House" | "Land" | "Foreclosure") => ({
  PropertyTypesJson: JSON.stringify([type]),
  PageSize: "50",
});

test.describe("Issue #17: Filter functionality for collapsible mobile filter", () => {
  test("Properties endpoint returns results without filters", async () => {
    const result = await getAllProperties({ PageSize: "50" });

    expect(result).toHaveProperty("Properties");
    expect(Array.isArray(result.Properties)).toBe(true);
    expect(result.Properties.length).toBeGreaterThan(0);
  });

  test("Filter by type House returns only houses", async () => {
    const result = await getAllProperties(typeFilter("House"));

    expect(result.Properties.length).toBeGreaterThan(0);
    for (const property of result.Properties) {
      expect(property.Type).toBe("House");
    }
  });

  test("Filter by type Land returns only land properties", async () => {
    const result = await getAllProperties(typeFilter("Land"));

    expect(result.Properties.length).toBeGreaterThan(0);
    for (const property of result.Properties) {
      expect(property.Type).toBe("Land");
    }
  });

  test("Filter by type Foreclosure returns only foreclosures", async () => {
    const result = await getAllProperties(typeFilter("Foreclosure"));

    for (const property of result.Properties) {
      expect(property.Type).toBe("Foreclosure");
    }
  });

  test("Filter by municipality returns matching results", async () => {
    const all = await getAllProperties({ PageSize: "50" });
    expect(all.Properties.length).toBeGreaterThan(0);

    const city = all.Properties[0].City;
    const municipalityId = await getMunicipalityId(city);
    const filtered = await getAllProperties({
      MunicipalityIdsJson: JSON.stringify([municipalityId]),
      PageSize: "50",
    });

    expect(filtered.Properties.length).toBeGreaterThan(0);
    for (const property of filtered.Properties) {
      expect(property.City).toBe(city);
    }
  });

  test("Combined type and municipality filter returns correct results", async () => {
    const houses = await getAllProperties(typeFilter("House"));
    if (houses.Properties.length === 0) return;

    const city = houses.Properties[0].City;
    const municipalityId = await getMunicipalityId(city);
    const filtered = await getAllProperties({
      PropertyTypesJson: JSON.stringify(["House"]),
      MunicipalityIdsJson: JSON.stringify([municipalityId]),
      PageSize: "50",
    });

    for (const property of filtered.Properties) {
      expect(property.Type).toBe("House");
      expect(property.City).toBe(city);
    }
  });

  test("Filtered result total is less than or equal to unfiltered", async () => {
    const all = await getAllProperties({ PageSize: "100" });
    const housesOnly = await getAllProperties({
      PropertyTypesJson: JSON.stringify(["House"]),
      PageSize: "100",
    });

    expect(housesOnly.Total).toBeLessThanOrEqual(all.Total);
  });

  test("Each property has required fields for card display", async () => {
    const result = await getAllProperties({ PageSize: "10" });

    for (const property of result.Properties) {
      expect(property.Id).toBeTruthy();
      expect(property.Title).toBeTruthy();
      expect(property.City).toBeTruthy();
      expect(property.Price).toBeGreaterThan(0);
      expect(["House", "Land", "Foreclosure"]).toContain(property.Type);
      expect(["Private", "Broker", "PropertyManager"]).toContain(
        property.SellerType
      );
      expect(property.CreatedAt).toBeTruthy();
    }
  });
});
