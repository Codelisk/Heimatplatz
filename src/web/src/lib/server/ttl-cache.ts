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

/** Standard-TTLs: Listen/Detail kurz (Aktualitaet), Bild-Checks/Rechtstexte laenger */
export const TTL = {
  properties: 60_000,
  images: 60 * 60_000,
  legal: 10 * 60_000,
  // Bezirk/Gemeinde-Hierarchie aendert sich praktisch nie
  locations: 60 * 60_000,
} as const;
