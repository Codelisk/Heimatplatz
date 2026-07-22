/**
 * Deutsche Labels fuer die Marketing-Enums. Die API liefert Enum-NAMEN als Strings
 * (globaler JsonStringEnumConverter) - hier bewusst switch statt dynamischer
 * i18n-Key-Konstruktion, damit t() typsicher bleibt.
 */
import { t } from "@/i18n";
import type { MarketingContactStatus, MarketingContactType } from "@/lib/server/admin-api";

export const CONTACT_TYPES: MarketingContactType[] = [
  "Unknown",
  "Broker",
  "PropertyManager",
  "PrivateSeller",
  "Municipality",
  "Partner",
  "Other",
];

export const CONTACT_STATUSES: MarketingContactStatus[] = [
  "Lead",
  "Contacted",
  "Replied",
  "Interested",
  "Customer",
  "NotInterested",
  "DoNotContact",
];

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
    case "Contacted":
      return t("intern.mkStatusContacted");
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

export function formatInternDate(value: string | null | undefined): string {
  if (!value) return "–";
  return new Intl.DateTimeFormat("de-AT", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Europe/Vienna",
  }).format(new Date(value));
}
