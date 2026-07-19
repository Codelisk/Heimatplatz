import type { APIRoute } from "astro";
import { adminApiSend } from "@/lib/server/admin-api";
import { buildRedirect, readSafeBackQuery, rejectCrossSite } from "./_shared";

// Server-seitiger Proxy zum Admin-Endpoint (Post-Redirect-Get): loescht ein Inserat
// endgueltig (inkl. Upload-Bilder, siehe AdminDeletePropertyHandler). Die Seite fragt
// vorher per confirm() nach. Zugriffsschutz wie visibility.ts (Caddy-IP + X-Admin-Key).
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const id = form.get("id")?.toString() ?? "";
  const back = readSafeBackQuery(form);

  const ok =
    id.length > 0 &&
    (await adminApiSend(`/api/admin/properties/${encodeURIComponent(id)}`, "DELETE"));

  return buildRedirect(back, ok ? "deleted" : "failed");
};
