import type { APIRoute } from "astro";
import {
  adminApiPost,
  type PartnerLogoUploadResponse,
  type PartnerSaveResponse,
} from "@/lib/server/admin-api";
import { buildRedirect, invalidatePartnersCache, optional, rejectCrossSite } from "./_shared";

// Logos sind kleine Grafiken - deutlich strenger als die 60-MB-Grenze der Inserats-Fotos
const MAX_LOGO_BYTES = 10 * 1024 * 1024;

/**
 * Server-seitiger Proxy (Post-Redirect-Get) fuer Anlegen/Bearbeiten eines Partners.
 * Ein mitgeschicktes Logo wird zuerst ueber /api/admin/partners/logo hochgeladen
 * (Base64, gleiche Pipeline wie Inserats-Fotos), die URL landet dann im Save.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();

  // Logo bestimmen: neuer Upload > "entfernen"-Haken > bestehende URL (hidden field)
  let logoUrl = optional(form, "logoUrl");
  if (form.get("logoRemove") === "on") logoUrl = null;

  const logoFile = form.get("logoFile");
  if (logoFile instanceof File && logoFile.size > 0) {
    if (!logoFile.type.startsWith("image/"))
      return buildRedirect("failed", "Das Logo muss eine Bilddatei sein (PNG/JPG).");

    if (logoFile.size > MAX_LOGO_BYTES)
      return buildRedirect("failed", "Das Logo ist zu groß (maximal 10 MB).");

    const upload = await adminApiPost<PartnerLogoUploadResponse>("/api/admin/partners/logo", {
      Image: {
        FileName: logoFile.name || "logo",
        ContentType: logoFile.type,
        Base64Data: Buffer.from(await logoFile.arrayBuffer()).toString("base64"),
      },
    });

    if (upload === null)
      return buildRedirect("failed", "Die API ist nicht erreichbar oder ADMIN_API_KEY fehlt.");
    if (!upload.Success || !upload.LogoUrl)
      return buildRedirect("failed", upload.Error ?? "Das Logo konnte nicht hochgeladen werden.");

    logoUrl = upload.LogoUrl;
  }

  const sinceYearRaw = optional(form, "partnerSinceYear");
  const sinceYear = sinceYearRaw === null ? null : Number.parseInt(sinceYearRaw, 10);
  if (sinceYear !== null && !Number.isFinite(sinceYear))
    return buildRedirect("failed", "Das „Partner seit“-Jahr muss eine Zahl sein.");

  const result = await adminApiPost<PartnerSaveResponse>("/api/admin/partners/save", {
    Id: optional(form, "id"),
    Name: form.get("name")?.toString().trim() ?? "",
    Category: form.get("category")?.toString() ?? "",
    Description: optional(form, "description"),
    WebsiteUrl: optional(form, "websiteUrl"),
    LogoUrl: logoUrl,
    Region: optional(form, "region"),
    PartnerSinceYear: sinceYear,
    SourceName: optional(form, "sourceName"),
    SellerName: optional(form, "sellerName"),
    DisplayOrder: Number.parseInt(form.get("displayOrder")?.toString() ?? "", 10) || 0,
    IsVisible: form.get("isVisible") === "on",
  });

  if (result === null)
    return buildRedirect("failed", "Die API ist nicht erreichbar oder ADMIN_API_KEY fehlt.");

  if (!result.Success) return buildRedirect("failed", result.Error ?? "Unbekannter Fehler.");

  invalidatePartnersCache();
  return buildRedirect("saved");
};
