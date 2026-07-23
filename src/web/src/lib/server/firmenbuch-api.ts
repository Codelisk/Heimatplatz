/**
 * Serverseitiger Client fuer den lokalen Firmenbuch-Katalog.
 *
 * Die Lese-API ist oeffentlich; der Client bleibt trotzdem serverseitig, damit
 * die interne Astro-Seite ihre Daten bereits beim Rendern vollstaendig hat.
 */
import { getServerApiBaseUrl } from "./api-base";

export type FirmenbuchCompany = {
  Id: string;
  Fnr: string;
  Name: string;
  Status: string | null;
  Sitz: string | null;
  RechtsformCode: string | null;
  RechtsformText: string | null;
  Rechtseigenschaft: string | null;
  GerichtCode: string | null;
  GerichtText: string | null;
  SourceOrtNr: string | null;
  FirstSeenAt: string;
  LastSeenAt: string;
};

type FirmenbuchCompaniesPage = {
  Companies: FirmenbuchCompany[];
  TotalCount: number;
  Page: number;
  PageSize: number;
};

/**
 * Die bestehende Katalog-API kann exakt nach der Firmenbuchnummer suchen.
 * Der exakte Abgleich verhindert, dass ein unerwarteter Teiltreffer als Detail
 * einer anderen Firma angezeigt wird.
 */
export async function fetchFirmenbuchCompanyByFnr(
  fnr: string,
): Promise<FirmenbuchCompany | null> {
  const normalizedFnr = fnr.trim();
  if (!normalizedFnr) return null;

  const query = new URLSearchParams({
    Page: "1",
    PageSize: "10",
    SearchText: normalizedFnr,
  });

  try {
    const response = await fetch(
      new URL(`/api/firmenbuch/companies?${query}`, getServerApiBaseUrl()),
    );
    if (!response.ok) return null;

    const data = (await response.json()) as FirmenbuchCompaniesPage;
    return (
      data.Companies.find(
        (company) => company.Fnr.toLocaleLowerCase("de-AT") === normalizedFnr.toLocaleLowerCase("de-AT"),
      ) ?? null
    );
  } catch {
    return null;
  }
}
