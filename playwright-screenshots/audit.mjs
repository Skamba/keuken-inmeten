import { chromium } from "playwright";
import fs from "fs";

const BASE = process.env.BASE_URL ?? "http://localhost:43463";
const OUT = process.env.SCREENSHOT_DIR ?? "./screenshots";
fs.mkdirSync(OUT, { recursive: true });
fs.readdirSync(OUT).filter(f => f.endsWith(".png")).forEach(f => fs.unlinkSync(`${OUT}/${f}`));

let n = 0;
async function shot(page, name) {
  n++;
  const p = `${OUT}/${String(n).padStart(3, "0")}_${name}.png`;
  await page.screenshot({ path: p, fullPage: false });
  console.log(`📸 ${n} ${name}`);
}
async function shotFull(page, name) {
  n++;
  const p = `${OUT}/${String(n).padStart(3, "0")}_${name}.png`;
  await page.screenshot({ path: p, fullPage: true });
  console.log(`📸 ${n} ${name} [full]`);
}

async function waitB(page, ms = 600) {
  // Wait for Blazor WASM to boot — the .loading-progress element is removed when done
  await page.waitForFunction(() => !document.querySelector(".loading-progress"), { timeout: 60000 });
  await page.waitForTimeout(ms);
}

// Helper: navigate via Blazor's client-side router (preserves state)
async function navTo(page, linkText, waitMs = 600) {
  await page.locator(`.nav-link:has-text("${linkText}")`).click();
  await page.waitForTimeout(waitMs);
}

// ─────────────────────────────────────────────
const browser = await chromium.launch({ headless: true });

// ── Desktop viewport ──
const ctx = await browser.newContext({ viewport: { width: 1400, height: 900 } });
const page = await ctx.newPage();

// == Load the app once ==
await page.goto(BASE, { waitUntil: "domcontentloaded" });
await waitB(page, 1200);
await shot(page, "01_home_leeg");
await shotFull(page, "02_home_leeg_full");

// == Navigate to KASTEN ==
await navTo(page, "Indeling");
await page.waitForTimeout(400);
await shot(page, "03_kasten_leeg");

// == KASTEN – wand toevoegen ==
const wandInput = page.locator("input[placeholder*='Achterwand']");
await wandInput.fill("Achterwand");
await wandInput.press("Tab");  // trigger Blazor @bind change event
await page.waitForTimeout(300);
await shot(page, "04_kasten_wand_naam_ingevuld");
await page.click("button:has-text('Wand toevoegen')");
await page.waitForTimeout(500);
await shot(page, "05_kasten_1_wand");

// Tweede wand
await wandInput.fill("Zijwand links");
await wandInput.press("Tab");
await page.waitForTimeout(300);
await page.click("button:has-text('Wand toevoegen')");
await page.waitForTimeout(400);
await shot(page, "06_kasten_2_wanden");

// Wandafmetingen invullen voor eerste wand
const eersteWandCard = page.locator(".card.border-primary").first();
const wandBreedte = eersteWandCard.locator("input[type=number]").nth(0);
const wandHoogte = eersteWandCard.locator("input[type=number]").nth(1);
await wandBreedte.fill("3600");
await wandBreedte.press("Tab");
await wandHoogte.fill("2400");
await wandHoogte.press("Tab");
await page.waitForTimeout(300);
await shot(page, "07_kasten_wand_afmetingen");

// == KASTEN – kast toevoegen formulier ==
await eersteWandCard.locator("button:has-text('+ Kast toevoegen')").click();
await page.waitForSelector(".position-fixed", { timeout: 8000 });
await page.waitForTimeout(500);
await shot(page, "08_kasten_formulier_leeg");

// Formulier invullen
const modal = page.locator(".position-fixed .card");
await modal.locator("input[placeholder*='Onderkast']").fill("Spoelbak onderkast");
await modal.locator("input[type=number]").nth(0).fill("800");
await modal.locator("input[type=number]").nth(0).press("Tab");
await modal.locator("input[type=number]").nth(1).fill("870");
await modal.locator("input[type=number]").nth(1).press("Tab");
await page.waitForTimeout(500);
await shot(page, "09_kasten_formulier_gevuld");
await shotFull(page, "10_kasten_formulier_gevuld_full");

