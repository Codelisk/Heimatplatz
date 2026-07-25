/**
 * Beschafft die selbst gehosteten Karten-Assets der Faltkarte:
 *  1. PMTiles-Ausschnitt Oberoesterreich (+Rand) aus dem taeglichen
 *     Protomaps-Planet-Build (nur der benoetigte Bereich wird per
 *     Range-Requests geladen, kein Planet-Download).
 *  2. Fonts (Noto Sans Regular/Medium/Italic) und Sprites aus
 *     protomaps/basemaps-assets.
 *
 * Nutzung:
 *   npm run map-tiles                 -> nach public/tiles (lokale Entwicklung)
 *   node scripts/fetch-map-tiles.mjs --out /pfad   -> beliebiges Ziel (Server)
 *
 * Die Ausgabe ist bewusst NICHT im Git (src/web/.gitignore): ~100-300 MB.
 * Fuer Prod wird der Ordner nach deploy/hetzner/map-tiles/ auf den Server
 * hochgeladen (Caddy served ihn unter /tiles/*, siehe Caddyfile).
 *
 * Lizenz der Daten: (c) OpenStreetMap-Mitwirkende (ODbL), Builds von Protomaps.
 */

import { execFileSync } from "node:child_process";
import { createWriteStream, existsSync, mkdirSync, cpSync, rmSync, readdirSync } from "node:fs";
import { pipeline } from "node:stream/promises";
import { Readable } from "node:stream";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const webRoot = dirname(dirname(fileURLToPath(import.meta.url)));
const outArg = process.argv.indexOf("--out");
const outDir = outArg >= 0 ? process.argv[outArg + 1] : join(webRoot, "public", "tiles");
const workDir = join(outDir, ".work");

// Oberoesterreich + Rand (Nachbarregionen als Kontext beim Herauszoomen)
const BBOX = "12.6,47.3,15.2,48.9";
const MAXZOOM = "14";
const FONTS = ["Noto Sans Regular", "Noto Sans Medium", "Noto Sans Italic"];

mkdirSync(outDir, { recursive: true });
mkdirSync(workDir, { recursive: true });

// Windows: explizit das System-bsdtar verwenden - das GNU-tar aus Git Bash
// interpretiert "C:\..." als Remote-Host ("Cannot connect to C")
const TAR = process.platform === "win32" && existsSync("C:\\Windows\\System32\\tar.exe")
  ? "C:\\Windows\\System32\\tar.exe"
  : "tar";

async function download(url, target) {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`${url} -> HTTP ${response.status}`);
  await pipeline(Readable.fromWeb(response.body), createWriteStream(target));
}

/** go-pmtiles-CLI fuer das aktuelle OS besorgen (einmalig in .work gecacht). */
async function ensurePmtilesCli() {
  const binaryName = process.platform === "win32" ? "pmtiles.exe" : "pmtiles";
  const binaryPath = join(workDir, binaryName);
  if (existsSync(binaryPath)) return binaryPath;

  const release = await (await fetch("https://api.github.com/repos/protomaps/go-pmtiles/releases/latest")).json();
  const osName = process.platform === "win32" ? "Windows" : process.platform === "darwin" ? "Darwin" : "Linux";
  const archName = process.arch === "arm64" ? "arm64" : "x86_64";
  const asset = release.assets.find((a) => a.name.includes(osName) && a.name.includes(archName));
  if (!asset) throw new Error(`Kein go-pmtiles-Release fuer ${osName}/${archName} gefunden`);

  const archivePath = join(workDir, asset.name);
  console.log(`Lade ${asset.name} ...`);
  await download(asset.browser_download_url, archivePath);
  // bsdtar (Windows 10+) entpackt auch .zip
  execFileSync(TAR, ["-xf", archivePath, "-C", workDir]);
  if (!existsSync(binaryPath)) throw new Error("pmtiles-Binary nach dem Entpacken nicht gefunden");
  return binaryPath;
}

/** Neuesten verfuegbaren Planet-Build finden (heute, sonst rueckwaerts suchen). */
async function findLatestBuild() {
  for (let daysBack = 0; daysBack < 8; daysBack++) {
    const date = new Date(Date.now() - daysBack * 24 * 60 * 60 * 1000);
    const stamp = date.toISOString().slice(0, 10).replaceAll("-", "");
    const url = `https://build.protomaps.com/${stamp}.pmtiles`;
    const response = await fetch(url, { method: "HEAD" });
    if (response.ok) return url;
  }
  throw new Error("Kein Protomaps-Build in den letzten 8 Tagen gefunden");
}

async function fetchTiles() {
  const target = join(outDir, "oberoesterreich.pmtiles");
  if (existsSync(target)) {
    console.log(`Tiles existieren schon: ${target} (loeschen fuer Neuabruf)`);
    return;
  }
  const cli = await ensurePmtilesCli();
  const buildUrl = await findLatestBuild();
  console.log(`Extrahiere ${BBOX} (maxzoom ${MAXZOOM}) aus ${buildUrl} ...`);
  execFileSync(cli, ["extract", buildUrl, target, `--bbox=${BBOX}`, `--maxzoom=${MAXZOOM}`], {
    stdio: "inherit",
  });
}

async function fetchAssets() {
  const assetsDir = join(outDir, "assets");
  if (existsSync(join(assetsDir, "fonts")) && existsSync(join(assetsDir, "sprites"))) {
    console.log(`Assets existieren schon: ${assetsDir}`);
    return;
  }
  const tarball = join(workDir, "basemaps-assets.tar.gz");
  console.log("Lade basemaps-assets (Fonts + Sprites) ...");
  await download("https://github.com/protomaps/basemaps-assets/archive/refs/heads/main.tar.gz", tarball);
  execFileSync(TAR, ["-xzf", tarball, "-C", workDir]);
  const extracted = readdirSync(workDir).find((name) => name.startsWith("basemaps-assets"));
  if (!extracted) throw new Error("basemaps-assets nach dem Entpacken nicht gefunden");

  mkdirSync(join(assetsDir, "fonts"), { recursive: true });
  for (const font of FONTS) {
    cpSync(join(workDir, extracted, "fonts", font), join(assetsDir, "fonts", font), { recursive: true });
  }
  cpSync(join(workDir, extracted, "sprites"), join(assetsDir, "sprites"), { recursive: true });
}

try {
  await fetchTiles();
  await fetchAssets();
  rmSync(workDir, { recursive: true, force: true });
  console.log(`Fertig: ${outDir}`);
} catch (error) {
  console.error("map-tiles fehlgeschlagen:", error.message);
  process.exit(1);
}
