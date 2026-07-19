/**
 * Gemeinsame Helfer der Immobilien-Action-Routes (visibility.ts, delete.ts).
 * Astro routet Dateien mit fuehrendem Unterstrich nicht - reine Hilfsdatei.
 */

// Nur bekannte Filter-Parameter aus dem "back"-Feld uebernehmen: verhindert
// Open-Redirect/Header-Injection ueber manipulierte Formularwerte.
const ALLOWED_BACK_PARAMS = ["source", "status", "q", "userId", "userLabel", "page"] as const;

export function readSafeBackQuery(form: FormData): URLSearchParams {
  const raw = new URLSearchParams(form.get("back")?.toString() ?? "");
  const safe = new URLSearchParams();
  for (const key of ALLOWED_BACK_PARAMS) {
    const value = raw.get(key);
    if (value) safe.set(key, value);
  }
  return safe;
}

export function buildRedirect(back: URLSearchParams, action: string): Response {
  back.set("action", action);
  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/immobilien/?${back}` },
  });
}

// CSRF-Schutz fuer die destruktiven Intern-Actions: Browser setzen Sec-Fetch-Site,
// Cross-Site-Formular-POSTs (fremde Seite -> /intern/...) werden abgelehnt. Aeltere
// Browser ohne den Header bleiben erlaubt - die Caddy-IP-Sperre gilt zusaetzlich.
export function rejectCrossSite(request: Request): Response | null {
  return request.headers.get("sec-fetch-site") === "cross-site"
    ? new Response("Forbidden", { status: 403 })
    : null;
}