// Opslaan
await modal.locator("button:has-text('Kast toevoegen')").click();
await page.waitForSelector(".position-fixed", { state: "hidden", timeout: 8000 }).catch(() => {});
await page.waitForTimeout(500);
await shot(page, "11_kasten_eerste_kast");

// Helper: kast toevoegen
const voegKastToe = async (naam, breedte, hoogte = 870, wandIndex = 0) => {
  const wc = page.locator(".card.border-primary").nth(wandIndex);
  await wc.locator("button:has-text('+ Kast toevoegen')").click();
  await page.waitForSelector(".position-fixed", { timeout: 8000 });
  await page.waitForTimeout(300);
  const m = page.locator(".position-fixed .card");
  await m.locator("input[placeholder*='Onderkast']").fill(naam);
  await m.locator("input[type=number]").nth(0).fill(String(breedte));
  await m.locator("input[type=number]").nth(0).press("Tab");
  if (hoogte) {
    await m.locator("input[type=number]").nth(1).fill(String(hoogte));
    await m.locator("input[type=number]").nth(1).press("Tab");
  }
  await page.waitForTimeout(300);
  await m.locator("button:has-text('Kast toevoegen')").click();
  await page.waitForSelector(".position-fixed", { state: "hidden", timeout: 8000 }).catch(() => {});
  await page.waitForTimeout(300);
};

await voegKastToe("Onderkast midden", 600);
await voegKastToe("Onderkast hoek", 900);
await shot(page, "12_kasten_3_kasten");

await voegKastToe("Bovenkast links", 600, 720);
await voegKastToe("Bovenkast rechts", 450, 720);
await shot(page, "13_kasten_5_kasten");

await voegKastToe("Hoge kast", 600, 2100);
await shot(page, "14_kasten_6_kasten_met_hoge");
await shotFull(page, "15_kasten_6_kasten_full");

// Wandopstelling SVG
await page.evaluate(() => window.scrollBy(0, 400));
await page.waitForTimeout(400);
await shot(page, "16_kasten_wand_diagram_svg");

// == Edit kast ==
await page.evaluate(() => window.scrollTo(0, 0));
await page.waitForTimeout(300);
const editBtn = eersteWandCard.locator("button[title='Bewerken']").first();
await editBtn.click();
await page.waitForSelector(".position-fixed", { timeout: 8000 });
await page.waitForTimeout(400);
await shot(page, "17_kasten_bewerken_formulier");
// Annuleren
await page.locator(".position-fixed .card .btn-close").click();
await page.waitForSelector(".position-fixed", { state: "hidden", timeout: 5000 }).catch(() => {});
await page.waitForTimeout(300);

// == Copy kast ==
const copyBtn = eersteWandCard.locator("button[title='Kopiëren']").first();
await copyBtn.click();
await page.waitForTimeout(500);
await shot(page, "18_kasten_kast_gekopieerd");

// == Delete kast confirm ==
const deleteKastBtn = eersteWandCard.locator("button[title='Verwijderen']").last();
await deleteKastBtn.click();
await page.waitForTimeout(300);
await shot(page, "19_kasten_delete_bevestiging");
// Cancel
await eersteWandCard.locator("button:has-text('✗')").click();
await page.waitForTimeout(300);

// == Wand hernoemen ==
await eersteWandCard.locator("button:has-text('Hernoemen')").click();
await page.waitForTimeout(300);
await shot(page, "20_kasten_wand_hernoemen");
await eersteWandCard.locator("button:has-text('Annuleren')").click();
await page.waitForTimeout(300);

// == Wand delete bevestiging ==
await eersteWandCard.locator("button:has-text('Verwijderen')").click();
await page.waitForTimeout(300);
await shot(page, "21_kasten_wand_delete_bevestiging");
await eersteWandCard.locator("button:has-text('Nee')").click();
await page.waitForTimeout(300);
await shotFull(page, "22_kasten_volledig_full");

