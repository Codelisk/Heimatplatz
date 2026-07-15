/**
 * Basis-URL der Heimatplatz-API fuer serverseitige Fetches (SSR).
 *
 * Zur Laufzeit per Umgebungsvariable API_BASE_URL_SERVER uebersteuerbar -
 * auf dem Hetzner-Server zeigt sie auf das interne Docker-Netz
 * (http://api:8080), womit SSR-Requests nicht den Umweg ueber die
 * oeffentliche Domain nehmen. Fallback ist die beim Build eingebettete
 * PUBLIC_API_BASE_URL (lokale Entwicklung) bzw. die Produktions-API.
 *
 * Nur serverseitig importieren (process.env existiert im Client-Bundle nicht).
 */
export function getServerApiBaseUrl(): string {
  return (
    process.env.API_BASE_URL_SERVER ||
    import.meta.env.PUBLIC_API_BASE_URL ||
    "https://api.heimatplatz.at"
  );
}
