/**
 * MapLibre-Kartenstil der Faltkarte: Protomaps-Basemap-Layer in den
 * Heimatplatz-Papiertoenen (hell = Manila-Wanderkarte, dunkel = Tafelkarte),
 * plus eigene GeoJSON-Overlays (Landesgrenze immer; Donau/Seen nur als
 * Fallback, wenn keine Vector-Tiles konfiguriert/erreichbar sind - dann sieht
 * die Karte aus wie die gezeichnete Phase-1-Papierkarte, nur frei zoombar).
 *
 * Tiles/Assets kommen ausschliesslich von der eigenen Origin (PMTiles via
 * Range-Requests, Fonts/Sprites statisch) - die CSP bleibt bei 'self'.
 *
 * Client-sicher: keine Server-Imports. Farbwerte sind Hex-Naeherungen der
 * starwind-Tokens (MapLibre kann keine CSS-Variablen aufloesen).
 */

import { layers as basemapLayers, namedFlavor } from "@protomaps/basemaps";
import type { LayerSpecification, StyleSpecification, SymbolLayerSpecification } from "maplibre-gl";
import type { Feature, FeatureCollection, LineString, Polygon } from "geojson";
import { DANUBE_WAYPOINTS, LAKES, OOE_OUTLINE, type LngLat } from "./ooe-geometry";

/** [WSW, ENE] Bounding-Box Oberoesterreichs fuer fitBounds/Start-Ausschnitt. */
export const OOE_BOUNDS: [[number, number], [number, number]] = [
  [12.7492, 47.4611],
  [14.9922, 48.7726],
];

/**
 * Pan-Begrenzung: knapp um OÖ. Der Rand existiert nur, damit Viewports mit
 * anderem Seitenverhaeltnis (Hochformat-Handys!) das ganze Land einpassen
 * koennen - sichtbar ist dort ohnehin nur noch leeres Papier, das Umland wird
 * seit 28.07.2026 vollstaendig maskiert (hpmap-outside-dim, Produktwunsch:
 * Fokus ausschliesslich auf Oberoesterreich).
 */
export const OOE_MAX_BOUNDS: [[number, number], [number, number]] = [
  [12.2, 46.9],
  [15.5, 49.3],
];

// Heimatplatz-Papiertoene (Hex-Naeherungen der oklch-Tokens aus starwind.css)
const LIGHT = {
  paper: "#f2ecda",
  paperDeep: "#e7ddc4",
  ink: "#3a332d",
  inkSoft: "#6b6053",
  inkFaint: "#8a7f6f",
  halo: "#f6f1e3",
  water: "#a9c2d2",
  park: "#e1e6cb",
  wood: "#d5deba",
  building: "#e7dfcb",
  road: "#fcfaf2",
  roadCasing: "#d9d1bd",
  highway: "#f5e9c9",
  highwayCasing: "#d3c39a",
};

const DARK = {
  paper: "#221f1b",
  paperDeep: "#1b1815",
  ink: "#e6e1d8",
  inkSoft: "#a79d8f",
  inkFaint: "#7d7466",
  halo: "#221f1b",
  water: "#32404c",
  park: "#262b22",
  wood: "#222719",
  building: "#292520",
  road: "#2e2a25",
  roadCasing: "#171512",
  highway: "#3d362c",
  highwayCasing: "#14110e",
};

function buildFlavor(dark: boolean) {
  const base = namedFlavor(dark ? "dark" : "light");
  const tone = dark ? DARK : LIGHT;
  return {
    ...base,
    // Landton statt paperDeep: jenseits der Kachelabdeckung scheint der
    // background durch - im Landton wirkt das als Papierrand statt als Kante
    // (Hochformat-Handys zeigen bei "ganz OOE" mehr Flaeche als die Kacheln)
    background: tone.paper,
    earth: tone.paper,
    water: tone.water,
    park_a: tone.park,
    park_b: tone.park,
    wood_a: tone.wood,
    wood_b: tone.wood,
    scrub_a: tone.park,
    scrub_b: tone.park,
    glacier: tone.paper,
    sand: tone.paperDeep,
    beach: tone.paperDeep,
    buildings: tone.building,
    minor_service: tone.road,
    minor_a: tone.road,
    minor_b: tone.road,
    link: tone.road,
    other: tone.road,
    major: tone.road,
    highway: tone.highway,
    minor_service_casing: tone.roadCasing,
    minor_casing: tone.roadCasing,
    link_casing: tone.roadCasing,
    major_casing_early: tone.roadCasing,
    major_casing_late: tone.roadCasing,
    highway_casing_early: tone.highwayCasing,
    highway_casing_late: tone.highwayCasing,
    boundaries: tone.inkFaint,
    railway: tone.roadCasing,
    roads_label_minor: tone.inkSoft,
    roads_label_minor_halo: tone.halo,
    roads_label_major: tone.inkSoft,
    roads_label_major_halo: tone.halo,
    ocean_label: tone.inkSoft,
    subplace_label: tone.inkSoft,
    subplace_label_halo: tone.halo,
    city_label: tone.ink,
    city_label_halo: tone.halo,
    state_label: tone.inkFaint,
    state_label_halo: tone.halo,
    country_label: tone.inkFaint,
    address_label: tone.inkSoft,
    address_label_halo: tone.halo,
  };
}