// ════════════════════════════════════════════════
// == PANELEN – leeg (geen kasten geselecteerd) ==
await navTo(page, "Panelen");
await page.waitForTimeout(600);
await shot(page, "23_panelen_leeg");
await shotFull(page, "24_panelen_leeg_full");

// Wandoverzicht sectie
await page.evaluate(() => window.scrollTo(0, 0));
await page.waitForTimeout(300);
await shot(page, "25_panelen_wandoverzicht");

// Helper: click a kast in the first WandOpstelling SVG
const clickKastInSvg = async (idx = 0) => {
  const kastG = page.locator(".wand-opstelling-svg g.wand-kast-sleepbaar").nth(idx);
  await kastG.waitFor({ timeout: 10000 });
  await kastG.click({ force: true });  // force: bypass overlap check in SVG
  await page.waitForTimeout(500);
};

// Select first kast
await clickKastInSvg(0);
await shot(page, "26_panelen_1_kast_geselecteerd");

// Select second kast (toggle 2nd)
await clickKastInSvg(1);
await page.waitForTimeout(400);
await shot(page, "27_panelen_2_kasten_geselecteerd");

// Deselect all
const deselectBtn = page.locator("button:has-text('Alles deselecteren')");
if (await deselectBtn.count() > 0) {
  await deselectBtn.first().click();
  await page.waitForTimeout(300);
  await shot(page, "28_panelen_alles_gedeselecteerd");
}

// Select first kast again for panel configuration
await clickKastInSvg(0);

// Type selector buttons (inside the sticky card-body)
const stickyCard = page.locator(".sticky-top");
const deurBtn = stickyCard.locator("button:has-text('Deur')");
const ladeBtn = stickyCard.locator("button:has-text('Ladefront')");
const blindBtn = stickyCard.locator("button:has-text('Blind paneel')");

if (await deurBtn.count() > 0) {
  await deurBtn.click();
  await page.waitForTimeout(300);
  await shot(page, "29_panelen_type_deur");
}
if (await ladeBtn.count() > 0) {
  await ladeBtn.click();
  await page.waitForTimeout(300);
  await shot(page, "30_panelen_type_ladefront");
}
if (await blindBtn.count() > 0) {
  await blindBtn.click();
  await page.waitForTimeout(300);
  await shot(page, "31_panelen_type_blind");
}
// Switch back to Deur for scharnier
if (await deurBtn.count() > 0) { await deurBtn.click(); await page.waitForTimeout(200); }

// Scharnier buttons
const linksBtn = stickyCard.locator("button:has-text('Links')");
const rechtsBtn = stickyCard.locator("button:has-text('Rechts')");
if (await linksBtn.count() > 0) {
  await linksBtn.click();
  await page.waitForTimeout(200);
  await shot(page, "32_panelen_scharnier_links");
}
if (await rechtsBtn.count() > 0) {
  await rechtsBtn.click();
  await page.waitForTimeout(200);
  await shot(page, "33_panelen_scharnier_rechts");
}

// Add panel (should be enabled now — check dims auto-fill)
const paneelToevBtn = stickyCard.locator("button:has-text('+ Paneel toevoegen')");
if (await paneelToevBtn.count() > 0) {
  await paneelToevBtn.click();
  await page.waitForTimeout(500);
  await shot(page, "34_panelen_paneel_toegevoegd");
}

// Select 2 kasten
await clickKastInSvg(0);
await page.waitForTimeout(300);
await clickKastInSvg(2);
await page.waitForTimeout(300);
await shot(page, "35_panelen_2_geselecteerd");

if (await deurBtn.count() > 0) { await deurBtn.click(); await page.waitForTimeout(200); }
if (await paneelToevBtn.count() > 0) {
  await paneelToevBtn.click();
  await page.waitForTimeout(400);
}

// Add ladefront
await clickKastInSvg(3);
await page.waitForTimeout(300);
if (await ladeBtn.count() > 0) { await ladeBtn.click(); await page.waitForTimeout(200); }
if (await paneelToevBtn.count() > 0) {
  await paneelToevBtn.click();
  await page.waitForTimeout(400);
}

