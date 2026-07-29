import type { APIRoute } from "astro";
import { adminApiPost, type MarketingSaveContactResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

/**
 * PRG-Action: Kontakt anlegen/bearbeiten. Formular-POST -> API-Upsert ->
 * 303-Redirect zurueck (Liste oder Detailseite) mit action=saved/failed.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const id = form.get("id")?.toString() || null;
  const redirectTarget = form.get("redirect")?.toString() === "detail" ? "detail" : "list";

  const result = await adminApiPost<MarketingSaveContactResponse>(
    "/api/admin/marketing/contacts/save",
    {
      Id: id,
      // Adresse ist optional - Firmenpool-Kontakte haben zunaechst keine
      Email: form.get("email")?.toString() || null,
      // Name = Alt-Bestand-Freitext (hidden-Feld); die strukturierten Felder gewinnen
      // serverseitig, sobald eines gefuellt ist
      Name: form.get("name")?.toString() || null,
      Salutation: form.get("salutation")?.toString() || "Unknown",
      Title: form.get("title")?.toString() || null,
      FirstName: form.get("firstName")?.toString() || null,
      LastName: form.get("lastName")?.toString() || null,
      Company: form.get("company")?.toString() || null,
      Phone: form.get("phone")?.toString() || null,
      City: form.get("city")?.toString() || null,
      ContactType: form.get("contactType")?.toString() || "Unknown",
      Status: form.get("status")?.toString() || "Lead",
      Notes: form.get("notes")?.toString() || null,
    },
  );

  const params = new URLSearchParams();
  let location: string;

  if (result?.Success && result.Id) {
    params.set("action", "saved");
    location =
      redirectTarget === "detail"
        ? `/intern/marketing/kontakte/detail/?id=${result.Id}&${params}`
        : `/intern/marketing/kontakte/?${params}`;
  } else {
    params.set("action", "failed");
    params.set("error", result?.Error ?? "API nicht erreichbar");
    location =
      redirectTarget === "detail" && id
        ? `/intern/marketing/kontakte/detail/?id=${id}&${params}`
        : `/intern/marketing/kontakte/?${params}`;
  }

  return new Response(null, { status: 303, headers: { Location: location } });
};
