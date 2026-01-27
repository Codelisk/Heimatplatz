import { test, expect } from "@playwright/test";

/**
 * Issue #7: Immobilie hinzufuegen - Testfaelle
 *
 * Tests cover:
 * 1. JWT tokens contain correct role claims (Seller vs Buyer)
 * 2. Seller can create a Haus (House) property via API
 * 3. Seller can create a Grund (Land) property via API
 * 4. Created properties have correct details and type-specific fields
 * 5. Both properties appear in "Meine Immobilien" (user properties endpoint)
 * 6. Public listing shows created properties (Home page)
 * 7. Property details contain correct data and seller info
 * 8. Property validation works (title length, price, description)
 * 9. Type-specific fields: House has rooms/livingArea/yearBuilt, Land does not
 *
 * Note: API responses use PascalCase property names and string enum values.
 */

const API_BASE = "http://localhost:5292";

const TEST_USERS = {
  buyer: { email: "test.buyer@heimatplatz.dev", password: "Test123!" },
  seller: { email: "test.seller@heimatplatz.dev", password: "Test123!" },
  both: { email: "test.both@heimatplatz.dev", password: "Test123!" },
};

// API responses use PascalCase
interface LoginResponse {
  AccessToken: string;
  RefreshToken: string;
  UserId: string;
  Email: string;
  FullName: string;
  ExpiresAt: string;
}

interface CreatePropertyResponse {
  PropertyId: string;
  Title: string;
  CreatedAt: string;
}

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

interface PropertyDetail {
  Id: string;
  Title: string;
  Address: string;
  City: string;
  PostalCode: string;
  Price: number;
  LivingAreaM2: number | null;
  PlotAreaM2: number | null;
  Rooms: number | null;
  YearBuilt: number | null;
  Type: string;
  SellerType: string;
  SellerName: string;
  Description: string | null;
  Features: string[];
  ImageUrls: string[];
  CreatedAt: string;
  InquiryType: string;
  Contacts: unknown[];
  TypeSpecificData: string | null;
}

