import { COMPANY } from "@/config/company";
import { getServerApiBaseUrl } from "@/lib/server/api-base";
import { cached, TTL } from "@/lib/server/ttl-cache";

export type LegalSection = {
  sortOrder: number;
  title: string;
  content: string;
  isVisible: boolean;
};

export type ResponsibleParty = {
  companyName: string;
  street: string;
  postalCode: string;
  city: string;
  country: string;
  email: string;
  phone: string;
  dataProtectionOfficer: string;
};

export type Imprint = ResponsibleParty & {
  /** phone normalisiert fuer href="tel:..." - kommt fertig aus der API */
  phoneLink: string;
  legalForm: string;
  owner: string;
  website: string;
  uidNumber: string;
  taxNumber: string;
  dunsNumber: string;
  gln: string;
  gisaNumber: string;
  trade: string;
  tradeAuthority: string;
  professionalLaw: string;
  chamberMembership: string;
  tradeGroup: string;
  sections: LegalSection[];
  version: string;
  effectiveDate: string;
  lastUpdated: string;
};

export type PrivacyPolicy = {
  responsibleParty: ResponsibleParty;
  /** Telefon des Verantwortlichen normalisiert fuer href="tel:..." */
  phoneLink: string;
  sections: LegalSection[];
  version: string;
  effectiveDate: string;
  lastUpdated: string;
};

export type SocialLink = {
  platform: string;
  url: string;
};

/**
 * Kontaktdaten aus GET /api/legal/contact - die Quelle fuer Footer, Makler-Seite und
 * JSON-LD. Das Backend hat supportEmail bereits aufgeloest und phoneLink normalisiert;
 * hier wird nichts mehr abgeleitet. Leere Strings heissen "nicht gepflegt" - dann die
 * jeweilige Zeile ausblenden statt einen Platzhalter zu zeigen.
 */
export type ContactInfo = {
  companyName: string;
  street: string;
  postalCode: string;
  city: string;
  country: string;
  email: string;
  supportEmail: string;
  phone: string;
  phoneLink: string;
  website: string;
  officeHours: string;
  socialLinks: SocialLink[];
};

type RawRecord = Record<string, unknown>;

/**
 * Nur fuer den Ausfall-Fall: normalerweise liefert die API phoneLink fertig berechnet
 * (PhoneNumberFormatter). Bewusst konservativ wie dort - keine Vorwahl erraten.
 */
const fallbackTelHref = COMPANY.phone.replace(/[^\d+]/g, "");

// Alle Fallback-Stammdaten kommen aus @/config/company - frueher standen sie hier inline
// und sind gegenueber der Datenbank auseinandergelaufen (Telefon: DB "+43 664 ...", hier "").
const fallbackResponsibleParty: ResponsibleParty = {
  companyName: COMPANY.name,
  street: COMPANY.street,
  postalCode: COMPANY.postalCode,
  city: COMPANY.city,
  country: COMPANY.country,
  email: COMPANY.email,
  phone: COMPANY.phone,
  dataProtectionOfficer: "",
};

const fallbackImprint: Imprint = {
  ...fallbackResponsibleParty,
  phoneLink: fallbackTelHref,
  legalForm: COMPANY.legalForm,
  owner: COMPANY.owner,
  website: COMPANY.website,
  uidNumber: COMPANY.uidNumber,
  taxNumber: COMPANY.taxNumber,
  dunsNumber: COMPANY.dunsNumber,
  gln: COMPANY.gln,
  gisaNumber: COMPANY.gisaNumber,
  trade: COMPANY.trade,
  tradeAuthority: COMPANY.tradeAuthority,
  professionalLaw: COMPANY.professionalLaw,
  chamberMembership: COMPANY.chamberMembership,
  tradeGroup: COMPANY.tradeGroup,
  sections: [
    {
      sortOrder: 1,
      title: "Haftungsausschluss",
      content:
        "Die Inhalte dieser Website wurden mit größter Sorgfalt erstellt. Für die Richtigkeit, Vollständigkeit und Aktualität der Inhalte übernehmen wir jedoch keine Gewähr.",
      isVisible: true,
    },
    {
      sortOrder: 2,
      title: "Urheberrecht",
      content:
        "Die durch den Seitenbetreiber erstellten Inhalte und Werke auf diesen Seiten unterliegen dem österreichischen Urheberrecht. Die Vervielfältigung, Bearbeitung, Verbreitung und jede Art der Verwertung außerhalb der Grenzen des Urheberrechtes bedürfen der schriftlichen Zustimmung des jeweiligen Autors bzw. Erstellers.",
      isVisible: true,
    },
    {
      sortOrder: 3,
      title: "Streitschlichtung",
      content:
        "Die Europäische Kommission stellt eine Plattform zur Online-Streitbeilegung (OS) bereit: https://ec.europa.eu/consumers/odr/\n\nWir sind nicht bereit oder verpflichtet, an Streitbeilegungsverfahren vor einer Verbraucherschlichtungsstelle teilzunehmen.",
      isVisible: true,
    },
  ],
  version: "1.0",
  effectiveDate: "2026-05-26T08:23:41.65898+02:00",
  lastUpdated: "2026-05-26T08:23:41.7600786+02:00",
};

