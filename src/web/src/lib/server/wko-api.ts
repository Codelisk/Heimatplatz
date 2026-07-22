/**
 * Serverseitiger Client fuer /api/wko-companies (Intern-Bereich).
 *
 * Anders als admin-api.ts: die WkoCompanies-Lese-Endpoints sind unauthentifiziert
 * (oeffentliche Firmenverzeichnis-Daten, kein X-Admin-Key noetig) - gleiches Muster wie
 * der ForeclosureAuctions-Sync-Status-Fetch in intern/index.astro.
 */
import { getServerApiBaseUrl } from "./api-base";

export type WkoCompanyPermit = {
  FachgruppeName: string | null;
  Description: string | null;
  ManagingDirector: string | null;
  GisaNumber: string | null;
  Since: string | null;
};

export type FirmenbuchPerson = {
  Name: string | null;
  BirthDate: string | null;
  Role: string | null;
};

export type WkoCompany = {
  Id: string;
  Name: string;
  CategoryText: string | null;
  Street: string | null;
  PostalCode: string | null;
  City: string | null;
  Phones: string[];
  Email: string | null;
  Website: string | null;
  OpeningHoursText: string | null;
  CompanyRegisterNumber: string | null;
  CompanyCourt: string | null;
  Gln: string | null;
  LegalForm: string | null;
  FoundedYear: number | null;
  IsTrainingCompany: boolean;
  Permits: WkoCompanyPermit[];
  Euid: string | null;
  FirmenbuchFoundedDate: string | null;
  FirmenbuchManagingDirectors: FirmenbuchPerson[];
  FirmenbuchEnrichedAt: string | null;
  CreatedAt: string;
  WkoFirmaId: string;
  DetailUrl: string;
  SourceSearchTerm: string | null;
  IsActive: boolean;
  FirstSeenAt: string | null;
  LastScrapedAt: string | null;
  RemovedAt: string | null;
};

export type WkoCompaniesPage = {
  Companies: WkoCompany[];
  TotalCount: number;
  Page: number;
  PageSize: number;
};

async function get<T>(pathWithQuery: string): Promise<T | null> {
  try {
    const response = await fetch(new URL(pathWithQuery, getServerApiBaseUrl()));
    if (!response.ok) return null;
    return (await response.json()) as T;
  } catch {
    return null;
  }
}

export function fetchWkoCompanies(query: URLSearchParams): Promise<WkoCompaniesPage | null> {
  return get<WkoCompaniesPage>(`/api/wko-companies/?${query}`);
}

export function fetchWkoCompanyById(id: string): Promise<WkoCompany | null> {
  return get<{ Company: WkoCompany | null }>(`/api/wko-companies/${id}`).then((r) => r?.Company ?? null);
}
