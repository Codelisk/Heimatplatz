/**
 * Debug-Werkzeuge (Pendant zur MAUI-DebugPage) sind nur aktiv, wenn die Seite
 * vom Entwicklungs-PC aus aufgerufen wird: localhost oder eine private LAN-IP
 * (z.B. Dev-Server per `--host` vom Handy aus). Auf oeffentlichen Hosts bleibt
 * der Debug-Eintrag versteckt und der API-Override wirkungslos.
 */
export function isDebugHost(hostname: string = window.location.hostname): boolean {
  if (hostname === "localhost" || hostname === "127.0.0.1" || hostname === "::1" || hostname === "[::1]") {
    return true;
  }
  return (
    /^10\.\d{1,3}\.\d{1,3}\.\d{1,3}$/.test(hostname) ||
    /^192\.168\.\d{1,3}\.\d{1,3}$/.test(hostname) ||
    /^172\.(1[6-9]|2\d|3[01])\.\d{1,3}\.\d{1,3}$/.test(hostname)
  );
}

/** localStorage-Schluessel des Debug-API-Endpunkt-Overrides (nur Debug-Hosts) */
export const DEBUG_API_URL_KEY = "heimatplatz:debug-api-url";

/**
 * Vom Debug-Tab gewaehlter API-Endpunkt. Wird von den Client-Skripten anstelle
 * der eingebauten PUBLIC_API_BASE_URL verwendet - wirkt sofort fuer alle
 * folgenden API-Aufrufe (statisch gebaute Inhalte bleiben vom Build).
 */
export function readDebugApiUrl(): string | null {
  try {
    if (!isDebugHost()) return null;
    return window.localStorage.getItem(DEBUG_API_URL_KEY);
  } catch {
    return null;
  }
}