// Add blind
await clickKastInSvg(4);
await page.waitForTimeout(300);
if (await blindBtn.count() > 0) { await blindBtn.click(); await page.waitForTimeout(200); }
if (await paneelToevBtn.count() > 0) {
  await paneelToevBtn.click();
  await page.waitForTimeout(400);
}

await shotFull(page, "36_panelen_lijst_met_meerdere");
await shot(page, "37_panelen_viewport");

// Sticky form (right side)
await page.evaluate(() => window.scrollTo(999999, 0));
await page.waitForTimeout(300);
await shot(page, "38_panelen_sticky_form");

// Delete a panel (direct × button, no confirm)
const deletePaneelBtn = page.locator(".list-group-item button.btn-outline-danger").first();
if (await deletePaneelBtn.count() > 0) {
  await deletePaneelBtn.click();
  await page.waitForTimeout(400);
  await shot(page, "39_panelen_na_verwijderen");
}

await shotFull(page, "40_panelen_eind_full");

// ════════════════════════════════════════════════
// == RESULTAAT ==
await navTo(page, "Resultaat");
await page.waitForTimeout(600);
await shot(page, "42_resultaat_stats_bar");
await shotFull(page, "43_resultaat_full");

// Table hover
const tableRows = page.locator("table tbody tr");
const rowCount = await tableRows.count();
if (rowCount > 0) {
  await tableRows.first().hover();
  await page.waitForTimeout(200);
  await shot(page, "44_resultaat_tabel_hover");
}

// Detail cards
await page.evaluate(() => window.scrollBy(0, 400));
await page.waitForTimeout(400);
await shot(page, "45_resultaat_detail_kaarten");
await shotFull(page, "46_resultaat_detail_full");

// ════════════════════════════════════════════════
// == MOBIEL (390px) ==
await ctx.close();
const mCtx = await browser.newContext({ viewport: { width: 390, height: 844 } });
const mPage = await mCtx.newPage();

await mPage.goto(BASE, { waitUntil: "domcontentloaded" });
await waitB(mPage, 1200);
await shot(mPage, "47_mobile_home");
await shotFull(mPage, "48_mobile_home_full");

await mPage.locator("button.navbar-toggler").click();
await mPage.waitForTimeout(300);
await mPage.locator(".nav-link:has-text('Indeling')").click();
await mPage.waitForTimeout(600);
await shot(mPage, "49_mobile_kasten");

await mPage.locator("button.navbar-toggler").click();
await mPage.waitForTimeout(300);
await mPage.locator(".nav-link:has-text('Panelen')").click();
await mPage.waitForTimeout(600);
await shot(mPage, "50_mobile_panelen");
await shotFull(mPage, "51_mobile_panelen_full");

await mPage.locator("button.navbar-toggler").click();
await mPage.waitForTimeout(300);
await mPage.locator(".nav-link:has-text('Resultaat')").click();
await mPage.waitForTimeout(600);
await shot(mPage, "52_mobile_resultaat");
await shotFull(mPage, "53_mobile_resultaat_full");

// == TABLET (768px) ==
await mCtx.close();
const tCtx = await browser.newContext({ viewport: { width: 768, height: 1024 } });
const tPage = await tCtx.newPage();

// Helper: toggle nav if on small viewport, then click
const mobileNav = async (p, txt) => {
  const toggler = p.locator("button.navbar-toggler");
  if (await toggler.isVisible()) {
    await toggler.click();
    await p.waitForTimeout(300);
  }
  await p.locator(`.nav-link:has-text("${txt}")`).click();
  await p.waitForTimeout(600);
};

await tPage.goto(BASE, { waitUntil: "domcontentloaded" });
await waitB(tPage, 1200);
await shot(tPage, "54_tablet_home");

await mobileNav(tPage, "Indeling");
await shot(tPage, "55_tablet_kasten");

await mobileNav(tPage, "Panelen");
await shot(tPage, "56_tablet_panelen");
await shotFull(tPage, "57_tablet_panelen_full");

await mobileNav(tPage, "Resultaat");
await shot(tPage, "58_tablet_resultaat");
await shotFull(tPage, "59_tablet_resultaat_full");

await tCtx.close();
await browser.close();
console.log(`\n✅ ${n} screenshots opgeslagen in ${OUT}`);
