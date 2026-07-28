/**
 * Zentraler Lazy-Loader fuer maplibre-gl: seit v6 (ESM-only) findet MapLibre
 * seinen Web-Worker in Bundlern nicht mehr selbst (import.meta.url zeigt in
 * Vites Modulgraph nicht auf die Worker-Datei) - ohne setWorkerUrl startet der
 * Worker still nicht und die Karte laedt weder Tiles noch Glyphs noch GeoJSON.
 * Das ?url-Asset ist same-origin, die CSP ('self') bleibt unangetastet.
 */
import workerUrl from "maplibre-gl/dist/maplibre-gl-worker.mjs?url";

let loading: Promise<typeof import("maplibre-gl")> | null = null;

export function loadMaplibre(): Promise<typeof import("maplibre-gl")> {
  loading ??= import("maplibre-gl").then((maplibre) => {
    maplibre.setWorkerUrl(workerUrl);
    return maplibre;
  });
  return loading;
}