const fallbackPrivacyPolicy: PrivacyPolicy = {
  responsibleParty: fallbackResponsibleParty,
  phoneLink: fallbackTelHref,
  sections: [
    {
      sortOrder: 1,
      title: "Verantwortlicher",
      content:
        "Verantwortlicher im Sinne der Datenschutz-Grundverordnung (DSGVO) ist die im Abschnitt genannte Stelle.",
      isVisible: true,
    },
    {
      sortOrder: 2,
      title: "Welche Daten wir erheben",
      content:
        "Bei der Nutzung unserer Website/App werden folgende Daten verarbeitet:\n\n- Server-Logdaten (IP-Adresse, Zugriffszeitpunkt, Browser-Typ)\n- Registrierungsdaten (Name, E-Mail-Adresse, Passwort)\n- Nutzungsdaten (Sucheinstellungen, Favoriten, Kontaktanfragen)\n- Immobiliendaten bei Inseratserstellung",
      isVisible: true,
    },
    {
      sortOrder: 3,
      title: "Zweck und Rechtsgrundlage",
      content:
        "Wir verarbeiten Ihre Daten zu folgenden Zwecken:\n\na) Vertragserfüllung (Art. 6 Abs. 1 lit. b DSGVO): Bereitstellung der Plattform, Verwaltung Ihres Benutzerkontos, Vermittlung von Immobilienanfragen.\n\nb) Berechtigte Interessen (Art. 6 Abs. 1 lit. f DSGVO): Gewährleistung der IT-Sicherheit, Analyse zur Verbesserung unserer Dienste, Betrugsprävention.\n\nc) Einwilligung (Art. 6 Abs. 1 lit. a DSGVO): Versand von Benachrichtigungen über neue Immobilien (sofern aktiviert).",
      isVisible: true,
    },
    {
      sortOrder: 4,
      title: "Speicherdauer",
      content:
        "- Server-Logs: 30 Tage\n- Benutzerkonto-Daten: Bis zur Löschung des Kontos\n- Kontaktanfragen: 3 Jahre nach Abschluss\n- Inserate: Bis zur Löschung durch den Nutzer",
      isVisible: true,
    },
    {
      sortOrder: 5,
      title: "Empfänger der Daten",
      content:
        "Ihre Daten werden an folgende Empfänger weitergegeben:\n\n- Hosting-Anbieter (Serverstandort: EU)\n- Immobilienanbieter bei Kontaktanfragen (nur freigegebene Daten)\n\nEine Übermittlung in Drittländer findet nicht statt.",
      isVisible: true,
    },
    {
      sortOrder: 6,
      title: "Ihre Rechte",
      content:
        "Sie haben folgende Rechte bezüglich Ihrer personenbezogenen Daten:\n\n- Auskunft über die gespeicherten Daten (Art. 15 DSGVO)\n- Berichtigung unrichtiger Daten (Art. 16 DSGVO)\n- Löschung Ihrer Daten (Art. 17 DSGVO)\n- Einschränkung der Verarbeitung (Art. 18 DSGVO)\n- Datenübertragbarkeit (Art. 20 DSGVO)\n- Widerspruch gegen die Verarbeitung (Art. 21 DSGVO)\n- Widerruf einer erteilten Einwilligung (Art. 7 Abs. 3 DSGVO)",
      isVisible: true,
    },
    {
      sortOrder: 7,
      title: "Beschwerderecht",
      content:
        "Sie haben das Recht, sich bei der zuständigen Aufsichtsbehörde zu beschweren:\n\nÖsterreichische Datenschutzbehörde\nBarichgasse 40-42\n1030 Wien\nE-Mail: dsb@dsb.gv.at\nWebsite: https://www.dsb.gv.at",
      isVisible: true,
    },
    {
      sortOrder: 8,
      title: "Cookies und Local Storage",
      content:
        "Unsere Website verwendet ausschließlich technisch notwendige Cookies bzw. Local Storage für:\n\n- Speicherung Ihrer Anmeldedaten (Session)\n- Speicherung Ihrer Filtereinstellungen\n\nFür technisch notwendige Cookies ist keine Einwilligung erforderlich (Paragraph 165 Abs. 3 TKG).",
      isVisible: true,
    },
    {
      sortOrder: 9,
      title: "Kontakt",
      content: "Bei Fragen zum Datenschutz wenden Sie sich bitte an die oben genannte E-Mail-Adresse.",
      isVisible: true,
    },
  ],
  version: "1.0",
  effectiveDate: "2026-04-03T07:52:32.3896373+02:00",
  lastUpdated: "2026-04-03T07:52:32.5106337+02:00",
};

