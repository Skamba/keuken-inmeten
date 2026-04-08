import { chromium } from 'playwright';
import fs from 'fs';

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

// ── Wand aanmaken ──
await page.locator('.nav-link:has-text("Indeling")').click();
await page.waitForTimeout(400);
const wandInput = page.locator("input[placeholder*='Achterwand']");
await wandInput.fill('Keuken');
await wandInput.press('Tab');
await page.waitForTimeout(200);
await page.click("button:has-text('Wand toevoegen')");
await page.waitForTimeout(500);

const wandCard = page.locator('.card.border-primary').first();
await wandCard.locator('input[type=number]').nth(0).fill('3600');
await wandCard.locator('input[type=number]').nth(0).press('Tab');
await wandCard.locator('input[type=number]').nth(1).fill('2400');
await wandCard.locator('input[type=number]').nth(1).press('Tab');
await page.waitForTimeout(300);

// ── Twee kasten toevoegen ──
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

// Klein (top) + Groot (bottom) — matches the user's screenshot scenario
await addKast('Klein', 600, 320);
await addKast('Groot', 600, 1920);

// ── Panelen: beide kasten selecteren + deur toevoegen ──
await page.locator('.nav-link:has-text("Panelen")').click();
await page.waitForTimeout(1200);

// Selecteer eerste kast
const kastenEls = page.locator('.wand-opstelling-svg g.paneel-kast-selecteerbaar');
await kastenEls.first().waitFor({ timeout: 15_000 });
await kastenEls.first().click({ force: true });
await page.waitForTimeout(400);
// Ctrl+click tweede kast
await kastenEls.nth(1).click({ force: true, modifiers: ['Control'] });
await page.waitForTimeout(400);

await page.locator("button:has-text('+ Paneel toevoegen')").click();
await page.waitForTimeout(600);

// ── Resultaat ──
await page.locator('.nav-link:has-text("Resultaat")').click();
await page.waitForTimeout(1000);

const toggleBtn = page.locator('.onderbouwing-toggle').first();
await toggleBtn.click();
await page.waitForTimeout(800);

const svgEl = page.locator('.scharnier-overzicht-svg').first();
await svgEl.scrollIntoViewIfNeeded();
await page.waitForTimeout(400);

// ── Full SVG screenshot ──
await svgEl.screenshot({ path: `${OUT}/crops_full.png` });
console.log('📸 crops_full.png');

// Get bounding box of svg in page coordinates
const bbox = await svgEl.boundingBox();

// ── Lees de Y-posities van alle boorgaten via tekst op de labels ──
const labelTexts = await page.locator('.scharnier-overzicht-svg text').allTextContents();
console.log('SVG texts:', labelTexts.filter(t => t.trim()).join(' | '));

// ── Crop: alleen de linker kast-labels kolom ──
await page.screenshot({
  path: `${OUT}/crops_links.png`,
  clip: {
    x: bbox.x,
    y: bbox.y,
    width: Math.round(bbox.width * 0.38), // linkerkant: kast-labels zone (~0..184 van 558)
    height: bbox.height
  }
});
console.log('📸 crops_links.png');

// ── Crop: alleen de rechter paneel-labels kolom ──
// LabelX-18 (cirkel) t/m SvgWidth → rechterkant, ca 75% van de breedte t/m einde
await page.screenshot({
  path: `${OUT}/crops_rechts.png`,
  clip: {
    x: bbox.x + Math.round(bbox.width * 0.78),
    y: bbox.y,
    width: Math.round(bbox.width * 0.22),
    height: bbox.height
  }
});
console.log('📸 crops_rechts.png');

// ── Individuele scharnier-rijen ──
// Bepaal Y-posities van de boorgat-cirkels (⌀35 labels)
const cupLabels = page.locator('.scharnier-overzicht-svg text').filter({ hasText: '⌀35' });
const count = await cupLabels.count();
console.log(`Aantal scharnieren: ${count}`);

for (let i = 0; i < count; i++) {
  const labelBbox = await cupLabels.nth(i).boundingBox();
  if (!labelBbox) continue;

  const rowY = labelBbox.y - 16;
  const rowH = 40;

  await page.screenshot({
    path: `${OUT}/crops_rij_${i + 1}.png`,
    clip: {
      x: bbox.x,
      y: rowY,
      width: bbox.width,
      height: rowH
    }
  });
  console.log(`📸 crops_rij_${i + 1}.png (scherm-y=${Math.round(labelBbox.y)})`);
}

await browser.close();
