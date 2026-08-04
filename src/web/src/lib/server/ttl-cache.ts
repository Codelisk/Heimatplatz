/**
 * Kleiner In-Memory-Cache mit Ablaufzeit fuer serverseitige API-Fetches (SSR).
 *
 * Der Node-Prozess lebt lange - ein permanenter Promise-Cache (wie frueher fuer
 * den statischen Build) wuerde die Daten fuer immer einfrieren. Stattdessen:
 * kurze TTL pro Eintrag, parallele Anfragen teilen sich das in-flight Promise,
 * abgelehnte Promises werden sofort verworfen, damit der naechste Request neu laedt.
 */
type CacheEntry = { value: Promise<unknown>; expiresAt: number };

const store = new Map<string, CacheEntry>();
const MAX_ENTRIES = 1000;

function purgeExpired(now: number) {
  for (const [key, entry] of store) {
    if (entry.expiresAt <= now) store.delete(key);
  }
}

export function cached<T>(key: string, ttlMs: number, factory: () => Promise<T>): Promise<T> {
  const now = Date.now();
  const entry = store.get(key);
  if (entry && entry.expiresAt > now) {
    return entry.value as Promise<T>;
  }

  if (store.size >= MAX_ENTRIES) purgeExpired(now);

  const value = factory();
  store.set(key, { value, expiresAt: now + ttlMs });
  value.catch(() => {
    // Fehler nicht cachen - nur entfernen, falls nicht schon ein neuer Eintrag existiert
    if (store.get(key)?.value === value) store.delete(key);
  });
  return value;
}

/**
 * Eintraege sofort verwerfen, nachdem der Admin sie ueber /intern geaendert hat - ohne das
 * zeigt die Seite direkt nach dem Speichern bis zu TTL lang den alten Stand, was wie ein
 * fehlgeschlagener Speichervorgang aussieht.
 *
 * Wirkt nur im eigenen Node-Prozess. Bei mehreren Web-Instanzen laufen die anderen normal
 * in die TTL - fuer Stammdaten mit 10 Minuten unkritisch.
 */
export function invalidateCached(...keys: string[]) {
  for (const key of keys) store.delete(key);
}

/** Standard-TTLs: Listen/Detail kurz (Aktualitaet), Bild-Checks/Rechtstexte laenger */
export const TTL = {
  properties: 60_000,
  // Detailseiten deutlich kuerzer: Wer sein Inserat gerade bearbeitet/geloescht hat,
  // soll nicht bis zu einer Minute lang den alten Stand (bzw. ein geloeschtes Inserat) sehen.
  propertyDetail: 10_000,
  images: 60 * 60_000,
  legal: 10 * 60_000,
  // Partner-Stammdaten wie legal: aendern sich selten, /intern/partner/ invalidiert direkt
  partners: 10 * 60_000,
  // Bezirk/Gemeinde-Hierarchie aendert sich praktisch nie
  locations: 60 * 60_000,
} as const;
