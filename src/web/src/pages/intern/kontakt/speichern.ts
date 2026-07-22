import type { APIRoute } from "astro";
import { adminApiPost, type LegalUpdateResponse } from "@/lib/server/admin-api";
import { buildRedirect, invalidateLegalCaches, optional, rejectCrossSite } from "./_shared";

// Server-seitiger Proxy zum Admin-Endpoint (Post-Redirect-Get): speichert die Kontakt-
// Zusatzfelder. Erreichbarkeit ist durch Caddy auf IP-Ebene beschraenkt (@internBlocked
// auf /intern*), die API verlangt zusaetzlich den X-Admin-Key (ADMIN_API_KEY).
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();

  // Social-Zeilen kommen als parallele Feld-Arrays; leere Zeilen wirft die API weg
  const platforms = form.getAll("socialPlatform").map((value) => value.toString().trim());
  const urls = form.getAll("socialUrl").map((value) => value.toString().trim());
  const socialLinks = platforms
    .map((platform, index) => ({ Platform: platform, Url: urls[index] ?? "" }))
    .filter((link) => link.Platform.length > 0 && link.Url.length > 0);

  const result = await adminApiPost<LegalUpdateResponse>("/api/admin/legal/contact", {
    Email: optional(form, "email"),
    SupportEmail: optional(form, "supportEmail"),
    Phone: optional(form, "phone"),
    Website: optional(form, "website"),
    OfficeHours: optional(form, "officeHours"),
    SocialLinks: socialLinks,
  });

  if (result === null)
    return buildRedirect("failed", "Die API ist nicht erreichbar oder ADMIN_API_KEY fehlt.");

  if (!result.Success) return buildRedirect("failed", result.Error ?? "Unbekannter Fehler.");

  invalidateLegalCaches();
  return buildRedirect("saved");
};
