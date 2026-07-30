import type { APIRoute } from "astro";
import { adminApiPost, type MarketingReplyResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

/**
 * PRG-Action: Antwort auf eine Rueckmeldung verfassen. Die API versendet den Text
 * als echtes Reply (Threading-Header), haengt die Signatur an und speichert den
 * Versand in der Kontakt-Historie.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const id = form.get("id")?.toString() ?? "";
  const body = form.get("body")?.toString().trim() ?? "";
  const back = form.get("back")?.toString() === "unread" ? "filter=unread&" : "";

  const result =
    id.length > 0 && body.length > 0
      ? await adminApiPost<MarketingReplyResponse>("/api/admin/marketing/inbox/reply", {
          InboundEmailId: id,
          Body: body,
        })
      : null;

  const params = result?.Success
    ? `action=replied${result.SmtpConfigured ? "" : "&smtp=off"}`
    : `action=replyfailed&error=${encodeURIComponent(result?.Error ?? "API nicht erreichbar")}`;

  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/marketing/eingang/?${back}${params}` },
  });
};
