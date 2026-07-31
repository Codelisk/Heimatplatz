import type { APIRoute } from "astro";
import { adminApiPost, type MarketingReplyResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

/**
 * PRG-Action: Antwort aus dem Chat-Verlauf der Kontakt-Detailseite. Fachlich identisch
 * zu eingang/antwort.ts (die API versendet ein echtes Reply mit Threading-Headern auf
 * die juengste Rueckmeldung, haengt die Signatur an und speichert den Versand in der
 * Historie), fuehrt aber zurueck auf die Detailseite des Kontakts.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const contactId = form.get("contactId")?.toString() ?? "";
  const inboundEmailId = form.get("inboundEmailId")?.toString() ?? "";
  const body = form.get("body")?.toString().trim() ?? "";

  const result =
    inboundEmailId.length > 0 && body.length > 0
      ? await adminApiPost<MarketingReplyResponse>("/api/admin/marketing/inbox/reply", {
          InboundEmailId: inboundEmailId,
          Body: body,
        })
      : null;

  const params = result?.Success
    ? `action=replied${result.SmtpConfigured ? "" : "&smtp=off"}`
    : `action=replyfailed&error=${encodeURIComponent(result?.Error ?? "API nicht erreichbar")}`;

  return new Response(null, {
    status: 303,
    headers: {
      Location: `/intern/marketing/kontakte/detail/?id=${encodeURIComponent(contactId)}&${params}`,
    },
  });
};
