import type { APIRoute } from "astro";
import { adminApiPost, type LegalUpdateResponse } from "@/lib/server/admin-api";
import { buildRedirect, invalidateLegalCaches, optional, rejectCrossSite, required } from "./_shared";

// Server-seitiger Proxy zum Admin-Endpoint (Post-Redirect-Get): speichert die Impressum-
// Stammdaten. Das Formular ist vollstaendig vorbefuellt - es ersetzt den Datensatz komplett,
// ein fehlendes Feld wuerde den bisherigen Wert loeschen.
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();

  const result = await adminApiPost<LegalUpdateResponse>("/api/admin/legal/imprint", {
    CompanyName: required(form, "companyName"),
    LegalForm: required(form, "legalForm"),
    Owner: required(form, "owner"),
    Street: required(form, "street"),
    PostalCode: required(form, "postalCode"),
    City: required(form, "city"),
    Country: required(form, "country"),
    Email: required(form, "email"),
    Phone: optional(form, "phone"),
    Website: optional(form, "website"),
    UidNumber: required(form, "uidNumber"),
    TaxNumber: required(form, "taxNumber"),
    DunsNumber: optional(form, "dunsNumber"),
    Gln: optional(form, "gln"),
    GisaNumber: optional(form, "gisaNumber"),
    Trade: required(form, "trade"),
    TradeAuthority: required(form, "tradeAuthority"),
    ProfessionalLaw: required(form, "professionalLaw"),
    ChamberMembership: optional(form, "chamberMembership"),
    TradeGroup: optional(form, "tradeGroup"),
  });

  if (result === null)
    return buildRedirect("failed", "Die API ist nicht erreichbar oder ADMIN_API_KEY fehlt.");

  if (!result.Success) return buildRedirect("failed", result.Error ?? "Unbekannter Fehler.");

  invalidateLegalCaches();
  return buildRedirect("imprint-saved");
};
