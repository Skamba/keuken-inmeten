import { chromium } from "playwright";
const b = await chromium.launch({ headless: true });
const p = await b.newPage();
const requests = [];
p.on("request", req => requests.push({ url: req.url(), method: req.method() }));
p.on("response", resp => {
  const r = requests.find(r => r.url === resp.url());
  if (r) r.status = resp.status();
});
p.on("pageerror", e => console.log("JS ERROR:", e.message));
await p.goto("http://localhost:43463", { waitUntil: "domcontentloaded" });
await p.waitForTimeout(5000);

// Show all framework requests
const fw = requests.filter(r => r.url.includes("_framework") || r.url.includes("blazor"));
console.log("Framework requests:");
fw.forEach(r => console.log(` ${r.status ?? "?"} ${r.url}`));

// Show failed
const failed = requests.filter(r => r.status >= 400);
console.log("\nFailed:");
failed.forEach(r => console.log(` ${r.status} ${r.url}`));
console.log("\nImportmap:", await p.evaluate(() => document.querySelector('script[type="importmap"]')?.textContent?.substring(0,300)));
await b.close();
