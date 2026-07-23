import type { APIRoute } from "astro";
import { adminApiPost, type MarketingAddLeadsResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

// Nur bekannte Filter-Parameter aus dem "back"-Feld uebernehmen: verhindert
// Open-Redirect/Header-Injection ueber manipulierte Formularwerte.
const ALLOWED_BACK_PARAMS = ["q", "ort", "alle", "page"] as const;

function safeBackQuery(form: FormData): URLSearchParams {
  const raw = new URLSearchParams(form.get("back")?.toString() ?? "");
  const safe = new URLSearchParams();
  for (const key of ALLOWED_BACK_PARAMS) {
    const value = raw.get(key);
    if (value) safe.set(key, value);
  }
  return safe;
}

/**
 * PRG-Action: ausgewaehlte Firmenbuch-Firmen als Kontakte mit Status "Zu kontaktieren"
 * uebernehmen. Formular-POST -> API -> 303 zurueck auf den Pool mit erhaltenem Filter.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const back = safeBackQuery(form);
  const ids = form.getAll("ids").map((value) => value.toString()).filter(Boolean);

  if (ids.length === 0) {
    back.set("action", "failed");
    back.set("error", "Bitte mindestens eine Firma auswählen.");
    return redirect(back);
  }

  const result = await adminApiPost<MarketingAddLeadsResponse>(
    "/api/admin/marketing/lead-pool/add",
    { FirmenbuchCompanyIds: ids },
  );

  if (result?.Success) {
    back.set("action", "added");
    back.set("added", String(result.Added));
    back.set("skipped", String(result.Skipped));
  } else {
    back.set("action", "failed");
    back.set("error", result?.Error ?? "API nicht erreichbar");
  }

  return redirect(back);
};

function redirect(params: URLSearchParams): Response {
  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/marketing/firmenpool/?${params}` },
  });
}
