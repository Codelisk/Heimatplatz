/**
 * CSRF-Schutz fuer die Intern-Action-Routes (Immobilien-Aktionen, Marketing):
 * Browser setzen Sec-Fetch-Site, Cross-Site-POSTs (fremde Seite -> /intern/...)
 * werden abgelehnt. Aeltere Browser ohne den Header bleiben erlaubt - die
 * Caddy-IP-Sperre gilt zusaetzlich.
 */
export function rejectCrossSite(request: Request): Response | null {
  return request.headers.get("sec-fetch-site") === "cross-site"
    ? new Response("Forbidden", { status: 403 })
    : null;
}
