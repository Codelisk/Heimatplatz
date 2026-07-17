import type { APIRoute } from "astro";
import { getServerApiBaseUrl } from "@/lib/server/api-base";

// Server-seitiger Proxy zum internen API-Endpoint (Post-Redirect-Get: das <form> in
// intern/index.astro postet hierher, wir loesen den Sync serverseitig ueber das interne
// Docker-Netz aus und leiten zurueck). Erreichbarkeit ist bereits durch Caddy auf
// IP-Ebene beschraenkt (siehe deploy/hetzner/Caddyfile, @internBlocked auf /intern*).
// Zusaetzlich verlangt die API einen Shared-Key (X-Sync-Key, SYNC_TRIGGER_KEY in
// deploy/hetzner/.env) - ohne konfigurierten Key ist der API-Endpoint gesperrt.
export const POST: APIRoute = async () => {
  let ok = false;
  try {
    const syncKey = process.env.SYNC_TRIGGER_KEY ?? "";
    const response = await fetch(new URL("/api/foreclosure-auctions/sync", getServerApiBaseUrl()), {
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
    headers: { Location: `/intern/?triggered=${ok ? "1" : "0"}` },
  });
};
