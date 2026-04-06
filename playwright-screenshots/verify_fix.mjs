import { chromium } from 'playwright';

const BASE = 'http://localhost:32591';

async function waitB(page) {
  await page.waitForFunction(() => !document.querySelector('.loading-progress'), { timeout: 60000 });
  await page.waitForTimeout(500);
}

async function shot(page, name) {
  await page.screenshot({ path: `screenshots/fix_${name}.png`, fullPage: false });
  console.log(`  → fix_${name}.png`);
}

const b = await chromium.launch();
const page = await b.newPage();
await page.setViewportSize({ width: 1400, height: 900 });
await page.goto(BASE);
await waitB(page);

// Add a wand + kast
await page.locator('.nav-link:has-text("Indeling")').click();
await page.waitForTimeout(400);

const wandInput = page.locator("input[placeholder*='Achterwand']");
await wandInput.fill('Muur');
await wandInput.press('Tab');
await page.click("button:has-text('Wand toevoegen')");
await page.waitForTimeout(500);

// Add a kast via the "+" button
const wandCard = page.locator(".card.border-primary").first();
await wandCard.locator("button:has-text('+ Kast toevoegen')").click();
await page.waitForSelector('.position-fixed', { timeout: 8000 });
await page.waitForTimeout(300);
const modal = page.locator('.position-fixed .card');
await modal.locator("input[placeholder*='Onderkast']").fill('Spoelbak');
await modal.locator('input[type=number]').nth(0).fill('600');
await modal.locator('input[type=number]').nth(0).press('Tab');
await modal.locator('input[type=number]').nth(1).fill('870');
await modal.locator('input[type=number]').nth(1).press('Tab');
await modal.locator("button:has-text('Kast toevoegen')").click();
await page.waitForSelector('.position-fixed', { state: 'hidden', timeout: 8000 }).catch(() => {});
await page.waitForTimeout(400);

// Add a second kast
await wandCard.locator("button:has-text('+ Kast toevoegen')").click();
await page.waitForSelector('.position-fixed', { timeout: 8000 });
await page.waitForTimeout(300);
await modal.locator("input[placeholder*='Onderkast']").fill('Onderkast midden');
await modal.locator('input[type=number]').nth(0).fill('600');
await modal.locator('input[type=number]').nth(0).press('Tab');
await modal.locator('input[type=number]').nth(1).fill('870');
await modal.locator('input[type=number]').nth(1).press('Tab');
await modal.locator("button:has-text('Kast toevoegen')").click();
await page.waitForSelector('.position-fixed', { state: 'hidden', timeout: 8000 }).catch(() => {});
await page.waitForTimeout(400);

// Navigate to panelen
await page.locator('.nav-link:has-text("2.")').click();
await page.waitForTimeout(600);

// Click kast in SVG
const kastG = page.locator('.wand-opstelling-svg g.wand-kast-sleepbaar').first();
await kastG.waitFor({ timeout: 8000 });
await kastG.click({ force: true });
await page.waitForTimeout(400);
await shot(page, 'panelen_kast_selected');

// Add panel
await page.locator("button:has-text('+ Paneel toevoegen')").click();
await page.waitForTimeout(500);
await shot(page, 'panelen_after_add');
await page.screenshot({ path: 'screenshots/fix_panelen_after_add_full.png', fullPage: true });
console.log('  → fix_panelen_after_add_full.png');

// Add a second panel
await page.locator('.wand-opstelling-svg g.wand-kast-sleepbaar').nth(1).click({ force: true });
await page.waitForTimeout(400);
await page.locator('button:has-text("Ladefront")').click();
await page.waitForTimeout(200);
await page.locator("button:has-text('+ Paneel toevoegen')").click();
await page.waitForTimeout(400);
await shot(page, 'panelen_two_panels');

await b.close();
console.log('Done!');


await b.close();
console.log('Done!');
