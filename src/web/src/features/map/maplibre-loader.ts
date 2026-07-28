/**
 * Zentraler Lazy-Loader fuer maplibre-gl: seit v6 (ESM-only) findet MapLibre
 * seinen Web-Worker in Bundlern nicht mehr selbst (import.meta.url zeigt in
 * Vites Modulgraph nicht auf die Worker-Datei) - ohne setWorkerUrl startet der
 * Worker still nicht und die Karte laedt weder Tiles noch Glyphs noch GeoJSON.
 *
 * WICHTIG: ?worker&url (nicht nur ?url)! Ein blosses ?url kopiert die
 * Worker-Datei unveraendert - ihr relativer Import ./maplibre-gl-shared.mjs
 * laeuft im Prod-Build dann auf 404 und der Worker stirbt still (im Dev-Server
 * unsichtbar, weil Vite dort Module on-the-fly aufloest). ?worker&url laesst
 * Vite den Worker SAMT Abhaengigkeiten als eigenes Asset bundeln.
 * Das Asset ist same-origin, die CSP ('self' + worker-src blob:) passt.
 */
import workerUrl from "maplibre-gl/dist/maplibre-gl-worker.mjs?worker&url";

let loading: Promise<typeof import("maplibre-gl")> | null = null;

export function loadMaplibre(): Promise<typeof import("maplibre-gl")> {
  loading ??= import("maplibre-gl").then((maplibre) => {
    maplibre.setWorkerUrl(workerUrl);
    return maplibre;
  });
  return loading;
}