function toLineString(points: LngLat[]): Feature<LineString> {
  return { type: "Feature", properties: {}, geometry: { type: "LineString", coordinates: points } };
}

function outlinePolygon(): Feature<Polygon> {
  return {
    type: "Feature",
    properties: {},
    geometry: { type: "Polygon", coordinates: [[...OOE_OUTLINE, OOE_OUTLINE[0]]] },
  };
}

/** Welt-Ring mit OÖ-Loch: dimmt mit Tiles das Umland (amtlicher Fokus-Effekt). */
function outsideMask(): Feature<Polygon> {
  const world: LngLat[] = [[-179, -85], [179, -85], [179, 85], [-179, 85], [-179, -85]];
  return {
    type: "Feature",
    properties: {},
    geometry: { type: "Polygon", coordinates: [world, [...OOE_OUTLINE, OOE_OUTLINE[0]]] },
  };
}

/** Seen-Ellipsen als Polygone (nur Fallback ohne Tiles - echte Tiles haben Wasser). */
function lakePolygons(): FeatureCollection {
  const features: Feature[] = LAKES.map(([lng, lat, rxKm, ryKm, rotationDeg]) => {
    const rotation = (rotationDeg * Math.PI) / 180;
    const points: LngLat[] = [];
    for (let i = 0; i <= 24; i++) {
      const t = (i / 24) * 2 * Math.PI;
      const dx = rxKm * Math.cos(t);
      const dy = ryKm * Math.sin(t);
      const eastKm = dx * Math.cos(rotation) - dy * Math.sin(rotation);
      const northKm = dx * Math.sin(rotation) + dy * Math.cos(rotation);
      points.push([
        lng + eastKm / (111.32 * Math.cos((lat * Math.PI) / 180)),
        lat + northKm / 111.32,
      ]);
    }
    return { type: "Feature", properties: {}, geometry: { type: "Polygon", coordinates: [points] } };
  });
  return { type: "FeatureCollection", features };
}

/**
 * Kreis-Polygon um eine Position (Mini-Karte der Detailseite: ungenaue Lagen
 * werden als Umgebungskreis statt als punktgenauer Pin gezeigt).
 */
export function locationCirclePolygon(lng: number, lat: number, radiusMeters: number): Feature<Polygon> {
  const points: LngLat[] = [];
  for (let i = 0; i <= 48; i++) {
    const angle = (i / 48) * 2 * Math.PI;
    const eastKm = (radiusMeters / 1000) * Math.cos(angle);
    const northKm = (radiusMeters / 1000) * Math.sin(angle);
    points.push([
      lng + eastKm / (111.32 * Math.cos((lat * Math.PI) / 180)),
      lat + northKm / 111.32,
    ]);
  }
  return { type: "Feature", properties: {}, geometry: { type: "Polygon", coordinates: [points] } };
}

/** Markenrot (Logo, themenunabhaengig) fuer Lage-Markierungen auf der Karte. */
export const BRAND_RED = "#de2a2f";

/**
 * Konvertiert die deprecated Filter-Syntax der protomaps-Basemap-Layer
 * (["==","kind","address"], ["in","kind","a","b"], ...) in Expression-Syntax,
 * damit sie mit der within-Expression kombinierbar ist. Liefert null, wenn der
 * Filter bereits eine Expression ist oder ein unbekannter Operator auftaucht -
 * der Aufrufer nutzt ihn dann unveraendert.
 */
function legacyFilterToExpression(filter: unknown): unknown[] | null {
  if (!Array.isArray(filter) || filter.length === 0) return null;
  const [op, ...rest] = filter as [string, ...unknown[]];
  const keyToGet = (key: unknown): unknown[] | null =>
    typeof key !== "string" ? null : key === "$type" ? ["geometry-type"] : ["get", key];

  switch (op) {
    case "all":
    case "any": {
      const parts = rest.map((part) => legacyFilterToExpression(part));
      return parts.every((part): part is unknown[] => part !== null) ? [op, ...parts] : null;
    }
    case "==":
    case "!=":
    case "<":
    case "<=":
    case ">":
    case ">=": {
      const get = keyToGet(rest[0]);
      return get ? [op, get, rest[1]] : null;
    }
    case "in":
    case "!in": {
      const get = keyToGet(rest[0]);
      if (!get) return null;
      const inExpression = ["in", get, ["literal", rest.slice(1)]];
      return op === "in" ? inExpression : ["!", inExpression];
    }
    case "has":
      return typeof rest[0] === "string" ? ["has", rest[0]] : null;
    case "!has":
      return typeof rest[0] === "string" ? ["!", ["has", rest[0]]] : null;
    default:
      return null;
  }
}

