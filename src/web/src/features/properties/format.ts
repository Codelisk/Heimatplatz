/**
 * Reine Formatierungs-Helfer ohne Server-Abhaengigkeiten - dieses Modul darf
 * (anders als live-api.ts) auch von Client-Skripten importiert werden.
 */
import { t } from "@/i18n";

export function formatApiPrice(value: number | string | null | undefined) {
  const number = Number(value);
  if (!Number.isFinite(number) || number <= 0) return t("property.priceOpen");
  // Branchenuebliche Schreibweise ("€ 365.000") statt "365 Tsd. EUR"
  return new Intl.NumberFormat("de-AT", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 0,
  }).format(number);
}

export function formatApiPriceLong(value: number | string | null | undefined) {
  const number = Number(value);
  if (!Number.isFinite(number) || number <= 0) return t("property.priceOnRequest");
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

type ApiAddressParts = {
  Address?: string | null;
  PostalCode?: string | null;
  City?: string | null;
};

function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/** Ortszeile der Karte: PLZ gehoert vor den Ort ("4742 Pram"), nie vor die Strasse. */
export function getApiLocationLine(property: ApiAddressParts) {
  return [property.PostalCode?.trim(), property.City?.trim()].filter(Boolean).join(" ");
}

/**
 * Strassenzeile der Karte: Address ohne die Ortsteile, die Importe oft
 * mitliefern ("4742 Pram", "Pram Bahnhofstrasse 3", "Bahnhofstrasse 3, 4742 Pram").
 * Liefert "" wenn die Adresse nichts ueber PLZ+Ort hinaus enthaelt.
 */
export function getApiStreetLine(property: ApiAddressParts) {
  const postalCode = escapeRegExp((property.PostalCode ?? "").trim());
  const city = escapeRegExp((property.City ?? "").trim());
  let street = (property.Address ?? "").trim();
  if (!street) return "";

  if (postalCode) street = street.replace(new RegExp(`^${postalCode}(?:[\\s,]+|$)`, "i"), "").trim();
  if (city) {
    // Nachgestelltes ", (PLZ) Ort" abschneiden
    street = street.replace(new RegExp(`[\\s,]+(?:${postalCode ? `${postalCode}\\s+` : ""})?${city}$`, "i"), "").trim();
    // Fuehrenden Ortsnamen nur strippen, wenn danach keine blosse Hausnummer
    // folgt - "Pram 44" IST die Strassenangabe in Doerfern ohne Strassennamen
    const rest = street.replace(new RegExp(`^${city}(?:[\\s,]+|$)`, "i"), "").trim();
    if (rest !== street && !/^\d/.test(rest)) street = rest;
  }
  return street;
}

/** Volle Adresszeile "Strasse, PLZ Ort" - Strasse zuerst, die PLZ gehoert zum Ort. */
export function getApiAddressLine(property: ApiAddressParts) {
  return [getApiStreetLine(property), getApiLocationLine(property)].filter(Boolean).join(", ");
}

export function formatApiDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return "";
  return new Intl.DateTimeFormat("de-AT", {
    timeZone: "Europe/Vienna",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(date);
}
