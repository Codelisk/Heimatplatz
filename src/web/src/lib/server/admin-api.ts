/**
 * Serverseitiger Client fuer die /api/admin-Endpoints (Intern-Bereich).
 *
 * Schickt den Shared-Key aus der Umgebungsvariable ADMIN_API_KEY als
 * X-Admin-Key-Header mit (deploy/hetzner/docker-compose.yml). Lokal gegen eine
 * Development-API ist kein Key noetig (die API laesst Development ohne
 * konfigurierten Admin:ApiKey durch, fail-open nur dort).
 *
 * Nur serverseitig importieren - der Key darf nie ins Client-Bundle gelangen.
 */
import { getServerApiBaseUrl } from "./api-base";

export type AdminStats = {
  TotalUsers: number;
  NewUsers7Days: number;
  NewUsers30Days: number;
  TotalProperties: number;
  UserProperties: number;
  ForeclosureProperties: number;
  HiddenProperties: number;
};

export type AdminUser = {
  Id: string;
  FullName: string;
  Email: string;
  // Serialisiert per globalem JsonStringEnumConverter (Program.cs) als Text, nicht numerisch
  SellerType: "Private" | "Broker" | "PropertyManager" | null;
  CompanyName: string | null;
  IsAdmin: boolean;
  EmailVerified: boolean;
  CreatedAt: string;
  PropertyCount: number;
};

export type AdminUsersPage = {
  Users: AdminUser[];
  Total: number;
  PageSize: number;
  CurrentPage: number;
  HasMore: boolean;
};

export type AdminProperty = {
  Id: string;
  Title: string;
  Address: string;
  MunicipalityName: string;
  PostalCode: string;
  Price: number;
  // Serialisiert per globalem JsonStringEnumConverter (Program.cs) als Text, nicht numerisch
  Type: "House" | "Land" | "Foreclosure";
  SellerType: "Private" | "Broker" | "PropertyManager";
  SellerName: string;
  UserId: string;
  OwnerEmail: string | null;
  SourceName: string | null;
  SourceUrl: string | null;
  IsHidden: boolean;
  CreatedAt: string;
  ThumbnailUrl: string | null;
};

export type AdminPropertiesPage = {
  Properties: AdminProperty[];
  Total: number;
  PageSize: number;
  CurrentPage: number;
  HasMore: boolean;
};

/** PropertyType.Foreclosure aus den API-Contracts (JsonStringEnumConverter -> Text, nicht 3) */
export const PROPERTY_TYPE_FORECLOSURE = "Foreclosure";

// Marketing-Feature (/api/admin/marketing) - Fehler kommen als Success=false + Error
export type MarketingGenerateResponse = {
  Success: boolean;
  Subject: string | null;
  Body: string | null;
  SignatureText: string | null;
  Error: string | null;
};

export type MarketingSendResponse = {
  Success: boolean;
  /** false = kein SMTP konfiguriert, Mail wurde nur im API-Log ausgegeben */
  SmtpConfigured: boolean;
  Error: string | null;
  ContactId: string | null;
};

// Enums serialisiert per globalem JsonStringEnumConverter als Text, nie numerisch
export type MarketingContactType =
  | "Unknown"
  | "Broker"
  | "PropertyManager"
  | "PrivateSeller"
  | "Municipality"
  | "Partner"
  | "Other";

export type MarketingContactStatus =
  | "Lead"
  | "Contacted"
  | "Replied"
  | "Interested"
  | "Customer"
  | "NotInterested"
  | "DoNotContact";

export type MarketingEmailStatus = "Sent" | "LoggedOnly";

export type MarketingStats = {
  TotalContacts: number;
  Leads: number;
  Contacted: number;
  Replied: number;
  Interested: number;
  Customers: number;
  NotInterested: number;
  EmailsSentTotal: number;
  EmailsSent30Days: number;
  RepliesTotal: number;
  Replies30Days: number;
  UnreadReplies: number;
  ReplyRatePercent: number | null;
};

export type MarketingContact = {
  Id: string;
  Email: string;
  Name: string | null;
  Company: string | null;
  Phone: string | null;
  ContactType: MarketingContactType;
  Status: MarketingContactStatus;
  Notes: string | null;
  Source: string | null;
  LastContactedAt: string | null;
  LastReplyAt: string | null;
  CreatedAt: string;
  EmailCount: number;
  ReplyCount: number;
};

export type MarketingContactsPage = {
  Contacts: MarketingContact[];
  Total: number;
  PageSize: number;
  CurrentPage: number;
  HasMore: boolean;
};

export type MarketingEmail = {
  Id: string;
  ContactId: string;
  ContactEmail: string;
  ContactName: string | null;
  Subject: string;
  Body: string;
  Keywords: string | null;
  Status: MarketingEmailStatus;
  SentAt: string;
  ReplyCount: number;
};

export type MarketingEmailsPage = {
  Emails: MarketingEmail[];
  Total: number;
  PageSize: number;
  CurrentPage: number;
  HasMore: boolean;
};

export type MarketingInboundEmail = {
  Id: string;
  ContactId: string | null;
  ContactName: string | null;
  FromAddress: string;
  FromName: string | null;
  Subject: string | null;
  BodyText: string | null;
  ReceivedAt: string;
  IsRead: boolean;
  RepliedToEmailId: string | null;
  RepliedToSubject: string | null;
};

export type MarketingInboxPage = {
  Items: MarketingInboundEmail[];
  Total: number;
  PageSize: number;
  CurrentPage: number;
  HasMore: boolean;
  ImapConfigured: boolean;
  SyncError: string | null;
};

export type MarketingContactDetail = {
  Contact: MarketingContact | null;
  Emails: MarketingEmail[];
  Replies: MarketingInboundEmail[];
};

export type MarketingSaveContactResponse = {
  Success: boolean;
  Id: string | null;
  Error: string | null;
};

export type MarketingSyncResponse = {
  Success: boolean;
  Added: number;
  Error: string | null;
};

function adminHeaders(): Record<string, string> {
  return {
    "content-type": "application/json",
    "x-admin-key": process.env.ADMIN_API_KEY ?? "",
  };
}

export async function adminApiGet<T>(pathWithQuery: string): Promise<T | null> {
  try {
    const response = await fetch(new URL(pathWithQuery, getServerApiBaseUrl()), {
      headers: adminHeaders(),
    });
    if (!response.ok) return null;
    return (await response.json()) as T;
  } catch {
    return null;
  }
}

/**
 * POST mit JSON-Antwort (z.B. Marketing-Generate/Send). Liefert null bei
 * Netzwerkfehler oder nicht-JSON-Antwort; API-Fehler kommen als Success=false
 * im Response-Body (die Handler geben auch bei Fehlern 200 + Error-Text zurueck).
 */
export async function adminApiPost<T>(pathWithQuery: string, body: unknown): Promise<T | null> {
  try {
    const response = await fetch(new URL(pathWithQuery, getServerApiBaseUrl()), {
      method: "POST",
      headers: adminHeaders(),
      body: JSON.stringify(body),
    });
    if (!response.ok) return null;
    return (await response.json()) as T;
  } catch {
    return null;
  }
}

/** Mutationen (POST/DELETE); liefert nur ok/nicht-ok - die Seiten zeigen ein Banner. */
export async function adminApiSend(
  pathWithQuery: string,
  method: "POST" | "DELETE",
  body?: unknown,
): Promise<boolean> {
  try {
    const response = await fetch(new URL(pathWithQuery, getServerApiBaseUrl()), {
      method,
      headers: adminHeaders(),
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    return response.ok;
  } catch {
    return false;
  }
}