async function apiLogin(
  email: string,
  password: string
): Promise<LoginResponse> {
  const response = await fetch(`${API_BASE}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, passwort: password }),
  });
  if (!response.ok) {
    throw new Error(
      `Login failed for ${email}: ${response.status} ${await response.text()}`
    );
  }
  return response.json();
}

async function createProperty(
  token: string,
  propertyData: Record<string, unknown>
): Promise<Response> {
  return fetch(`${API_BASE}/api/properties/`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(propertyData),
  });
}

async function getUserProperties(
  token: string
): Promise<{ Properties: PropertyListItem[] }> {
  const response = await fetch(`${API_BASE}/api/properties/user`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!response.ok) {
    throw new Error(
      `Get user properties failed: ${response.status} ${await response.text()}`
    );
  }
  return response.json();
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

async function getPropertyById(
  id: string
): Promise<{ Property: PropertyDetail }> {
  const response = await fetch(`${API_BASE}/api/properties/${id}`);
  if (!response.ok) {
    throw new Error(
      `Get property failed: ${response.status} ${await response.text()}`
    );
  }
  return response.json();
}

async function deleteProperty(
  token: string,
  propertyId: string
): Promise<void> {
  await fetch(`${API_BASE}/api/properties/${propertyId}`, {
    method: "DELETE",
    headers: { Authorization: `Bearer ${token}` },
  });
}

function decodeJwtPayload(token: string): Record<string, unknown> {
  const parts = token.split(".");
  let payload = parts[1].replace(/-/g, "+").replace(/_/g, "/");
  while (payload.length % 4 !== 0) {
    payload += "=";
  }
  return JSON.parse(Buffer.from(payload, "base64").toString("utf-8"));
}

// Unique suffix to identify test-created properties
const TEST_RUN_ID = `test-${Date.now()}`;

const HAUS_PROPERTY = {
  title: `Testhaus Familienhaus ${TEST_RUN_ID}`,
  address: "Teststrasse 42",
  city: "Linz",
  postalCode: "4020",
  price: 450000,
  type: 1, // House
  sellerType: 1, // Private
  sellerName: "Test Seller",
  description:
    "Ein wunderschoenes Familienhaus mit Garten und Garage. Ideal fuer Familien mit Kindern. Ruhige Lage nahe dem Zentrum.",
  livingAreaSquareMeters: 150,
  plotAreaSquareMeters: 600,
  rooms: 5,
  yearBuilt: 2015,
  features: ["Garten", "Garage", "Keller"],
};

const GRUND_PROPERTY = {
  title: `Testgrund Baugrundsueck ${TEST_RUN_ID}`,
  address: "Feldweg 7",
  city: "Wels",
  postalCode: "4600",
  price: 120000,
  type: 2, // Land
  sellerType: 1, // Private
  sellerName: "Test Seller",
  description:
    "Sonniges Baugrundsueck in guter Lage. Ideal fuer den Bau eines Einfamilienhauses. Alle Anschluesse vorhanden.",
  plotAreaSquareMeters: 800,
};

// Track created property IDs for cleanup
let createdPropertyIds: string[] = [];
let sellerToken: string;

test.describe("Issue #7: Immobilie hinzufuegen", () => {
  test.beforeAll(async () => {
    const loginResult = await apiLogin(
      TEST_USERS.seller.email,
      TEST_USERS.seller.password
    );
    sellerToken = loginResult.AccessToken;
  });

  test.afterAll(async () => {
    if (sellerToken && createdPropertyIds.length > 0) {
      for (const id of createdPropertyIds) {
        try {
          await deleteProperty(sellerToken, id);
        } catch {
          // Ignore cleanup errors
        }
      }
    }
  });

  test.describe("Role-based Authorization", () => {
    test("JWT token for seller contains Seller role claim", async () => {
      const decoded = decodeJwtPayload(sellerToken);
      const role = decoded.user_role;
      if (Array.isArray(role)) {
        expect(role).toContain("Seller");
      } else {
        expect(role).toBe("Seller");
      }
    });

    test("JWT token for buyer contains Buyer role but NOT Seller", async () => {
      const buyerLogin = await apiLogin(
        TEST_USERS.buyer.email,
        TEST_USERS.buyer.password
      );
      const decoded = decodeJwtPayload(buyerLogin.AccessToken);
      const role = decoded.user_role;
      if (Array.isArray(role)) {
        expect(role).not.toContain("Seller");
        expect(role).toContain("Buyer");
      } else {
        expect(role).toBe("Buyer");
      }
    });

    test("JWT token for 'both' user contains Buyer AND Seller roles", async () => {
      const bothLogin = await apiLogin(
        TEST_USERS.both.email,
        TEST_USERS.both.password
      );
      const decoded = decodeJwtPayload(bothLogin.AccessToken);
      const role = decoded.user_role;
      expect(Array.isArray(role)).toBe(true);
      expect(role).toContain("Buyer");
      expect(role).toContain("Seller");
    });

    test("Seller can access user properties endpoint", async () => {
      const result = await getUserProperties(sellerToken);
      expect(result).toHaveProperty("Properties");
      expect(Array.isArray(result.Properties)).toBe(true);
    });
  });

  test.describe("Create Haus (House) Property", () => {
    let hausPropertyId: string;

    test("Seller can create a Haus property via API", async () => {
      const response = await createProperty(sellerToken, HAUS_PROPERTY);
      expect(response.status).toBe(200);

      const result: CreatePropertyResponse = await response.json();
      expect(result.PropertyId).toBeTruthy();
      expect(result.Title).toBe(HAUS_PROPERTY.title);
      expect(result.CreatedAt).toBeTruthy();

      hausPropertyId = result.PropertyId;
      createdPropertyIds.push(hausPropertyId);
    });

    test("Created Haus property has correct details", async () => {
      expect(hausPropertyId).toBeTruthy();

      const result = await getPropertyById(hausPropertyId);
      const property = result.Property;

      expect(property).toBeTruthy();
      expect(property.Title).toBe(HAUS_PROPERTY.title);
      expect(property.Address).toBe(HAUS_PROPERTY.address);
      expect(property.City).toBe(HAUS_PROPERTY.city);
      expect(property.PostalCode).toBe(HAUS_PROPERTY.postalCode);
      expect(property.Price).toBe(HAUS_PROPERTY.price);
      expect(property.Type).toBe("House");
      expect(property.SellerType).toBe("Private");
      expect(property.SellerName).toBe(HAUS_PROPERTY.sellerName);
      expect(property.Description).toBe(HAUS_PROPERTY.description);
      expect(property.LivingAreaM2).toBe(
        HAUS_PROPERTY.livingAreaSquareMeters
      );
      expect(property.PlotAreaM2).toBe(HAUS_PROPERTY.plotAreaSquareMeters);
      expect(property.Rooms).toBe(HAUS_PROPERTY.rooms);
      expect(property.YearBuilt).toBe(HAUS_PROPERTY.yearBuilt);
    });

    test("Created Haus property appears in seller user properties", async () => {
      expect(hausPropertyId).toBeTruthy();

      const result = await getUserProperties(sellerToken);
      const found = result.Properties.find((p) => p.Id === hausPropertyId);

      expect(found).toBeTruthy();
      expect(found!.Title).toBe(HAUS_PROPERTY.title);
      expect(found!.Type).toBe("House");
      expect(found!.Price).toBe(HAUS_PROPERTY.price);
      expect(found!.City).toBe(HAUS_PROPERTY.city);
    });
  });

  test.describe("Create Grund (Land) Property", () => {
    let grundPropertyId: string;

    test("Seller can create a Grund property via API", async () => {
      const response = await createProperty(sellerToken, GRUND_PROPERTY);
      expect(response.status).toBe(200);

      const result: CreatePropertyResponse = await response.json();
      expect(result.PropertyId).toBeTruthy();
      expect(result.Title).toBe(GRUND_PROPERTY.title);
      expect(result.CreatedAt).toBeTruthy();

      grundPropertyId = result.PropertyId;
      createdPropertyIds.push(grundPropertyId);
    });

    test("Created Grund property has correct details", async () => {
      expect(grundPropertyId).toBeTruthy();

      const result = await getPropertyById(grundPropertyId);
      const property = result.Property;

      expect(property).toBeTruthy();
      expect(property.Title).toBe(GRUND_PROPERTY.title);
      expect(property.Address).toBe(GRUND_PROPERTY.address);
      expect(property.City).toBe(GRUND_PROPERTY.city);
      expect(property.PostalCode).toBe(GRUND_PROPERTY.postalCode);
      expect(property.Price).toBe(GRUND_PROPERTY.price);
      expect(property.Type).toBe("Land");
      expect(property.SellerType).toBe("Private");
      expect(property.SellerName).toBe(GRUND_PROPERTY.sellerName);
      expect(property.Description).toBe(GRUND_PROPERTY.description);
      expect(property.PlotAreaM2).toBe(GRUND_PROPERTY.plotAreaSquareMeters);
      // Land should NOT have house-specific fields
      expect(property.LivingAreaM2).toBeNull();
      expect(property.Rooms).toBeNull();
      expect(property.YearBuilt).toBeNull();
    });

    test("Created Grund property appears in seller user properties", async () => {
      expect(grundPropertyId).toBeTruthy();

      const result = await getUserProperties(sellerToken);
      const found = result.Properties.find((p) => p.Id === grundPropertyId);

      expect(found).toBeTruthy();
      expect(found!.Title).toBe(GRUND_PROPERTY.title);
      expect(found!.Type).toBe("Land");
      expect(found!.Price).toBe(GRUND_PROPERTY.price);
      expect(found!.City).toBe(GRUND_PROPERTY.city);
    });
  });

  test.describe("Both Properties in Meine Immobilien", () => {
    test("Seller user properties contain both Haus and Grund", async () => {
      const result = await getUserProperties(sellerToken);

      const testProperties = result.Properties.filter((p) =>
        p.Title.includes(TEST_RUN_ID)
      );

      expect(testProperties.length).toBeGreaterThanOrEqual(2);

      const hausProperty = testProperties.find((p) => p.Type === "House");
      const grundProperty = testProperties.find((p) => p.Type === "Land");

      expect(hausProperty).toBeTruthy();
      expect(hausProperty!.Title).toBe(HAUS_PROPERTY.title);

      expect(grundProperty).toBeTruthy();
      expect(grundProperty!.Title).toBe(GRUND_PROPERTY.title);
    });
  });

  test.describe("Buyer sees Properties on Home Page (Public Listing)", () => {
    test("Public properties endpoint returns created Haus property", async () => {
      const result = await getAllProperties({ Take: "50" });

      const hausProperty = result.Properties.find(
        (p) => p.Title === HAUS_PROPERTY.title
      );

      expect(hausProperty).toBeTruthy();
      expect(hausProperty!.Price).toBe(HAUS_PROPERTY.price);
      expect(hausProperty!.City).toBe(HAUS_PROPERTY.city);
      expect(hausProperty!.Type).toBe("House");
      expect(hausProperty!.SellerName).toBe(HAUS_PROPERTY.sellerName);
      expect(hausProperty!.SellerType).toBe("Private");
    });

    test("Public properties endpoint returns created Grund property", async () => {
      const result = await getAllProperties({ Take: "50" });

      const grundProperty = result.Properties.find(
        (p) => p.Title === GRUND_PROPERTY.title
      );

      expect(grundProperty).toBeTruthy();
      expect(grundProperty!.Price).toBe(GRUND_PROPERTY.price);
      expect(grundProperty!.City).toBe(GRUND_PROPERTY.city);
      expect(grundProperty!.Type).toBe("Land");
      expect(grundProperty!.SellerName).toBe(GRUND_PROPERTY.sellerName);
    });

    test("Properties can be filtered by type House", async () => {
      const result = await getAllProperties({ Type: "1", Take: "50" });

      const hausProperty = result.Properties.find(
        (p) => p.Title === HAUS_PROPERTY.title
      );
      expect(hausProperty).toBeTruthy();

      // All returned should be House type
      for (const p of result.Properties) {
        expect(p.Type).toBe("House");
      }
    });

    test("Properties can be filtered by type Land", async () => {
      const result = await getAllProperties({ Type: "2", Take: "50" });

      const grundProperty = result.Properties.find(
        (p) => p.Title === GRUND_PROPERTY.title
      );
      expect(grundProperty).toBeTruthy();

      // All returned should be Land type
      for (const p of result.Properties) {
        expect(p.Type).toBe("Land");
      }
    });

    test("Buyer can view Haus property details with seller info", async () => {
      // Find the Haus property from public listing
      const allProperties = await getAllProperties({ Take: "50" });
      const hausProperty = allProperties.Properties.find(
        (p) => p.Title === HAUS_PROPERTY.title
      );
      expect(hausProperty).toBeTruthy();

      // Get full details (public endpoint, no auth needed)
      const detail = await getPropertyById(hausProperty!.Id);
      const property = detail.Property;

      expect(property).toBeTruthy();
      expect(property.Title).toBe(HAUS_PROPERTY.title);
      expect(property.SellerName).toBe(HAUS_PROPERTY.sellerName);
      expect(property.SellerType).toBe("Private");
      expect(property.Price).toBe(HAUS_PROPERTY.price);
      expect(property.LivingAreaM2).toBe(
        HAUS_PROPERTY.livingAreaSquareMeters
      );
      expect(property.Rooms).toBe(HAUS_PROPERTY.rooms);
      expect(property.YearBuilt).toBe(HAUS_PROPERTY.yearBuilt);
      expect(property.Description).toBe(HAUS_PROPERTY.description);
    });

    test("Buyer can view Grund property details with seller info", async () => {
      const allProperties = await getAllProperties({ Take: "50" });
      const grundProperty = allProperties.Properties.find(
        (p) => p.Title === GRUND_PROPERTY.title
      );
      expect(grundProperty).toBeTruthy();

      const detail = await getPropertyById(grundProperty!.Id);
      const property = detail.Property;

      expect(property).toBeTruthy();
      expect(property.Title).toBe(GRUND_PROPERTY.title);
      expect(property.SellerName).toBe(GRUND_PROPERTY.sellerName);
      expect(property.SellerType).toBe("Private");
      expect(property.Price).toBe(GRUND_PROPERTY.price);
      expect(property.PlotAreaM2).toBe(GRUND_PROPERTY.plotAreaSquareMeters);
      expect(property.Description).toBe(GRUND_PROPERTY.description);
      // Land property should NOT have house-specific fields
      expect(property.LivingAreaM2).toBeNull();
      expect(property.Rooms).toBeNull();
      expect(property.YearBuilt).toBeNull();
    });
  });

  test.describe("Property Validation", () => {
    test("Cannot create property with title too short", async () => {
      const response = await createProperty(sellerToken, {
        ...HAUS_PROPERTY,
        title: "Short",
      });
      expect(response.status).toBeGreaterThanOrEqual(400);
    });

    test("Cannot create property with negative price", async () => {
      const response = await createProperty(sellerToken, {
        ...HAUS_PROPERTY,
        title: `Validation Test Negative ${TEST_RUN_ID}`,
        price: -1,
      });
      expect(response.status).toBeGreaterThanOrEqual(400);
    });

    test("Cannot create property with description too short", async () => {
      const response = await createProperty(sellerToken, {
        ...HAUS_PROPERTY,
        title: `Validation Test Desc ${TEST_RUN_ID}`,
        description: "Too short",
      });
      expect(response.status).toBeGreaterThanOrEqual(400);
    });
  });

  test.describe("Property Type Specific Fields", () => {
    test("House property has living area, rooms, and year built", async () => {
      const allProperties = await getAllProperties({ Take: "50" });
      const hausProperty = allProperties.Properties.find(
        (p) => p.Title === HAUS_PROPERTY.title
      );
      expect(hausProperty).toBeTruthy();

      const detail = await getPropertyById(hausProperty!.Id);
      const property = detail.Property;

      expect(property.LivingAreaM2).toBe(
        HAUS_PROPERTY.livingAreaSquareMeters
      );
      expect(property.PlotAreaM2).toBe(HAUS_PROPERTY.plotAreaSquareMeters);
      expect(property.Rooms).toBe(HAUS_PROPERTY.rooms);
      expect(property.YearBuilt).toBe(HAUS_PROPERTY.yearBuilt);
    });

    test("Land property only has plot area, no house-specific fields", async () => {
      const allProperties = await getAllProperties({ Take: "50" });
      const grundProperty = allProperties.Properties.find(
        (p) => p.Title === GRUND_PROPERTY.title
      );
      expect(grundProperty).toBeTruthy();

      const detail = await getPropertyById(grundProperty!.Id);
      const property = detail.Property;

      expect(property.PlotAreaM2).toBe(GRUND_PROPERTY.plotAreaSquareMeters);
      expect(property.LivingAreaM2).toBeNull();
      expect(property.Rooms).toBeNull();
      expect(property.YearBuilt).toBeNull();
    });
  });
});
