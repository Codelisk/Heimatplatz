/**
 * Server-seitige Beschaffung des Übersicht-Zustands für die SSR-Erstauslieferung
 * von /meine-uebersicht/: liest das vom PropertyStateScript gespiegelte
 * Token-Cookie und holt Liste + aktive Übersicht + Widget-Daten mit einem
 * Bearer-Fetch. NUR im Server-Frontmatter importieren (Muster live-api.ts).
 *
 * Bewusst OHNE cached()/TTL-Cache: personalisierte Daten dürfen nie im
 * prozessweiten Cache landen. Jeder Fehler fällt leise auf das bewährte
 * Client-Rendering zurück (return null) — SSR ist hier reine Beschleunigung.
 */
import type { AstroCookies } from "astro";
import { getServerApiBaseUrl } from "@/lib/server/api-base";
import {
  normalizeStatus,
  type DashboardResponse,
  type DashboardSummary,
  type WidgetData,
} from "./spec";

/** Muss zum Cookie-Namen in PropertyStateScript.syncSsrCookie passen */
export const SSR_TOKEN_COOKIE = "hp_ssr_at";

export interface DashboardSsrState {
  Summaries: DashboardSummary[];
  Dashboard: DashboardResponse | null;
  Widgets: WidgetData[] | null;
}

/**
 * Token aus dem Cookie-Spiegel; abgelaufene Tokens liefern null (dann rendert
 * die Seite wie bisher clientseitig — der Refresh passiert im Browser).
 */
export function readSsrAccessToken(cookies: AstroCookies): string | null {
  const raw = cookies.get(SSR_TOKEN_COOKIE)?.value;
  if (!raw) return null;
  try {
    const payload = raw.split(".")[1];
    if (!payload) return null;
    const padded = payload.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(payload.length / 4) * 4, "=");
    const exp = (JSON.parse(Buffer.from(padded, "base64").toString("utf8")) as { exp?: number }).exp;
    // 30s Marge: ein Token, das waehrend des Renderns ablaeuft, hilft niemandem
    if (typeof exp === "number" && exp * 1000 <= Date.now() + 30_000) return null;
    return raw;
  } catch {
    return null;
  }
}

export async function fetchDashboardSsrState(
  token: string,
  preferredId: string | null,
): Promise<DashboardSsrState | null> {
  try {
    const base = getServerApiBaseUrl();
    const fetchJson = async <T>(path: string): Promise<T> => {
      const response = await fetch(new URL(path, base), {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!response.ok) throw new Error(`API ${path} -> ${response.status}`);
      return (await response.json()) as T;
    };

    const list = await fetchJson<{ Dashboards: DashboardSummary[] }>("/api/dashboards");
    const summaries = list.Dashboards ?? [];
    // Ohne ?id (oder mit unbekannter Id) rendert die Seite den Uebersichts-Modus
    // (Karten-Grid) - KEINE Auto-Auswahl der ersten Ansicht
    const target = preferredId ? summaries.find((item) => item.Id === preferredId) : undefined;
    if (!target) return { Summaries: summaries, Dashboard: null, Widgets: null };

    const dashboard = await fetchJson<DashboardResponse>(`/api/dashboards/${target.Id}`);

    let widgets: WidgetData[] | null = null;
    if (normalizeStatus(dashboard.Status) === "finished" && dashboard.Definition) {
      widgets = (await fetchJson<{ Widgets: WidgetData[] }>(`/api/dashboards/${target.Id}/data`)).Widgets ?? [];
    }

    return { Summaries: summaries, Dashboard: dashboard, Widgets: widgets };
  } catch (error) {
    console.warn("[dashboard-ssr] Erstauslieferung nicht möglich, Client-Rendering übernimmt:", error);
    return null;
  }
}
