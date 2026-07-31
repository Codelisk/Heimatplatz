import type { APIRoute } from "astro";
import { adminApiPost, type MarketingContactEmailActionResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

/**
 * PRG-Action: Zusatzadresse zu einem Kontakt hinzufuegen. Formular-POST ->
 * API -> 303-Redirect zurueck auf die Detailseite mit action=emailadded/emailfailed.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const contactId = form.get("contactId")?.toString() ?? "";

  const result = await adminApiPost<MarketingContactEmailActionResponse>(
    "/api/admin/marketing/contacts/emails/add",
    {
      ContactId: contactId,
      Email: form.get("email")?.toString() ?? "",
    },
  );

  const params = new URLSearchParams();
  if (result?.Success) {
    params.set("action", "emailadded");
  } else {
    params.set("action", "emailfailed");
    params.set("error", result?.Error ?? "API nicht erreichbar");
  }

  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/marketing/kontakte/detail/?id=${contactId}&${params}` },
  });
};
