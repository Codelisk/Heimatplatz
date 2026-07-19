// Zeigt das neue Landhaus-Logo im echten App-Header (nur zur Laufzeit per JS getauscht, keine Dateiaenderung)
import { chromium } from "@playwright/test";

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1440, height: 300 } });
await page.goto("http://localhost:4321/", { waitUntil: "networkidle" });
await page.evaluate(() => {
  document.querySelectorAll('img[src="/logo-mark.svg"]').forEach((img) => {
    img.src = "/logo-alpenhaus.svg";
  });
});
await page.waitForTimeout(400);
await page.locator("header").first().screenshot({ path: "shots/landhaus-header-light.png" });
await page.evaluate(() => document.documentElement.classList.add("dark"));
await page.waitForTimeout(400);
await page.locator("header").first().screenshot({ path: "shots/landhaus-header-dark.png" });
// Footer ebenfalls
await page.locator("footer").first().screenshot({ path: "shots/landhaus-footer-dark.png" });
await page.evaluate(() => document.documentElement.classList.remove("dark"));
await page.waitForTimeout(300);
await page.locator("footer").first().screenshot({ path: "shots/landhaus-footer-light.png" });
await browser.close();
console.log("done");
