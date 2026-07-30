import type { APIRoute } from "astro";
import { adminApiPost, type MarketingQuickActionResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

const ALLOWED_ACTIONS = new Set([
  "Interested",
  "Reject",
  "Block",
  "Snooze",
  "NotReached",
  "Restore",
]);

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

/** Wiener Kalendertag in N Tagen als YYYY-MM-DD - toISOString wäre UTC und spränge abends auf den Folgetag */
function viennaDayIn(days: number): string {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Europe/Vienna",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(new Date(Date.now() + days * 86_400_000));
}

function json(body: MarketingQuickActionResponse, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json" },
  });
}

function fail(error: string, status = 200): Response {
  return json(
    {
      Success: false,
      Error: error,
      Status: null,
      NextFollowUpAt: null,
      PreviousStatus: null,
      PreviousFollowUpAt: null,
    },
    status,
  );
}

/**
 * Fetch-Action (kein PRG): Akquise-Schnellaktion aus der Kontaktliste bzw. Detailseite.
 * Nimmt form-encoded Felder entgegen, hängt serverseitig den ADMIN_API_KEY an und gibt
 * die API-Antwort als JSON durch - die Zeile aktualisiert sich clientseitig ohne Reload.
 * Wiedervorlage kommt entweder als fixes Datum (date, YYYY-MM-DD) oder relativ (days);
 * Mitternacht UTC ist in Wien 01:00/02:00 desselben Tages (gleiches Muster wie aktivitaet.ts).
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const contactId = form.get("contactId")?.toString() ?? "";
  const action = form.get("action")?.toString() ?? "";

  if (!contactId || !ALLOWED_ACTIONS.has(action)) return fail("Ungültige Anfrage", 400);

  const date = form.get("date")?.toString() ?? "";
  const days = Number.parseInt(form.get("days")?.toString() ?? "", 10);
  const day = /^\d{4}-\d{2}-\d{2}$/.test(date)
    ? date
    : Number.isFinite(days) && days > 0
      ? viennaDayIn(days)
      : "";

  const restoreStatus = form.get("restoreStatus")?.toString() ?? "";
  const restoreFollowUp = form.get("restoreFollowUp")?.toString() ?? "";

  const result = await adminApiPost<MarketingQuickActionResponse>(
    "/api/admin/marketing/contacts/quick",
    {
      ContactId: contactId,
      Action: action,
      Reason: form.get("reason")?.toString() || null,
      FollowUpAt: day ? `${day}T00:00:00Z` : null,
      RestoreStatus: ALLOWED_STATUSES.has(restoreStatus) ? restoreStatus : null,
      RestoreFollowUpAt: restoreFollowUp || null,
    },
  );

  return result ? json(result) : fail("API nicht erreichbar");
};
