import type { APIRoute } from "astro";
import { getServerApiBaseUrl } from "@/lib/server/api-base";

// Server-seitiger Proxy zum WKO-Firmen-Sync (gleiches Muster wie ./sync.ts fuer die
// Zwangsversteigerungen): das <form> in intern/index.astro postet hierher, der Sync wird
// serverseitig ueber das interne Docker-Netz ausgeloest, danach Redirect zurueck (PRG).
// Erreichbarkeit ist bereits durch Caddy auf IP-Ebene beschraenkt (@internBlocked auf
// /intern*), die API verlangt zusaetzlich den Shared-Key X-Sync-Key (SYNC_TRIGGER_KEY).
export const POST: APIRoute = async () => {
  let ok = false;
  try {
    const syncKey = process.env.SYNC_TRIGGER_KEY ?? "";
    const response = await fetch(new URL("/api/wko-companies/sync", getServerApiBaseUrl()), {
      method: "POST",
      headers: { "content-type": "application/json", "x-sync-key": syncKey },
      body: "{}",
    });
    ok = response.ok;
  } catch {
    ok = false;
  }

  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/?wkoTriggered=${ok ? "1" : "0"}` },
  });
};
