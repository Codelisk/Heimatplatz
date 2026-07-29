/**
 * Deutsche Labels fuer die Marketing-Enums. Die API liefert Enum-NAMEN als Strings
 * (globaler JsonStringEnumConverter) - hier bewusst switch statt dynamischer
 * i18n-Key-Konstruktion, damit t() typsicher bleibt.
 */
import { t } from "@/i18n";
import type {
  MarketingActivityType,
  MarketingContactStatus,
  MarketingContactType,
  MarketingSalutation,
} from "@/lib/server/admin-api";

export const CONTACT_TYPES: MarketingContactType[] = [
  "Unknown",
  "Broker",
  "PropertyManager",
  "PrivateSeller",
  "Municipality",
  "Partner",
  "Other",
];

// Reihenfolge = Funnel-Reihenfolge, nicht Enum-Reihenfolge: "Zu kontaktieren" steht am
// Anfang der Arbeit, "Wiedervorlage" direkt hinter "Kontaktiert".
export const CONTACT_STATUSES: MarketingContactStatus[] = [
  "ToContact",
  "Contacted",
  "FollowUp",
  "Replied",
  "Interested",
  "Customer",
  "NotInterested",
  "DoNotContact",
  "Lead",
];

export const ACTIVITY_TYPES: MarketingActivityType[] = ["Call", "Note", "Meeting"];

export const SALUTATIONS: MarketingSalutation[] = ["Unknown", "Herr", "Frau"];

export function salutationLabel(salutation: MarketingSalutation | string): string {
  switch (salutation) {
    case "Herr":
      return t("intern.mkFieldSalutationHerr");
    case "Frau":
      return t("intern.mkFieldSalutationFrau");
    default:
      return t("intern.mkFieldSalutationNone");
  }
}

export function contactTypeLabel(type: MarketingContactType | string): string {
  switch (type) {
    case "Broker":
      return t("intern.mkTypeBroker");
    case "PropertyManager":
      return t("intern.mkTypePropertyManager");
    case "PrivateSeller":
      return t("intern.mkTypePrivateSeller");
    case "Municipality":
      return t("intern.mkTypeMunicipality");
    case "Partner":
      return t("intern.mkTypePartner");
    case "Other":
      return t("intern.mkTypeOther");
    default:
      return t("intern.mkTypeUnknown");
  }
}

export function contactStatusLabel(status: MarketingContactStatus | string): string {
  switch (status) {
    case "ToContact":
      return t("intern.mkStatusToContact");
    case "Contacted":
      return t("intern.mkStatusContacted");
    case "FollowUp":
      return t("intern.mkStatusFollowUp");
    case "Replied":
      return t("intern.mkStatusReplied");
    case "Interested":
      return t("intern.mkStatusInterested");
    case "Customer":
      return t("intern.mkStatusCustomer");
    case "NotInterested":
      return t("intern.mkStatusNotInterested");
    case "DoNotContact":
      return t("intern.mkStatusDoNotContact");
    default:
      return t("intern.mkStatusLead");
  }
}

/** Dezente Badge-Faerbung je Status (Tailwind-Klassen, Light/Dark ueber Tokens) */
export function contactStatusBadgeClass(status: MarketingContactStatus | string): string {
  switch (status) {
    case "ToContact":
    case "FollowUp":
      return "bg-blue-100 text-blue-900 dark:bg-blue-900/40 dark:text-blue-200";
    case "Replied":
    case "Interested":
      return "bg-amber-100 text-amber-900 dark:bg-amber-900/40 dark:text-amber-200";
    case "Customer":
      return "bg-green-100 text-green-900 dark:bg-green-900/40 dark:text-green-200";
    case "NotInterested":
    case "DoNotContact":
      return "bg-red-100 text-red-900 dark:bg-red-900/40 dark:text-red-200";
    default:
      return "bg-muted text-muted-foreground";
  }
}

export function activityTypeLabel(type: MarketingActivityType | string): string {
  switch (type) {
    case "Call":
      return t("intern.mkActivityCall");
    case "Meeting":
      return t("intern.mkActivityMeeting");
    case "StatusChange":
      return t("intern.mkActivityStatusChange");
    case "FollowUp":
      return t("intern.mkActivityFollowUp");
    default:
      return t("intern.mkActivityNote");
  }
}

export function formatInternDate(value: string | null | undefined): string {
  if (!value) return "–";
  return new Intl.DateTimeFormat("de-AT", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Europe/Vienna",
  }).format(new Date(value));
}

/** Nur Datum - fuer Wiedervorlage-Termine, bei denen die Uhrzeit nichts aussagt */
export function formatInternDay(value: string | null | undefined): string {
  if (!value) return "–";
  return new Intl.DateTimeFormat("de-AT", {
    dateStyle: "medium",
    timeZone: "Europe/Vienna",
  }).format(new Date(value));
}

/** true, wenn der Wiedervorlage-Termin erreicht oder ueberschritten ist */
export function isFollowUpDue(value: string | null | undefined): boolean {
  if (!value) return false;
  return new Date(value).getTime() <= Date.now();
}

/**
 * Wert fuer <input type="date"> (YYYY-MM-DD) in Wiener Zeit - toISOString() waere UTC
 * und wuerde abends auf den Folgetag springen.
 */
export function toDateInputValue(value: string | null | undefined): string {
  if (!value) return "";
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: "Europe/Vienna",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(new Date(value));
  return parts;
}
