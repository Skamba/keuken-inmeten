import { chromium } from 'playwright';

const BASE = 'http://localhost:32591';

async function waitB(page) {
  await page.waitForFunction(() => !document.querySelector('.loading-progress'), { timeout: 60000 });
  await page.waitForTimeout(500);
}

const b = await chromium.launch();
const page = await b.newPage();
await page.setViewportSize({ width: 1400, height: 900 });
await page.goto(BASE);
await waitB(page);

// Add wand
await page.locator('.nav-link:has-text("Indeling")').click();
await page.waitForTimeout(400);
const wandInput = page.locator("input[placeholder*='Achterwand']");
await wandInput.fill('Keuken');
await wandInput.press('Tab');
await page.click("button:has-text('Wand toevoegen')");
await page.waitForTimeout(400);

// Set wall size
const wandCard = page.locator(".card.border-primary").first();
const wb = wandCard.locator("input[type=number]").nth(0);
await wb.fill('2400'); await wb.press('Tab');
const wh = wandCard.locator("input[type=number]").nth(1);
await wh.fill('2200'); await wh.press('Tab');
await page.waitForTimeout(300);

// Add kast
await wandCard.locator("button:has-text('+ Kast toevoegen')").click();
await page.waitForSelector('.position-fixed', { timeout: 8000 });
await page.waitForTimeout(300);
const modal = page.locator('.position-fixed .card');
await modal.locator("input[placeholder*='Onderkast']").fill('Bovenkast');
await modal.locator('input[type=number]').nth(0).fill('600');
await modal.locator('input[type=number]').nth(0).press('Tab');
await modal.locator('input[type=number]').nth(1).fill('720');
await modal.locator('input[type=number]').nth(1).press('Tab');
await modal.locator("button:has-text('Kast toevoegen')").click();
await page.waitForSelector('.position-fixed', { state: 'hidden', timeout: 8000 }).catch(() => {});
await page.waitForTimeout(400);

// Double-click kast to add shelves
const kastG = page.locator('.wand-opstelling-svg g.wand-kast-sleepbaar').first();
await kastG.waitFor({ timeout: 8000 });
await kastG.dblclick({ force: true });
await page.waitForTimeout(400);
await kastG.dblclick({ force: true });
await page.waitForTimeout(400);

// Screenshot showing plank labels
await page.screenshot({ path: 'screenshots/fix_plank_labels.png', fullPage: false });
console.log('  → fix_plank_labels.png');

// Zoom in by cropping: take screenshot of SVG area
const svgBox = await page.locator('.wand-opstelling-svg').first().boundingBox();
console.log('SVG box:', svgBox);
await page.screenshot({ path: 'screenshots/fix_plank_labels_full.png', fullPage: true });
console.log('  → fix_plank_labels_full.png');

await b.close();
console.log('Done!');
