import type { APIRoute } from "astro";
import { adminApiSend } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

/**
 * PRG-Action: Kontakt endgueltig loeschen (inkl. Historie/Rueckmeldungen per
 * FK-Kaskade in der API). Redirect zurueck zur Kontaktliste.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const id = form.get("id")?.toString() ?? "";

  const ok =
    id.length > 0 &&
    (await adminApiSend(`/api/admin/marketing/contacts/${encodeURIComponent(id)}`, "DELETE"));

  const params = new URLSearchParams();
  if (ok) {
    params.set("action", "deleted");
  } else {
    params.set("action", "failed");
    params.set("error", "Löschen fehlgeschlagen");
  }

  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/marketing/kontakte/?${params}` },
  });
};
