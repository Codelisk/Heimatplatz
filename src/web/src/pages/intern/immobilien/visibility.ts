import type { APIRoute } from "astro";
import { adminApiSend } from "@/lib/server/admin-api";
import { buildRedirect, readSafeBackQuery, rejectCrossSite } from "./_shared";

// Server-seitiger Proxy zum Admin-Endpoint (Post-Redirect-Get): blendet ein Inserat
// aus bzw. wieder ein. Erreichbarkeit ist durch Caddy auf IP-Ebene beschraenkt
// (@internBlocked auf /intern*), die API verlangt zusaetzlich den X-Admin-Key
// (ADMIN_API_KEY in deploy/hetzner/.env).
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const id = form.get("id")?.toString() ?? "";
  const hidden = form.get("hidden")?.toString() === "true";
  const back = readSafeBackQuery(form);

  const ok =
    id.length > 0 &&
    (await adminApiSend("/api/admin/properties/visibility", "POST", { Id: id, Hidden: hidden }));

  const action = !ok ? "failed" : hidden ? "hidden" : "shown";
  return buildRedirect(back, action);
};
