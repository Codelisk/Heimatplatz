import type { APIRoute } from "astro";
import { adminApiPost, type MarketingSaveContactResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

/**
 * PRG-Action: E-Mail-Vorlage anlegen/bearbeiten. Formular-POST -> API-Upsert ->
 * 303-Redirect zurueck auf die Vorlagen-Seite.
 * Die Antwort hat dieselbe Form wie beim Kontakt-Upsert (Success/Id/Error).
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const id = form.get("id")?.toString() || null;

  const result = await adminApiPost<MarketingSaveContactResponse>(
    "/api/admin/marketing/templates/save",
    {
      Id: id,
      Name: form.get("name")?.toString() ?? "",
      Description: form.get("description")?.toString() || null,
      Subject: form.get("subject")?.toString() ?? "",
      Body: form.get("body")?.toString() ?? "",
      IsActive: form.get("isActive") !== null,
      DisplayOrder: Number.parseInt(form.get("displayOrder")?.toString() ?? "0", 10) || 0,
    },
  );

  const params = new URLSearchParams();
  if (result?.Success) {
    params.set("action", "saved");
  } else {
    params.set("action", "failed");
    params.set("error", result?.Error ?? "API nicht erreichbar");
    // Bei Fehlern im Bearbeiten-Modus bleiben, damit die Eingaben wieder vorliegen
    if (id) params.set("id", id);
  }

  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/marketing/vorlagen/?${params}` },
  });
};
