import type { APIRoute } from "astro";
import { adminApiPost, type MarketingRenderTemplateResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";
import { t } from "@/i18n";

/**
 * Proxy fuer das Einsetzen einer Vorlage: ruft POST /api/admin/marketing/templates/render
 * mit dem serverseitigen ADMIN_API_KEY auf. Die Platzhalter-Ersetzung passiert bewusst
 * in der API (Backend-First), damit die Anrede-Regel nur an einer Stelle steht.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const payload = await request.json().catch(() => null);
  const templateId = typeof payload?.templateId === "string" ? payload.templateId.trim() : "";
  const contactId = typeof payload?.contactId === "string" ? payload.contactId.trim() : "";

  if (!templateId) {
    return json({ ok: false, error: t("intern.marketingTemplateValidation") });
  }

  const result = await adminApiPost<MarketingRenderTemplateResponse>(
    "/api/admin/marketing/templates/render",
    { TemplateId: templateId, ContactId: contactId || null },
  );

  if (!result) {
    return json({ ok: false, error: t("intern.marketingApiUnreachable") }, 502);
  }

  return json({
    ok: result.Success,
    subject: result.Subject,
    body: result.Body,
    error: result.Error,
  });
};

function json(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "content-type": "application/json" },
  });
}
