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

/** Signatur-Vorschau fuer die Compose-Seite (GET /api/admin/marketing/email/signature) */
export type MarketingSignatureResponse = {
  SignatureText: string;
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
  | "DoNotContact"
  | "ToContact"
  | "FollowUp";

export type MarketingActivityType = "Note" | "Call" | "StatusChange" | "FollowUp" | "Meeting";

// Deutsche Enum-Namen (Anrede ist ein deutsches Domaenenwort, keine Mr/Mrs-Uebersetzung)
export type MarketingSalutation = "Unknown" | "Herr" | "Frau";

export type MarketingEmailStatus = "Sent" | "LoggedOnly" | "DeliveryFailed";

export type MarketingStats = {
  TotalContacts: number;
  Leads: number;
  ToContact: number;
  FollowUpDue: number;
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

// Email ist optional: Kontakte aus dem Firmenpool entstehen ohne Adresse (das Firmenbuch
// fuehrt keine Kontaktdaten) und bekommen sie erst beim Telefonat.
export type MarketingContact = {
  Id: string;
  Email: string | null;
  /** Anzeigename: aus Titel/Vorname/Nachname zusammengesetzt bzw. Alt-Bestand */
  Name: string | null;
  Salutation: MarketingSalutation;
  Title: string | null;
  FirstName: string | null;
  LastName: string | null;
  Company: string | null;
  Phone: string | null;
  City: string | null;
  ContactType: MarketingContactType;
  Status: MarketingContactStatus;
  Notes: string | null;
  Source: string | null;
  FirmenbuchFnr: string | null;
  NextFollowUpAt: string | null;
  LastContactedAt: string | null;
  LastReplyAt: string | null;
  CreatedAt: string;
  EmailCount: number;
  ReplyCount: number;
};

/** Verteilung je Funnel-Status fuer die Pipeline-Chips (Status ohne Kontakte fehlen) */
export type MarketingStatusCount = {
  Status: MarketingContactStatus;
  Count: number;
};

export type MarketingContactsPage = {
  Contacts: MarketingContact[];
  Total: number;
  PageSize: number;
  CurrentPage: number;
  HasMore: boolean;
  /** Suche/Typ-Filter wirken auf die Zaehler, der Status-Filter bewusst nicht */
  StatusCounts: MarketingStatusCount[];
  DueCount: number;
};

/**
 * Antwort der Akquise-Schnellaktion (/api/admin/marketing/contacts/quick).
 * PreviousStatus/PreviousFollowUpAt tragen den Zustand vor der Aktion - Grundlage
 * fuer das "Rueckgaengig" in der Kontaktliste (Action=Restore).
 */
export type MarketingQuickActionResponse = {
  Success: boolean;
  Error: string | null;
  Status: MarketingContactStatus | null;
  NextFollowUpAt: string | null;
  PreviousStatus: MarketingContactStatus | null;
  PreviousFollowUpAt: string | null;
};

export type MarketingEmail = {
  Id: string;
  ContactId: string;
  ContactEmail: string | null;
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
  /** Unzustellbarkeits-Meldung (Bounce) statt echter Antwort */
  IsBounce: boolean;
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

export type MarketingActivity = {
  Id: string;
  ContactId: string;
  Type: MarketingActivityType;
  Notes: string | null;
  StatusFrom: MarketingContactStatus | null;
  StatusTo: MarketingContactStatus | null;
  FollowUpAt: string | null;
  OccurredAt: string;
};

/** Zusatzadresse eines Kontakts; Source "Manuell" oder "Posteingang" (automatisch gelernt) */
export type MarketingContactAdditionalEmail = {
  Id: string;
  Email: string;
  Source: string | null;
  CreatedAt: string;
};

export type MarketingContactDetail = {
  Contact: MarketingContact | null;
  Emails: MarketingEmail[];
  Replies: MarketingInboundEmail[];
  Activities: MarketingActivity[];
  AdditionalEmails: MarketingContactAdditionalEmail[];
};

/** Ergebnis von contacts/emails/add bzw. /remove */
export type MarketingContactEmailActionResponse = {
  Success: boolean;
  Error: string | null;
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

/** Antwort auf eine Posteingang-Rueckmeldung (POST /api/admin/marketing/inbox/reply) */
export type MarketingReplyResponse = {
  Success: boolean;
  /** false = kein SMTP konfiguriert, Mail wurde nur im API-Log ausgegeben */
  SmtpConfigured: boolean;
  Error: string | null;
};

/**
 * KI-Pruefung eines Antwort-Entwurfs (POST /api/admin/marketing/inbox/reply-check).
 * CorrectedText/SuggestedText null = nichts zu korrigieren bzw. kein Vorschlag.
 */
export type MarketingReplyCheckResponse = {
  Success: boolean;
  FitsContext: boolean;
  ContextNote: string | null;
  CorrectedText: string | null;
  SuggestedText: string | null;
  Error: string | null;
};

// Firmenpool: aufrechte Firmenbuch-Firmen mit Immobilien-Namensbezug - live aus der
// Firmenpool-API, Heimatplatz haelt keinen eigenen Firmenkatalog mehr.
// ContactId gesetzt = bereits als Kontakt uebernommen (Schluessel ist die FNR).
export type MarketingLead = {
  Fnr: string;
  Name: string;
  Sitz: string | null;
  RechtsformText: string | null;
  ContactId: string | null;
  ContactStatus: MarketingContactStatus | null;
};

export type MarketingLeadPoolPage = {
  Leads: MarketingLead[];
  Total: number;
  PageSize: number;
  CurrentPage: number;
  HasMore: boolean;
};

// Firmenpool-Detailansicht: voller Firmendatensatz (Auszug, Funktionaere, Gewerbe)
// live aus der Firmenpool-API. Company == null heisst: FNR dort unbekannt.
export type MarketingLeadOfficer = {
  Name: string;
  FunktionText: string | null;
  Seit: string | null;
  Aktiv: boolean;
};

export type MarketingLeadTrade = {
  GisaZahl: number;
  Wortlaut: string | null;
  Plz: string | null;
  Ort: string | null;
  WeitereStandorte: string[];
  Aktiv: boolean;
};

export type MarketingLeadCompany = {
  Fnr: string;
  Name: string;
  Status: string | null;
  Euid: string | null;
  Gegruendet: string | null;
  Strasse: string | null;
  Hausnummer: string | null;
  Plz: string | null;
  Ort: string | null;
  Staat: string | null;
  Sitz: string | null;
  RechtsformCode: string | null;
  RechtsformText: string | null;
  GerichtText: string | null;
  Handelsregisternummer: string | null;
  AuszugStand: string | null;
  AbschluesseVorhanden: number;
  Funktionaere: MarketingLeadOfficer[];
  Gewerbe: MarketingLeadTrade[];
};

export type MarketingLeadCompanyResponse = {
  Company: MarketingLeadCompany | null;
  ContactId: string | null;
  ContactStatus: MarketingContactStatus | null;
};

export type MarketingAddLeadsResponse = {
  Success: boolean;
  Added: number;
  Skipped: number;
  Error: string | null;
};

export type MarketingTemplate = {
  Id: string;
  Name: string;
  Description: string | null;
  Subject: string;
  Body: string;
  IsActive: boolean;
  DisplayOrder: number;
  CreatedAt: string;
};

export type MarketingTemplatesResponse = {
  Templates: MarketingTemplate[];
};

// Antwort von /templates/save (eigener Typ statt Kontakt-Save wiederzuverwenden -
// gleiche Form, aber anderer Endpunkt, damit die Kopplung nicht implizit ist)
export type MarketingSaveTemplateResponse = {
  Success: boolean;
  Id: string | null;
  Error: string | null;
};

export type MarketingRenderTemplateResponse = {
  Success: boolean;
  Subject: string | null;
  Body: string | null;
  Error: string | null;
  /** Nicht befuellbare Platzhalter, Anrede-Fallback etc. - Platzhalter bleiben dann im Text */
  Warnings: string[] | null;
};

export type MarketingActivityResponse = {
  Success: boolean;
  Id: string | null;
  Error: string | null;
};

// Feedback-Feature (/api/admin/feedback) - Nutzer-Anfragen mit Verlauf und Anhaengen.
// Enums serialisiert per globalem JsonStringEnumConverter als Text, nie numerisch.
export type FeedbackCategory = "Idea" | "Problem" | "Question" | "Praise" | "Other";

export type FeedbackTicketStatus = "Open" | "InProgress" | "Answered" | "Closed";

export type FeedbackSource = "Unknown" | "Web" | "Android" | "Ios" | "Windows";

export type FeedbackAttachment = {
  Id: string;
  Kind: "Image" | "Audio";
  /** Absolute URL (Original bzw. Display-Variante bei Bildern) */
  Url: string;
  /** Skalierte Bild-Vorschau via /api/images/local (null bei Audio) */
  ThumbnailUrl: string | null;
  ContentType: string;
  FileSizeBytes: number;
  DurationSeconds: number | null;
};

export type FeedbackMessage = {
  Id: string;
  Author: "User" | "Team";
  Body: string;
  CreatedAt: string;
  Attachments: FeedbackAttachment[];
};

export type AdminFeedbackTicket = {
  Id: string;
  Category: FeedbackCategory;
  Subject: string;
  Status: FeedbackTicketStatus;
  CreatedAt: string;
  LastMessageAt: string;
  HasUnreadFromUser: boolean;
  MessageCount: number;
  LastMessagePreview: string;
  UserId: string;
  UserName: string | null;
  UserEmail: string | null;
  Source: FeedbackSource;
  AppVersion: string | null;
};

export type AdminFeedbackTicketsPage = {
  Tickets: AdminFeedbackTicket[];
  Total: number;
  PageSize: number;
  Page: number;
  HasMore: boolean;
};

export type AdminFeedbackTicketDetail = {
  Id: string;
  Category: FeedbackCategory;
  Subject: string;
  Status: FeedbackTicketStatus;
  CreatedAt: string;
  UserId: string;
  UserName: string | null;
  UserEmail: string | null;
  Source: FeedbackSource;
  AppVersion: string | null;
  Messages: FeedbackMessage[];
};

export type AdminFeedbackTicketDetailResponse = {
  Ticket: AdminFeedbackTicketDetail | null;
};

export type FeedbackReplyResponse = {
  Success: boolean;
  MessageId: string | null;
  Error: string | null;
};

export type FeedbackStatusResponse = {
  Success: boolean;
  Error: string | null;
};

export type AdminFeedbackStats = {
  Total: number;
  Open: number;
  InProgress: number;
  UnreadFromUser: number;
};

function adminHeaders(): Record<string, string> {
  return {
    "content-type": "application/json",
    "x-admin-key": process.env.ADMIN_API_KEY ?? "",
  };
}

// SearchConsole-Feature (/api/admin/search-console) - fail-soft: Enabled=false statt Fehler,
// solange kein Service-Account-Key konfiguriert ist (siehe Features/SearchConsole/README.md)
export type SearchConsoleRow = {
  Label: string;
  Clicks: number;
  Impressions: number;
  Ctr: number;
  Position: number;
};

export type SearchConsoleSummary = {
  Enabled: boolean;
  ClicksTotal: number;
  ImpressionsTotal: number;
  AverageCtr: number;
  AveragePosition: number;
  TopQueries: SearchConsoleRow[];
  TopPages: SearchConsoleRow[];
};

// Kontakt-Stammdaten (/api/admin/legal/*) - beide Endpoints liefern auch bei fachlichen
// Fehlern 200 mit Success=false + Error-Text, damit /intern/kontakt konkret melden kann
export type LegalUpdateResponse = {
  Success: boolean;
  Error: string | null;
};

// Partner-Feature (/api/admin/partners) - Pflege der oeffentlichen /partner/-Seite.
// Fachliche Fehler kommen wie bei Legal als Success=false + Error (HTTP 200).
export type AdminPartner = {
  Id: string;
  Name: string;
  /** PartnerCategories-Konstante: "Broker" | "DataSource" */
  Category: string;
  Description: string | null;
  WebsiteUrl: string | null;
  LogoUrl: string | null;
  Region: string | null;
  PartnerSinceYear: number | null;
  SourceName: string | null;
  SellerName: string | null;
  DisplayOrder: number;
  IsVisible: boolean;
  /** Live-Zaehlung ueber Property.SourceName (nur sichtbare Inserate) */
  ActiveListingCount: number;
};

export type AdminPartnersResponse = {
  Partners: AdminPartner[];
};

export type PartnerSaveResponse = {
  Success: boolean;
  Error: string | null;
  Id: string | null;
};

export type PartnerDeleteResponse = {
  Success: boolean;
  Error: string | null;
};

export type PartnerLogoUploadResponse = {
  Success: boolean;
  Error: string | null;
  LogoUrl: string | null;
};

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
