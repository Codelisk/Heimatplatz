/**
 * Deutsche Labels fuer die Feedback-Enums im Intern-Bereich. Die API liefert
 * Enum-NAMEN als Strings (globaler JsonStringEnumConverter) - hier bewusst switch
 * statt dynamischer i18n-Key-Konstruktion, damit t() typsicher bleibt
 * (gleiche Konvention wie marketing-labels.ts).
 */
import { t } from "@/i18n";
import type {
  FeedbackCategory,
  FeedbackSource,
  FeedbackTicketStatus,
} from "@/lib/server/admin-api";

export const FEEDBACK_STATUSES: FeedbackTicketStatus[] = [
  "Open",
  "InProgress",
  "Answered",
  "Closed",
];

export const FEEDBACK_CATEGORIES: FeedbackCategory[] = [
  "Idea",
  "Problem",
  "Question",
  "Praise",
  "Other",
];

export function feedbackCategoryLabel(category: FeedbackCategory | string): string {
  switch (category) {
    case "Idea":
      return t("intern.fbCategoryIdea");
    case "Problem":
      return t("intern.fbCategoryProblem");
    case "Question":
      return t("intern.fbCategoryQuestion");
    case "Praise":
      return t("intern.fbCategoryPraise");
    default:
      return t("intern.fbCategoryOther");
  }
}

export function feedbackStatusLabel(status: FeedbackTicketStatus | string): string {
  switch (status) {
    case "InProgress":
      return t("intern.fbStatusInProgress");
    case "Answered":
      return t("intern.fbStatusAnswered");
    case "Closed":
      return t("intern.fbStatusClosed");
    default:
      return t("intern.fbStatusOpen");
  }
}

export function feedbackSourceLabel(source: FeedbackSource | string): string {
  switch (source) {
    case "Web":
      return t("intern.fbSourceWeb");
    case "Android":
      return t("intern.fbSourceAndroid");
    case "Ios":
      return t("intern.fbSourceIos");
    case "Windows":
      return t("intern.fbSourceWindows");
    default:
      return t("intern.fbSourceUnknown");
  }
}

/** Dezente Badge-Faerbung je Status (Tailwind-Klassen, Light/Dark ueber Tokens) */
export function feedbackStatusBadgeClass(status: FeedbackTicketStatus | string): string {
  switch (status) {
    case "InProgress":
      return "bg-amber-100 text-amber-900 dark:bg-amber-900/40 dark:text-amber-200";
    case "Answered":
      return "bg-green-100 text-green-900 dark:bg-green-900/40 dark:text-green-200";
    case "Closed":
      return "bg-muted text-muted-foreground";
    default:
      return "bg-red-100 text-red-900 dark:bg-red-900/40 dark:text-red-200";
  }
}