function isRecord(value: unknown): value is RawRecord {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value);
}

function camelCase(key: string) {
  return `${key.charAt(0).toLowerCase()}${key.slice(1)}`;
}

function getValue(record: RawRecord, key: string) {
  return record[key] ?? record[camelCase(key)];
}

function readString(record: RawRecord, key: string) {
  const value = getValue(record, key);
  if (value === null || value === undefined) return "";
  if (typeof value === "string") return value.trim();
  if (typeof value === "number" || typeof value === "boolean") return String(value);
  return "";
}

function readNumber(record: RawRecord, key: string) {
  const value = getValue(record, key);
  const number = Number(value);
  return Number.isFinite(number) ? number : null;
}

function readBoolean(record: RawRecord, key: string, fallback = true) {
  const value = getValue(record, key);
  return typeof value === "boolean" ? value : fallback;
}

function readSections(record: RawRecord) {
  const sections = getValue(record, "Sections");
  if (!Array.isArray(sections)) return [];

  return sections
    .map((section, index): LegalSection | null => {
      if (!isRecord(section)) return null;
      const title = readString(section, "Title");
      const content = readString(section, "Content");
      if (!title && !content) return null;

      return {
        sortOrder: readNumber(section, "SortOrder") ?? index + 1,
        title,
        content,
        isVisible: readBoolean(section, "IsVisible"),
      };
    })
    .filter((section): section is LegalSection => Boolean(section));
}

function extractPayload(payload: unknown, key: string) {
  if (!isRecord(payload)) return {};
  const nested = getValue(payload, key);
  return isRecord(nested) ? nested : payload;
}

function normalizeResponsibleParty(record: RawRecord): ResponsibleParty {
  return {
    companyName: readString(record, "CompanyName"),
    street: readString(record, "Street"),
    postalCode: readString(record, "PostalCode"),
    city: readString(record, "City"),
    country: readString(record, "Country"),
    email: readString(record, "Email"),
    phone: readString(record, "Phone"),
    dataProtectionOfficer: readString(record, "DataProtectionOfficer"),
  };
}

function normalizeImprint(record: RawRecord): Imprint {
  return {
    ...normalizeResponsibleParty(record),
    phoneLink: readString(record, "PhoneLink"),
    legalForm: readString(record, "LegalForm"),
    owner: readString(record, "Owner"),
    website: readString(record, "Website"),
    uidNumber: readString(record, "UidNumber"),
    taxNumber: readString(record, "TaxNumber"),
    dunsNumber: readString(record, "DunsNumber"),
    gln: readString(record, "Gln"),
    gisaNumber: readString(record, "GisaNumber"),
    trade: readString(record, "Trade"),
    tradeAuthority: readString(record, "TradeAuthority"),
    professionalLaw: readString(record, "ProfessionalLaw"),
    chamberMembership: readString(record, "ChamberMembership"),
    tradeGroup: readString(record, "TradeGroup"),
    sections: readSections(record),
    version: readString(record, "Version"),
    effectiveDate: readString(record, "EffectiveDate"),
    lastUpdated: readString(record, "LastUpdated"),
  };
}

function normalizePrivacyPolicy(record: RawRecord): PrivacyPolicy {
  const responsibleParty = getValue(record, "ResponsibleParty");
  return {
    responsibleParty: isRecord(responsibleParty)
      ? normalizeResponsibleParty(responsibleParty)
      : fallbackResponsibleParty,
    phoneLink: readString(record, "PhoneLink"),
    sections: readSections(record),
    version: readString(record, "Version"),
    effectiveDate: readString(record, "EffectiveDate"),
    lastUpdated: readString(record, "LastUpdated"),
  };
}

function readSocialLinks(record: RawRecord): SocialLink[] {
  const links = getValue(record, "SocialLinks");
  if (!Array.isArray(links)) return [];

  return links
    .map((link): SocialLink | null => {
      if (!isRecord(link)) return null;
      const platform = readString(link, "Platform");
      const url = readString(link, "Url");
      return platform && url ? { platform, url } : null;
    })
    .filter((link): link is SocialLink => Boolean(link));
}

