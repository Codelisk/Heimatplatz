/**
 * Gemeinsame Helfer der Partner-Action-Routes (speichern.ts, loeschen.ts).
 * Astro routet Dateien mit fuehrendem Unterstrich nicht - reine Hilfsdatei.
 */
import { PARTNERS_CACHE_KEY } from "@/features/partners/api";
import { invalidateCached } from "@/lib/server/ttl-cache";

/**
 * Nach jedem Schreibvorgang den SSR-Cache der oeffentlichen Partner-Seite verwerfen -
 * ohne das zeigt /partner/ bis zu 10 Minuten den alten Stand und das Speichern
 * wirkt fehlgeschlagen (gleiche Begruendung wie invalidateLegalCaches).
 */
export function invalidatePartnersCache() {
  invalidateCached(PARTNERS_CACHE_KEY);
}

export function buildRedirect(action: "saved" | "deleted" | "failed", error?: string): Response {
  const query = new URLSearchParams({ action });
  if (error) query.set("error", error.slice(0, 300));

  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/partner/?${query}` },
  });
}

/** Leere Formularfelder als "nicht gepflegt" (null) an die API geben, nicht als "". */
export function optional(form: FormData, field: string): string | null {
  const value = form.get(field)?.toString().trim() ?? "";
  return value.length > 0 ? value : null;
}

export function required(form: FormData, field: string): string {
  return form.get(field)?.toString().trim() ?? "";
}

export { rejectCrossSite } from "@/lib/server/csrf";
