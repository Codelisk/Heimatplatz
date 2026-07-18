// Temporaeres Hilfsskript: App-Screenshots (Light/Dark) fuer Logo-Redesign
import { chromium } from "@playwright/test";

const out = process.argv[2] || "shots";
const url = process.argv[3] || "http://localhost:4321/";

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
await page.goto(url, { waitUntil: "networkidle" });
await page.screenshot({ path: `${out}-light.png` });

// Dark Mode: Theme wird vermutlich per .dark-Klasse auf <html> gesteuert
await page.evaluate(() => {
  document.documentElement.classList.add("dark");
  try { localStorage.setItem("theme", "dark"); } catch {}
});
await page.waitForTimeout(400);
await page.screenshot({ path: `${out}-dark.png` });

// Header-Nahaufnahme mit Logo, beide Themes
const header = page.locator("header").first();
if (await header.count()) {
  await header.screenshot({ path: `${out}-header-dark.png` });
  await page.evaluate(() => document.documentElement.classList.remove("dark"));
  await page.waitForTimeout(300);
  await page.locator("header").first().screenshot({ path: `${out}-header-light.png` });
}

await browser.close();
console.log("done");
