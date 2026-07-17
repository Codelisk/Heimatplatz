/**
 * Reine Formatierungs-Helfer ohne Server-Abhaengigkeiten - dieses Modul darf
 * (anders als live-api.ts) auch von Client-Skripten importiert werden.
 */

export function formatApiPrice(value: number | string | null | undefined) {
  const number = Number(value);
  if (!Number.isFinite(number) || number <= 0) return "Preis offen";
  // Branchenuebliche Schreibweise ("€ 365.000") statt "365 Tsd. EUR"
  return new Intl.NumberFormat("de-AT", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 0,
  }).format(number);
}

export function formatApiPriceLong(value: number | string | null | undefined) {
  const number = Number(value);
  if (!Number.isFinite(number) || number <= 0) return "Preis auf Anfrage";
  return new Intl.NumberFormat("de-AT", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 0,
  }).format(number);
}

/**
 * Sichtbarer Anzeigetitel: Aus Zwangsversteigerungen synchronisierte Inserate tragen
 * den Titel "Zwangsversteigerung: Mehrfamilienhaus ..." - das Praefix ist neben dem
 * ZV-Badge redundant und wird fuer die Anzeige entfernt (der SEO-Titel bleibt unberuehrt).
 */
export function getDisplayTitle(title: string) {
  return title.replace(/^\s*Zwangsversteigerung\s*[:\-–—]\s*/i, "").trim() || title;
}
