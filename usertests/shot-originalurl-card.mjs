import { chromium } from "playwright";
const SESSION = process.env.HP_SESSION;
const PROPERTY_ID = process.env.HP_PROPERTY_ID;
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1440, height: 1100 } });
await page.addInitScript(([s]) => {
  window.localStorage.setItem("heimatplatz:session", s);
  window.localStorage.setItem("heimatplatz:debug-api-url", "http://localhost:5293");
}, [SESSION]);
await page.goto(`http://localhost:4321/immobilien/bearbeiten/?id=${PROPERTY_ID}`, { waitUntil: "networkidle" });
await page.waitForTimeout(1500);
await page.locator("form aside").screenshot({ path: "shots/originalurl-aside-full.png" });
await browser.close();
console.log("done");
