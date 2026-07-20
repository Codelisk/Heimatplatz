import type { APIRoute } from "astro";
import { adminApiPost, type MarketingSyncResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

/**
 * PRG-Action: "Jetzt abrufen" - erzwingt einen sofortigen IMAP-Sync (umgeht die
 * 5-Minuten-Drossel des Auto-Syncs). Der generierte Mediator-Endpoint braucht
 * einen {}-Body, auch wenn der Request keine Felder hat.
 */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const back = form.get("back")?.toString() === "unread" ? "filter=unread&" : "";

  const result = await adminApiPost<MarketingSyncResponse>("/api/admin/marketing/inbox/sync", {});

  const params = result?.Success
    ? `action=synced&count=${result.Added}`
    : `action=syncfailed&error=${encodeURIComponent(result?.Error ?? "API nicht erreichbar")}`;

  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/marketing/eingang/?${back}${params}` },
  });
};
