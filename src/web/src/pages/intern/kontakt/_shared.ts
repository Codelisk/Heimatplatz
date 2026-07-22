/**
 * Gemeinsame Helfer der Kontaktdaten-Action-Routes (speichern.ts, impressum.ts).
 * Astro routet Dateien mit fuehrendem Unterstrich nicht - reine Hilfsdatei.
 */
import { invalidateCached } from "@/lib/server/ttl-cache";

/**
 * Nach jedem Schreibvorgang die SSR-Caches der Rechtsdaten verwerfen. Ohne das zeigen
 * /intern/kontakt, Footer und Impressum bis zu 10 Minuten den alten Stand - der Bearbeiter
 * haelt das Speichern dann fuer fehlgeschlagen. Die Keys stammen aus features/legal/api.ts.
 */
export function invalidateLegalCaches() {
  invalidateCached("legal:contact", "legal:imprint", "legal:privacy-policy");
}

export function buildRedirect(action: "saved" | "imprint-saved" | "failed", error?: string): Response {
  const query = new URLSearchParams({ action });
  // Fehlertext aus der API mitgeben, damit die Seite konkret werden kann statt nur
  // "hat nicht geklappt" - gekuerzt, damit die URL nicht ausufert.
  if (error) query.set("error", error.slice(0, 300));

  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/kontakt/?${query}` },
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
