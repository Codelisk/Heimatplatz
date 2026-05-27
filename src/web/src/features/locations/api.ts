import { SITE } from "@/config/site";

type ApiMunicipality = {
  Id: string;
  Name: string;
};

type ApiDistrict = {
  Name: string;
  Municipalities?: ApiMunicipality[];
};

type ApiFederalProvince = {
  Name: string;
  Districts?: ApiDistrict[];
};

type ApiLocationsResponse = {
  FederalProvinces?: ApiFederalProvince[];
};

const districtRegionSlugs: Record<string, string> = {
  braunau: "braunau-am-inn",
  eferding: "eferding",
  freistadt: "freistadt",
  gmunden: "gmunden",
  grieskirchen: "grieskirchen",
  kirchdorf: "kirchdorf-an-der-krems",
  "linz-land": "linz-land",
  perg: "perg",
  ried: "ried-im-innkreis",
  rohrbach: "rohrbach",
  scharding: "schaerding",
  "stadt-linz": "linz",
  "stadt-steyr": "steyr",
  "stadt-wels": "wels",
  "steyr-land": "steyr-land",
  "urfahr-umgebung": "urfahr-umgebung",
  vocklabruck: "voecklabruck",
  "wels-land": "wels-land",
};

let municipalityRegionPromise: Promise<Record<string, string>> | null = null;

function slugify(value: string) {
  return value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

async function fetchMunicipalityRegionSlugsUncached() {
  const response = await fetch(new URL("/api/locations", SITE.apiBaseUrl), {
    headers: { Accept: "application/json" },
  });
  if (!response.ok) throw new Error(`API ${response.status}`);

  const payload = await response.json() as ApiLocationsResponse;
  const map: Record<string, string> = {};

  payload.FederalProvinces?.forEach((province) => {
    province.Districts?.forEach((district) => {
      const regionSlug = districtRegionSlugs[slugify(district.Name)];
      if (!regionSlug) return;

      district.Municipalities?.forEach((municipality) => {
        if (municipality.Id) map[municipality.Id.toLowerCase()] = regionSlug;
      });
    });
  });

  return map;
}

export function fetchMunicipalityRegionSlugs() {
  municipalityRegionPromise ??= fetchMunicipalityRegionSlugsUncached().catch((error) => {
    console.warn("[Heimatplatz] Locations could not be pre-rendered", error);
    return {};
  });

  return municipalityRegionPromise;
}
