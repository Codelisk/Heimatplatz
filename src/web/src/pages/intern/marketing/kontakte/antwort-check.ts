import type { APIRoute } from "astro";
import { adminApiPost, type MarketingReplyCheckResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";
import { t } from "@/i18n";

/**
 * Proxy fuer die KI-Pruefung eines Antwort-Entwurfs aus dem Kontakt-Chat-Verlauf:
 * nimmt JSON vom Composer entgegen und ruft POST /api/admin/marketing/inbox/reply-check
 * mit dem serverseitigen ADMIN_API_KEY auf. Versendet und speichert nichts.
 * Die KI-Antwort kann eine Minute dauern - kein Timeout hier, der Fetch wartet.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const payload = await request.json().catch(() => null);
  const inboundEmailId =
    typeof payload?.inboundEmailId === "string" ? payload.inboundEmailId.trim() : "";
  const draft = typeof payload?.draft === "string" ? payload.draft.trim() : "";

  if (!inboundEmailId || !draft) {
    return json({ ok: false, error: t("intern.mkConvCheckValidation") });
  }

  const result = await adminApiPost<MarketingReplyCheckResponse>(
    "/api/admin/marketing/inbox/reply-check",
    { InboundEmailId: inboundEmailId, Draft: draft },
  );

  if (!result) {
    return json({ ok: false, error: t("intern.marketingApiUnreachable") }, 502);
  }

  return json({
    ok: result.Success,
    fitsContext: result.FitsContext,
    contextNote: result.ContextNote,
    correctedText: result.CorrectedText,
    suggestedText: result.SuggestedText,
    error: result.Error,
  });
};

function json(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "content-type": "application/json" },
  });
}
