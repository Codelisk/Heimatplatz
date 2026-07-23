import type { APIRoute } from "astro";
import { adminApiPost, type MarketingActivityResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

const ALLOWED_TYPES = new Set(["Call", "Note", "Meeting"]);
const ALLOWED_STATUSES = new Set([
  "Lead",
  "ToContact",
  "Contacted",
  "FollowUp",
  "Replied",
  "Interested",
  "Customer",
  "NotInterested",
  "DoNotContact",
]);

/**
 * PRG-Action: Anruf/Notiz/Termin am Kontakt festhalten, inklusive Wiedervorlage und
 * Statuswechsel. Formular-POST -> API -> 303 zurueck auf die Detailseite.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const contactId = form.get("contactId")?.toString() ?? "";
  if (!contactId) {
    return new Response(null, {
      status: 303,
      headers: { Location: "/intern/marketing/kontakte/" },
    });
  }

  const type = form.get("type")?.toString() ?? "";
  const status = form.get("status")?.toString() ?? "";
  const followUp = form.get("followUp")?.toString() ?? "";

  const result = await adminApiPost<MarketingActivityResponse>(
    "/api/admin/marketing/contacts/activity",
    {
      ContactId: contactId,
      Type: ALLOWED_TYPES.has(type) ? type : "Note",
      Notes: form.get("notes")?.toString() || null,
      // <input type="date"> liefert YYYY-MM-DD ohne Zeitzone. Mitternacht UTC ist in Wien
      // 01:00/02:00 desselben Tages - der Termin wird also am richtigen Kalendertag
      // angezeigt und ab dem fruehen Morgen als faellig gefuehrt. Ein fester Offset
      // (+02:00) waere im Winter falsch.
      FollowUpAt: followUp ? `${followUp}T00:00:00Z` : null,
      // Ohne neuen Termin die bestehende Wiedervorlage auf Wunsch entfernen
      ClearFollowUp: !followUp && form.get("clearFollowUp") !== null,
      NewStatus: ALLOWED_STATUSES.has(status) ? status : null,
    },
  );

  const params = new URLSearchParams({ id: contactId });
  if (result?.Success) {
    params.set("action", "activity");
  } else {
    params.set("action", "activityfailed");
    params.set("error", result?.Error ?? "API nicht erreichbar");
  }

  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/marketing/kontakte/detail/?${params}` },
  });
};
