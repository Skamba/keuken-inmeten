import { chromium } from 'playwright';

const BASE = process.env.BASE_URL ?? 'http://localhost:5296';
const OUT = '../screenshots';

async function waitB(page) {
  await page.waitForFunction(() => !document.querySelector('.loading-progress'), { timeout: 60_000 });
  await page.waitForTimeout(600);
}

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
await page.setViewportSize({ width: 1400, height: 900 });
await page.goto(BASE);
await waitB(page);

// ── 1. Maak wand aan ──
await page.locator('.nav-link:has-text("Indeling")').click();
await page.waitForTimeout(400);
const wandInput = page.locator("input[placeholder*='Achterwand']");
await wandInput.fill('Keuken');
await wandInput.press('Tab');
await page.waitForTimeout(200);
await page.click("button:has-text('Wand toevoegen')");
await page.waitForTimeout(500);

// Wandafmetingen invullen op de kaart die nu zichtbaar is
const wandCard = page.locator('.card.border-primary').first();
await wandCard.locator('input[type=number]').nth(0).fill('3600');
await wandCard.locator('input[type=number]').nth(0).press('Tab');
await wandCard.locator('input[type=number]').nth(1).fill('2400');
await wandCard.locator('input[type=number]').nth(1).press('Tab');
await page.waitForTimeout(300);

// ── 2. Voeg twee hoge kasten toe ──
const addKast = async (naam, breedte, hoogte) => {
  await wandCard.locator("button:has-text('+ Kast toevoegen')").click();
  await page.waitForSelector('.position-fixed', { timeout: 8_000 });
  await page.waitForTimeout(300);
  const modal = page.locator('.position-fixed .card');
  await modal.locator("input[placeholder*='Onderkast']").fill(naam);
  await modal.locator('input[type=number]').nth(0).fill(String(breedte));
  await modal.locator('input[type=number]').nth(0).press('Tab');
  await modal.locator('input[type=number]').nth(1).fill(String(hoogte));
  await modal.locator('input[type=number]').nth(1).press('Tab');
  await modal.locator("button:has-text('Kast toevoegen')").click();
  await page.waitForSelector('.position-fixed', { state: 'hidden', timeout: 8_000 }).catch(() => {});
  await page.waitForTimeout(400);
};

await addKast('Keuken', 600, 1920);

// ── 3. Ga naar panelen – selecteer kast en voeg deur toe ──
await page.locator('.nav-link:has-text("Panelen")').click();
await page.waitForTimeout(1200);

// Klik op de kast in het SVG (PaneelPlaatsEditor gebruikt .paneel-kast-selecteerbaar)
const kastEl = page.locator('.wand-opstelling-svg g.paneel-kast-selecteerbaar').first();
await kastEl.waitFor({ timeout: 15_000 });
await kastEl.click({ force: true });
await page.waitForTimeout(600);
await page.locator("button:has-text('+ Paneel toevoegen')").click();
await page.waitForTimeout(500);

// ── 4. Resultaat – klap onderbouwing open voor het eerste paneel ──
await page.locator('.nav-link:has-text("Resultaat")').click();
await page.waitForTimeout(1000);

// Klik op "Onderbouwing per scharnier" toggle
const toggleBtn = page.locator('.onderbouwing-toggle').first();
await toggleBtn.click();
await page.waitForTimeout(800);

// Scroll het scharnieroverzicht in beeld
const overzichtEl = page.locator('.scharnier-overzicht-wrap').first();
await overzichtEl.scrollIntoViewIfNeeded();
await page.waitForTimeout(400);

// Screenshot van de volledige onderbouwing-inhoud
const onderbouwingEl = page.locator('.onderbouwing-inhoud').first();
await onderbouwingEl.scrollIntoViewIfNeeded();
await page.waitForTimeout(400);
await onderbouwingEl.screenshot({ path: `${OUT}/scharnier_overzicht_check.png` });
console.log('📸 screenshot gesaved: screenshots/scharnier_overzicht_check.png');

// Ook fullpage voor context
await page.screenshot({ path: `${OUT}/scharnier_overzicht_fullpage.png`, fullPage: true });
console.log('📸 screenshot gesaved: screenshots/scharnier_overzicht_fullpage.png');

await browser.close();
