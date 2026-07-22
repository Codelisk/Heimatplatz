/**
 * Firmen- und Kontaktdaten als NOTFALL-FALLBACK - nicht die Quelle!
 *
 * Die Wahrheit sind die LegalSettings in der Datenbank (`GET /api/legal/contact`,
 * `/api/legal/imprint`, `/api/legal/privacy-policy`), gepflegt ueber /intern/kontakt.
 * Diese Konstanten greifen nur, wenn die API beim SSR nicht erreichbar ist - dann soll
 * /impressum nicht mit einer leeren Pflichtangabe ausgeliefert werden.
 *
 * Vorher lagen dieselben Werte zusaetzlich inline in features/legal/api.ts und zweimal in
 * pages/makler/index.astro. Wenn sich Stammdaten aendern: in der DB aendern, und diese
 * Datei nachziehen - sonst zeigt der Ausfall-Fall veraltete Angaben.
 */
export const COMPANY = {
  name: "Ing. Daniel Hufnagl",
  legalForm: "Einzelunternehmen",
  owner: "Ing. Daniel Hufnagl",

  street: "Stockham 44",
  postalCode: "4663",
  city: "Laakirchen",
  country: "Österreich",

  email: "info@heimatplatz.at",
  website: "https://www.heimatplatz.at",
  /** Leer = keine Telefonzeile. Gepflegt wird die Nummer in der DB, nicht hier. */
  phone: "",

  uidNumber: "ATU75151817",
  taxNumber: "532163383",
  dunsNumber: "30-080-8592",
  gln: "9110026231195",
  gisaNumber: "31233118",

  trade: "Dienstleistungen in der automatischen Datenverarbeitung und Informationstechnik",
  tradeAuthority: "Bezirkshauptmannschaft Gmunden",
  professionalLaw: "Gewerbeordnung 1994 (GewO)",
  chamberMembership: "Wirtschaftskammer Oberösterreich",
  tradeGroup: "Fachgruppe Unternehmensberatung, Buchhaltung und Informationstechnologie",
} as const;
