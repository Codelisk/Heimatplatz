// Exportiert den Web-Kartenstil (buildMapStyle aus src/features/map/map-style.ts)
// als statische JSON-Dateien fuer die native MAUI-Karte (MapLibre Native).
//
// Warum ein Export statt Laufzeit-Teilung: MapLibre Native kann keine relativen
// URLs aufloesen (pmtiles://https://... muss vollstaendig sein) und die App soll
// die Karte auch ohne vorherigen Web-Request aufbauen koennen. Die Origin wird
// deshalb als Platzhalter geschrieben und in der App zur Laufzeit ersetzt
// (MapStyleProvider waehlt test./www. passend zum aktiven API-Endpunkt).
//
// Aufruf: npm run export:map-styles   (nach jeder Aenderung an map-style.ts!)
import { mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const scriptDir = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptDir, "..");
const mapDir = join(webRoot, "src", "features", "map");
const outDir = resolve(webRoot, "..", "maui", "src", "Heimatplatz.Maui", "Resources", "Raw", "Map");

// Node kann extensionslose TS-Imports nicht aufloesen (map-style.ts importiert
// "./ooe-geometry") - gepatchte Kopien in ein Temp-Verzeichnis legen und von
// dort importieren. Der @protomaps/basemaps-Import zeigt direkt auf den
// ESM-Einstieg im node_modules des Web-Projekts.
const tempDir = mkdtempSync(join(tmpdir(), "hp-map-style-"));
try {
  const basemapsEntry = pathToFileURL(
    join(webRoot, "node_modules", "@protomaps", "basemaps", "dist", "esm", "index.js"),
  ).href;
  const styleSource = readFileSync(join(mapDir, "map-style.ts"), "utf8")
    .replace('from "@protomaps/basemaps"', `from "${basemapsEntry}"`)
    .replace('from "./ooe-geometry"', 'from "./ooe-geometry.ts"');
  writeFileSync(join(tempDir, "map-style.ts"), styleSource);
  writeFileSync(join(tempDir, "ooe-geometry.ts"), readFileSync(join(mapDir, "ooe-geometry.ts"), "utf8"));

  const { buildMapStyle } = await import(pathToFileURL(join(tempDir, "map-style.ts")).href);

  const ORIGIN = "https://__MAP_ORIGIN__";
  mkdirSync(outDir, { recursive: true });

  for (const dark of [false, true]) {
    for (const tilesAvailable of [true, false]) {
      const style = buildMapStyle({
        dark,
        tilesAvailable,
        tilesUrl: `${ORIGIN}/tiles/oberoesterreich.pmtiles`,
        assetsUrl: `${ORIGIN}/tiles/assets`,
      });
      // Auch der Fallback-Stil (ohne Tiles) braucht Glyphs/Sprites: die nativen
      // Pin- und Stempel-Layer der App sind Text-Layer und rendern sonst nichts.
      style.glyphs ??= `${ORIGIN}/tiles/assets/fonts/{fontstack}/{range}.pbf`;
      style.sprite ??= `${ORIGIN}/tiles/assets/sprites/v4/${dark ? "dark" : "light"}`;

      const file = join(outDir, `mapstyle-${dark ? "dark" : "light"}${tilesAvailable ? "" : "-fallback"}.json`);
      writeFileSync(file, JSON.stringify(style));
      console.log(`geschrieben: ${file}`);
    }
  }
} finally {
  rmSync(tempDir, { recursive: true, force: true });
}
