import type { APIRoute } from "astro";
import { adminApiPost, type MarketingSendResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";
import { t } from "@/i18n";

/**
 * Proxy fuer den E-Mail-Versand: nimmt den (ggf. nachbearbeiteten) Entwurf entgegen
 * und ruft POST /api/admin/marketing/email/send mit dem serverseitigen
 * ADMIN_API_KEY auf. Die Signatur haengt die API an; Name/Stichwoerter gehen mit,
 * damit die API den Kontakt anlegt und den Versand in der Historie speichert.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const payload = await request.json().catch(() => null);
  const recipientEmail =
    typeof payload?.recipientEmail === "string" ? payload.recipientEmail.trim() : "";
  const recipientName =
    typeof payload?.recipientName === "string" ? payload.recipientName.trim() : "";
  const ccEmail = typeof payload?.ccEmail === "string" ? payload.ccEmail.trim() : "";
  const bccEmail = typeof payload?.bccEmail === "string" ? payload.bccEmail.trim() : "";
  const keywords = typeof payload?.keywords === "string" ? payload.keywords.trim() : "";
  const subject = typeof payload?.subject === "string" ? payload.subject.trim() : "";
  const body = typeof payload?.body === "string" ? payload.body.trim() : "";

  if (!recipientEmail) {
    return json({ ok: false, error: t("intern.marketingValidationRecipient") });
  }
  if (!subject || !body) {
    return json({ ok: false, error: t("intern.marketingValidationDraft") });
  }

  const result = await adminApiPost<MarketingSendResponse>("/api/admin/marketing/email/send", {
    RecipientEmail: recipientEmail,
    Subject: subject,
    Body: body,
    RecipientName: recipientName || null,
    Keywords: keywords || null,
    CcEmail: ccEmail || null,
    BccEmail: bccEmail || null,
  });

  if (!result) {
    return json({ ok: false, error: t("intern.marketingApiUnreachable") }, 502);
  }

  return json({
    ok: result.Success,
    smtpConfigured: result.SmtpConfigured,
    error: result.Error,
    contactId: result.ContactId,
  });
};

function json(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "content-type": "application/json" },
  });
}