function normalizeContactInfo(record: RawRecord): ContactInfo {
  const email = readString(record, "Email");
  return {
    companyName: readString(record, "CompanyName"),
    street: readString(record, "Street"),
    postalCode: readString(record, "PostalCode"),
    city: readString(record, "City"),
    country: readString(record, "Country"),
    email,
    // Doppelter Boden: das Backend liefert supportEmail nie leer, ein alter API-Stand koennte es aber
    supportEmail: readString(record, "SupportEmail") || email,
    phone: readString(record, "Phone"),
    phoneLink: readString(record, "PhoneLink"),
    website: readString(record, "Website"),
    officeHours: readString(record, "OfficeHours"),
    socialLinks: readSocialLinks(record),
  };
}

function hasImprintContent(imprint: Imprint) {
  return Boolean(imprint.companyName && imprint.email);
}

function hasContactContent(contact: ContactInfo) {
  return Boolean(contact.companyName && contact.email);
}

function hasPrivacyPolicyContent(policy: PrivacyPolicy) {
  return Boolean(policy.responsibleParty.companyName && policy.sections.length);
}

export function fetchImprint(): Promise<Imprint> {
  return cached("legal:imprint", TTL.legal, async () => {
  try {
    const response = await fetch(new URL("/api/legal/imprint", getServerApiBaseUrl()), {
      headers: { Accept: "application/json" },
    });
    if (!response.ok) throw new Error(`API ${response.status}`);

    const imprint = normalizeImprint(extractPayload(await response.json(), "Imprint"));
    return hasImprintContent(imprint) ? imprint : fallbackImprint;
  } catch (error) {
    console.warn("[Heimatplatz] Imprint could not be loaded", error);
    return fallbackImprint;
  }
  });
}

export function fetchPrivacyPolicy(): Promise<PrivacyPolicy> {
  return cached("legal:privacy-policy", TTL.legal, async () => {
  try {
    const response = await fetch(new URL("/api/legal/privacy-policy", getServerApiBaseUrl()), {
      headers: { Accept: "application/json" },
    });
    if (!response.ok) throw new Error(`API ${response.status}`);

    const policy = normalizePrivacyPolicy(extractPayload(await response.json(), "PrivacyPolicy"));
    return hasPrivacyPolicyContent(policy) ? policy : fallbackPrivacyPolicy;
  } catch (error) {
    console.warn("[Heimatplatz] Privacy policy could not be loaded", error);
    return fallbackPrivacyPolicy;
  }
  });
}

const fallbackContact: ContactInfo = {
  companyName: COMPANY.name,
  street: COMPANY.street,
  postalCode: COMPANY.postalCode,
  city: COMPANY.city,
  country: COMPANY.country,
  email: COMPANY.email,
  supportEmail: COMPANY.email,
  phone: COMPANY.phone,
  phoneLink: fallbackTelHref,
  website: COMPANY.website,
  officeHours: "",
  socialLinks: [],
};

/**
 * Kontaktdaten fuer Footer, Makler-Seite und Organization-JSON-LD. Wird pro Seitenaufruf
 * im BaseLayout gebraucht - der TTL-Cache (10 Min) haelt das auf einem API-Call.
 */
export function fetchContactInfo(): Promise<ContactInfo> {
  return cached("legal:contact", TTL.legal, async () => {
    try {
      const response = await fetch(new URL("/api/legal/contact", getServerApiBaseUrl()), {
        headers: { Accept: "application/json" },
      });
      if (!response.ok) throw new Error(`API ${response.status}`);

      const contact = normalizeContactInfo(extractPayload(await response.json(), "Contact"));
      return hasContactContent(contact) ? contact : fallbackContact;
    } catch (error) {
      console.warn("[Heimatplatz] Contact info could not be loaded", error);
      return fallbackContact;
    }
  });
}

export function visibleLegalSections(sections: LegalSection[]) {
  return [...sections]
    .filter((section) => section.isVisible && (section.title || section.content))
    .sort((a, b) => a.sortOrder - b.sortOrder);
}

export function formatLegalDate(value: string) {
  if (!value) return "";
  const date = new Date(value);
  if (!Number.isFinite(date.valueOf())) return "";

  return new Intl.DateTimeFormat("de-AT", {
    timeZone: "Europe/Vienna",
    day: "2-digit",
    month: "long",
    year: "numeric",
  }).format(date);
}

export function paragraphsFromLegalText(value: string) {
  return value
    .split(/\n{2,}/)
    .map((paragraph) => paragraph.trim())
    .filter(Boolean);
}
