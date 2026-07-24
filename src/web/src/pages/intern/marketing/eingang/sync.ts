import type { APIRoute } from "astro";
import {
  adminApiGet,
  adminApiPost,
  type MarketingInboxPage,
  type MarketingSyncResponse,
} from "@/lib/server/admin-api";
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

  // Ohne IMAP-Konfiguration den Sync gar nicht erst ausloesen: Die Eingangsseite
  // zeigt bereits den "nicht konfiguriert"-Hinweis, ein Sync-Versuch wuerde nur
  // eine zweite, irrefuehrende Fehlermeldung produzieren. (Der Button ist dann
  // zwar deaktiviert, aber ein direkter POST soll trotzdem sauber zurueckleiten.)
  const inbox = await adminApiGet<MarketingInboxPage>("/api/admin/marketing/inbox?page=0&pageSize=1");
  if (inbox && !inbox.ImapConfigured) {
    return new Response(null, {
      status: 303,
      headers: {
        Location: back ? "/intern/marketing/eingang/?filter=unread" : "/intern/marketing/eingang/",
      },
    });
  }

  const result = await adminApiPost<MarketingSyncResponse>("/api/admin/marketing/inbox/sync", {});

  const params = result?.Success
    ? `action=synced&count=${result.Added}`
    : `action=syncfailed&error=${encodeURIComponent(result?.Error ?? "API nicht erreichbar")}`;

  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/marketing/eingang/?${back}${params}` },
  });
};
