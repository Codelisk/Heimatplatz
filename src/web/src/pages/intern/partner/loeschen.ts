import type { APIRoute } from "astro";
import { adminApiPost, type PartnerDeleteResponse } from "@/lib/server/admin-api";
import { buildRedirect, invalidatePartnersCache, rejectCrossSite, required } from "./_shared";

/**
 * Server-seitiger Proxy (Post-Redirect-Get): loescht einen Partner endgueltig.
 * Die Bestaetigung passiert clientseitig (confirm im Formular) - destruktive Aktion.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const id = required(form, "id");
  if (!id) return buildRedirect("failed", "Es wurde kein Partner angegeben.");

  const result = await adminApiPost<PartnerDeleteResponse>("/api/admin/partners/delete", { Id: id });

  if (result === null)
    return buildRedirect("failed", "Die API ist nicht erreichbar oder ADMIN_API_KEY fehlt.");

  if (!result.Success) return buildRedirect("failed", result.Error ?? "Unbekannter Fehler.");

  invalidatePartnersCache();
  return buildRedirect("deleted");
};
