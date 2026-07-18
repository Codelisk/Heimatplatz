// Wegwerf-Verify: Originalinserat-Feld im WYSIWYG-Editor (/inserieren + /bearbeiten)
// Laeuft gegen den lokalen Astro-Dev-Server (4321) + lokale Wegwerf-API (5293).
import { chromium } from "playwright";

const SESSION = process.env.HP_SESSION;
const PROPERTY_ID = process.env.HP_PROPERTY_ID;
const EXPECTED_URL = "https://www.example-portal.at/inserat/ui-verify-777";

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });

// Session + Debug-API-Override VOR dem ersten Skriptlauf der Seite setzen
await page.addInitScript(([session]) => {
  window.localStorage.setItem("heimatplatz:session", session);
  window.localStorage.setItem("heimatplatz:debug-api-url", "http://localhost:5293");
}, [SESSION]);

// 1) /inserieren/: Feld vorhanden und leer
await page.goto("http://localhost:4321/inserieren/", { waitUntil: "networkidle" });
const createField = page.locator("#originalListingUrl");
const createVisible = await createField.isVisible();
const createValue = await createField.inputValue();
console.log("CREATE field visible:", createVisible, "| value:", JSON.stringify(createValue));
await page.locator("aside").first().screenshot({ path: "shots/originalurl-create-aside.png" });

// 2) /immobilien/bearbeiten/?id=...: Prefill aus den Kontakten
await page.goto(`http://localhost:4321/immobilien/bearbeiten/?id=${PROPERTY_ID}`, { waitUntil: "networkidle" });
const editField = page.locator("#originalListingUrl");
await page.waitForFunction(
  (el) => (document.querySelector("#originalListingUrl")?.value ?? "") !== "",
  null,
  { timeout: 15000 },
).catch(() => {});
const editValue = await editField.inputValue();
const titleValue = await page.locator("#title").inputValue();
console.log("EDIT prefill title:", JSON.stringify(titleValue));
console.log("EDIT prefill url:", JSON.stringify(editValue), "| match:", editValue === EXPECTED_URL);
await page.locator("aside").first().screenshot({ path: "shots/originalurl-edit-aside.png" });

await browser.close();
if (!createVisible || createValue !== "" || editValue !== EXPECTED_URL) {
  console.error("VERIFY FAILED");
  process.exit(1);
}
console.log("VERIFY OK");
