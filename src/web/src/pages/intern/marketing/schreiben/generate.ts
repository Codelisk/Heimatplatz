import type { APIRoute } from "astro";
import { adminApiPost, type MarketingGenerateResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";
import { t } from "@/i18n";

/**
 * Proxy fuer die KI-E-Mail-Generierung: nimmt JSON vom Marketing-Formular entgegen
 * und ruft POST /api/admin/marketing/email/generate mit dem serverseitigen
 * ADMIN_API_KEY auf. Die KI-Antwort kann 1-2 Minuten dauern - kein Timeout hier,
 * der Fetch wartet auf die API.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const payload = await request.json().catch(() => null);
  const keywords = typeof payload?.keywords === "string" ? payload.keywords.trim() : "";
  const recipientName =
    typeof payload?.recipientName === "string" ? payload.recipientName.trim() : "";

  if (!keywords) {
    return json({ ok: false, error: t("intern.marketingValidationKeywords") });
  }

  const result = await adminApiPost<MarketingGenerateResponse>(
    "/api/admin/marketing/email/generate",
    { Keywords: keywords, RecipientName: recipientName || null },
  );

  if (!result) {
    return json({ ok: false, error: t("intern.marketingApiUnreachable") }, 502);
  }

  return json({
    ok: result.Success,
    subject: result.Subject,
    body: result.Body,
    signatureText: result.SignatureText,
    error: result.Error,
  });
};

function json(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "content-type": "application/json" },
  });
}
