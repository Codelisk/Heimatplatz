// Rendert die Logo-Vorschau ueber den laufenden Dev-Server (public/ wird statisch ausgeliefert)
import { chromium } from "@playwright/test";

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1400, height: 760 }, deviceScaleFactor: 2 });
await page.goto("file:///C:/Users/Daniel/source/repos/ai/projects/Heimatplatz/usertests/shots/logo-preview.html", { waitUntil: "networkidle" });
await page.screenshot({ path: "shots/logo-preview.png", fullPage: true });
await browser.close();
console.log("done");