export type MapStyleOptions = {
  dark: boolean;
  /** PMTiles erreichbar? Ohne Tiles rendert der Stil die Papier-Fallback-Karte. */
  tilesAvailable: boolean;
  /** z.B. "/tiles/oberoesterreich.pmtiles" (same-origin, Range-Requests via Caddy) */
  tilesUrl: string;
  /** Basis fuer Fonts/Sprites, z.B. "/tiles/assets" */
  assetsUrl: string;
};

export function buildMapStyle(options: MapStyleOptions): StyleSpecification {
  const tone = options.dark ? DARK : LIGHT;

  const sources: StyleSpecification["sources"] = {
    "hpmap-outline": {
      type: "geojson",
      data: outlinePolygon(),
      attribution: "Grenzen: <a href=\"https://www.statistik.at\">Statistik Austria</a> (CC BY 4.0)",
    },
    "hpmap-outside": { type: "geojson", data: outsideMask() },
  };

  const styleLayers: LayerSpecification[] = [];

  if (options.tilesAvailable) {
    sources.protomaps = {
      type: "vector",
      url: `pmtiles://${options.tilesUrl}`,
      attribution: "© <a href=\"https://openstreetmap.org/copyright\">OpenStreetMap</a>-Mitwirkende, <a href=\"https://protomaps.com\">Protomaps</a>",
    };

    // Harter OOE-Fokus (Produktwunsch 28.07.2026): ausserhalb der Landesgrenze
    // ist NICHTS zu sehen. Zwei Mechanismen, weil einer nicht reicht:
    //  1. Labels/POIs (Symbol-Layer) rendern in MapLibre in einem eigenen Pass
    //     UEBER allen Flaechen - eine Deck-Maske kann sie nicht uebermalen.
    //     Deshalb bekommt jeder Symbol-Layer einen within-Filter: nur Features
    //     innerhalb des OOE-Umrisses werden ueberhaupt platziert.
    //  2. Geometrie (Strassen, Wasser, Flaechen) deckt die opake Maske darunter ab.
    // Die protomaps-Basemap-Filter sind noch Legacy-Syntax; within gibt es nur
    // als Expression und mischen verbietet MapLibre - also erst konvertieren.
    const ooePolygon = outlinePolygon();
    const withinOoe = ["within", ooePolygon];
    const basemap = basemapLayers("protomaps", buildFlavor(options.dark), { lang: "de" });
    for (const layer of basemap) {
      if (layer.type === "symbol") {
        const existing = layer.filter === undefined ? null : (legacyFilterToExpression(layer.filter) ?? layer.filter);
        layer.filter = (existing ? ["all", existing, withinOoe] : withinOoe) as SymbolLayerSpecification["filter"];
      }
    }
    styleLayers.push(...basemap);

    styleLayers.push({
      id: "hpmap-outside-dim",
      type: "fill",
      source: "hpmap-outside",
      paint: { "fill-color": tone.paper, "fill-opacity": 1 },
    });
  } else {
    // Fallback: gezeichnete Papierkarte wie Phase 1, nur frei zoombar
    sources["hpmap-danube"] = { type: "geojson", data: toLineString(DANUBE_WAYPOINTS) };
    sources["hpmap-lakes"] = { type: "geojson", data: lakePolygons() };
    styleLayers.push(
      { id: "hpmap-paper", type: "background", paint: { "background-color": tone.paperDeep } },
      {
        id: "hpmap-earth",
        type: "fill",
        source: "hpmap-outline",
        paint: { "fill-color": tone.paper },
      },
      {
        id: "hpmap-danube",
        type: "line",
        source: "hpmap-danube",
        paint: { "line-color": tone.water, "line-width": 3, "line-opacity": 0.8 },
        layout: { "line-cap": "round", "line-join": "round" },
      },
      {
        id: "hpmap-lakes",
        type: "fill",
        source: "hpmap-lakes",
        paint: { "fill-color": tone.water, "fill-opacity": 0.6 },
      },
    );
  }

  // Landesgrenze immer: Schummer-Halo + Tintenlinie (amtliche Kartensprache)
  styleLayers.push(
    {
      id: "hpmap-outline-halo",
      type: "line",
      source: "hpmap-outline",
      paint: { "line-color": tone.inkFaint, "line-width": 5, "line-opacity": 0.22 },
      layout: { "line-join": "round" },
    },
    {
      id: "hpmap-outline-line",
      type: "line",
      source: "hpmap-outline",
      paint: { "line-color": tone.ink, "line-width": 1.6, "line-opacity": 0.6 },
      layout: { "line-join": "round" },
    },
  );

  const style: StyleSpecification = {
    version: 8,
    sources,
    layers: styleLayers,
  };

  if (options.tilesAvailable) {
    // Fonts/Sprites nur noetig, wenn die Basemap-Layer Text/Icons rendern
    style.glyphs = `${options.assetsUrl}/fonts/{fontstack}/{range}.pbf`;
    style.sprite = `${options.assetsUrl}/sprites/v4/${options.dark ? "dark" : "light"}`;
  }

  return style;
}
