import type { APIRoute } from "astro";
import { adminApiSend } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

/** PRG-Action: E-Mail-Vorlage loeschen. Versendete Mails bleiben unberuehrt. */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const id = form.get("id")?.toString() ?? "";

  const params = new URLSearchParams();
  if (!id) {
    params.set("action", "failed");
    params.set("error", "Keine Vorlage angegeben");
  } else {
    const ok = await adminApiSend(
      `/api/admin/marketing/templates/${encodeURIComponent(id)}`,
      "DELETE",
    );
    if (ok) {
      params.set("action", "deleted");
    } else {
      params.set("action", "failed");
      params.set("error", "API nicht erreichbar");
    }
  }

  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/marketing/vorlagen/?${params}` },
  });
};
