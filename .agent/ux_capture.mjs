import { chromium } from '@playwright/test';
import fs from 'node:fs/promises';
import path from 'node:path';

const baseURL = process.env.BASE_URL ?? 'http://127.0.0.1:4173';
const iterationLabel = process.argv[2] ?? 'iteration-1';
const phaseLabel = process.argv[3] ?? 'before';
const outputDir = path.resolve('.agent', 'screenshots', iterationLabel, phaseLabel);
const desktopViewport = { width: 1440, height: 1200 };
const mobileViewport = { width: 390, height: 844 };
const screenshotCategories = ['journey', 'page', 'section', 'element'];
const captured = [];

await fs.mkdir(outputDir, { recursive: true });

for (const category of screenshotCategories) {
  await fs.mkdir(path.join(outputDir, category), { recursive: true });
}

const browser = await chromium.launch({ headless: true });

try {
  await captureDesktopScenarios();
  await captureMobileSpotChecks();
  await writeManifest();

  const counts = summarizeCounts();
  console.log(JSON.stringify({ outputDir, counts, captured }, null, 2));
} finally {
  await browser.close();
}

async function captureDesktopScenarios() {
  await captureEmptyAndErrorDesktop();
  await captureNormalDesktop();
  await captureDenseDesktop();
}

async function captureEmptyAndErrorDesktop() {
  await withFreshPage(desktopViewport, async (page) => {
    await gotoAndWait(page, '/', 'Van keukenmaat naar paneel, boorgat en zaagplan');
    await capturePage(page, 'journey-home-empty-desktop', 'journey');
    await capturePage(page, 'home-empty-desktop');
    await captureLocator(page, page.locator('.home-startflow > section').first(), 'home-hero-empty-desktop', 'section');
    await captureLocator(page, page.locator('.home-summary-card').first(), 'home-needs-card-empty-desktop', 'section');
    await captureLocator(page, page.locator('.home-startflow .btn.btn-primary').first(), 'home-primary-cta-empty-desktop', 'element');
    await captureLocator(page, page.locator('.home-startflow .btn.btn-outline-secondary').first(), 'home-secondary-cta-empty-desktop', 'element');
    await captureLocator(page, page.locator('details.terminologie-card summary').first(), 'home-glossary-toggle-empty-desktop', 'element');

    await gotoAndWait(page, '/kasten', 'Stap 1: Indeling');
    await capturePage(page, 'journey-indeling-empty-desktop', 'journey');
    await capturePage(page, 'indeling-empty-desktop');
    await captureLocator(page, page.locator('.card.mb-4').first(), 'indeling-add-wall-card-empty-desktop', 'section');
    await captureLocator(page, page.getByTestId('nieuwe-wand-naam-input'), 'indeling-new-wall-input-empty-desktop', 'element');
    await captureLocator(page, page.getByTestId('wand-toevoegen-button'), 'indeling-new-wall-button-empty-desktop', 'element');

    await addWall(page, 'Achterwand');
    await openIndelingWorkspace(page, 'Achterwand');
    await click(page.getByTestId('open-kast-form-button'));
    await page.getByTestId('kast-form').waitFor();
    await capturePage(page, 'indeling-kast-form-validation-desktop');
    await captureLocator(page, page.getByTestId('kast-form'), 'indeling-kast-form-dialog-validation-desktop', 'section');
    await captureLocator(page, page.getByTestId('kast-form-progress'), 'indeling-kast-form-progress-validation-desktop', 'element');
    await captureLocator(page, page.getByTestId('kast-form-volgende-button'), 'indeling-kast-form-next-validation-desktop', 'element');
    await click(page.getByRole('button', { name: 'Annuleren' }).last());

    await click(page.getByTestId('open-apparaat-form-button'));
    await page.getByTestId('apparaat-form').waitFor();
    await capturePage(page, 'indeling-apparaat-form-desktop');
    await captureLocator(page, page.getByTestId('apparaat-form'), 'indeling-apparaat-form-dialog-desktop', 'section');
    await captureLocator(page, page.getByTestId('apparaat-form-progress'), 'indeling-apparaat-form-progress-desktop', 'element');
    await click(page.locator('[data-testid="apparaat-form"] .btn-close').first());

    await page.goto(`${baseURL}/panelen`, { waitUntil: 'networkidle' });
    await page.getByRole('alert').waitFor();
    await page.getByText('Stap 2: Panelen is nog niet beschikbaar').waitFor();
    await page.waitForTimeout(250);
    await capturePage(page, 'panelen-route-gate-desktop');
    await captureLocator(page, page.getByRole('alert').first(), 'panelen-route-gate-alert-desktop', 'section');

    await gotoAndWait(page, '/?share=invalid', 'Van keukenmaat naar paneel, boorgat en zaagplan');
    await capturePage(page, 'home-invalid-share-desktop');
    await captureLocator(page, page.getByRole('alert').first(), 'home-invalid-share-alert-desktop', 'section');
  });
}

async function captureNormalDesktop() {
  await withFreshPage(desktopViewport, async (page) => {
    await createNormalProject(page);

    await gotoAndWait(page, '/', 'Ga verder met uw keukenproject');
    await capturePage(page, 'journey-home-resume-desktop', 'journey');
    await capturePage(page, 'home-resume-desktop');
    await captureLocator(page, page.locator('.home-startflow > section').first(), 'home-hero-resume-desktop', 'section');
    await captureLocator(page, page.getByTestId('home-project-dashboard'), 'home-project-dashboard-desktop', 'section');
    await captureLocator(page, page.locator('.home-resume-links').first(), 'home-direct-links-desktop', 'section');
    await captureLocator(page, page.locator('.home-startflow .btn.btn-primary').first(), 'home-primary-cta-resume-desktop', 'element');

    await gotoAndWait(page, '/kasten', 'Stap 1: Indeling');
    await openIndelingWorkspace(page, 'Achterwand');
    await capturePage(page, 'journey-indeling-normal-desktop', 'journey');
    await capturePage(page, 'indeling-normal-desktop');
    await captureLocator(page, page.getByTestId('actieve-wand-werkruimte'), 'indeling-active-workspace-normal-desktop', 'section');
    await captureLocator(page, page.locator('.indeling-object-lijst').first(), 'indeling-object-list-normal-desktop', 'section');
    await captureLocator(page, page.getByTestId('open-kast-form-button'), 'indeling-add-cabinet-button-normal-desktop', 'element');
    await captureLocator(page, page.getByTestId('wand-breedte-input'), 'indeling-wall-width-input-normal-desktop', 'element');

    await goToPanelen(page);
    await openPanelWand(page, 'Achterwand');
    await ensurePanelEditorOpen(page, 'Achterwand');
    await capturePage(page, 'journey-panelen-editor-desktop', 'journey');
    await capturePage(page, 'panelen-editor-normal-desktop');
    await captureLocator(page, page.getByTestId('paneel-weergave-tabs'), 'panelen-tabs-normal-desktop', 'section');
    await captureLocator(page, page.getByTestId('paneel-actieve-wand-werkruimte'), 'panelen-workspace-normal-desktop', 'section');
    await captureLocator(page, page.getByTestId('paneel-editor-drawer'), 'panelen-editor-drawer-normal-desktop', 'section');
    await captureLocator(page, page.locator('[data-testid="paneel-plaats-editor"]').first(), 'panelen-canvas-normal-desktop', 'section');
    await captureLocator(page, page.getByTestId('close-paneel-editor-button'), 'panelen-editor-close-button-normal-desktop', 'element');
    await captureLocator(page, page.locator('[data-testid="paneel-kast"]').first(), 'panelen-cabinet-target-normal-desktop', 'element');
    await selectPanelCabinet(page, 'Achterwand', 0);
    await savePanel(page);
    await openPanelReview(page);
    await capturePage(page, 'panelen-review-normal-desktop');
    await captureLocator(page, page.getByTestId('paneel-review-weergave'), 'panelen-review-normal-desktop', 'section');
    await captureLocator(page, page.locator('.paneel-review-item').first(), 'panelen-review-item-normal-desktop', 'element');

    await goToVerificatie(page);
    await capturePage(page, 'journey-verificatie-tasklist-desktop', 'journey');
    await capturePage(page, 'verificatie-tasklist-normal-desktop');
    await captureLocator(page, page.getByTestId('verificatie-taaklijst'), 'verificatie-tasklist-normal-desktop', 'section');
    await captureLocator(page, page.getByTestId('verificatie-start-button'), 'verificatie-start-button-normal-desktop', 'element');
    await captureLocator(page, page.getByTestId('verificatie-open-controle-button').first(), 'verificatie-open-button-normal-desktop', 'element');
    await startVerification(page);
    await capturePage(page, 'verificatie-detail-normal-desktop');
    await captureLocator(page, page.locator('.verificatie-check').first(), 'verificatie-measure-check-normal-desktop', 'section');
    await captureLocator(page, page.locator('.verificatie-check-cirkel').first(), 'verificatie-check-toggle-normal-desktop', 'element');
    await completeVisibleVerificationChecks(page);
    await click(page.getByRole('button', { name: /Afronden/ }));
    await page.getByRole('heading', { name: /Alle 1 kastmetingen gecontroleerd|1 van 1 panelen afgevinkt/ }).waitFor();
    await capturePage(page, 'verificatie-complete-desktop');
    await captureLocator(page, page.locator('.card.mb-4').first(), 'verificatie-completion-card-desktop', 'section');

    await goToBestellijst(page);
    await capturePage(page, 'journey-bestellijst-desktop', 'journey');
    await capturePage(page, 'bestellijst-normal-desktop');
    await captureLocator(page, page.locator('.bestellijst-toolbar').first(), 'bestellijst-toolbar-normal-desktop', 'section');
    await captureLocator(page, page.getByTestId('bestellijst-tabel').first(), 'bestellijst-table-normal-desktop', 'section');
    await captureLocator(page, page.getByTestId('bestellijst-open-exportflow-button'), 'bestellijst-export-button-normal-desktop', 'element');
    await captureLocator(page, page.locator('#toon-technische-bestellijst').first(), 'bestellijst-technical-toggle-normal-desktop', 'element');
    await openExportFlow(page);
    await fillExportBasics(page);
    await nextExportStep(page, 'bestellijst-export-step-preview');
    await nextExportStep(page, 'bestellijst-export-step-bevestig');
    await capturePage(page, 'bestellijst-export-confirm-desktop');
    await captureLocator(page, page.getByTestId('bestellijst-export-drawer'), 'bestellijst-export-drawer-confirm-desktop', 'section');
    await captureLocator(page, page.getByTestId('bestellijst-export-confirm-button'), 'bestellijst-export-confirm-button-desktop', 'element');
    await closeExportFlow(page);

    await goToZaagplan(page);
    await capturePage(page, 'journey-zaagplan-desktop', 'journey');
    await capturePage(page, 'zaagplan-normal-desktop');
    await captureLocator(page, page.locator('.zaagplan-toolbar').first(), 'zaagplan-toolbar-normal-desktop', 'section');
    await captureLocator(page, page.getByTestId('zaagplan-een-plaat-button'), 'zaagplan-focus-button-normal-desktop', 'element');
  });
}

async function captureDenseDesktop() {
  await withFreshPage(desktopViewport, async (page) => {
    await createDenseProject(page);

    await gotoAndWait(page, '/kasten', 'Stap 1: Indeling');
    await openIndelingWorkspace(page, 'Achterwand');
    await capturePage(page, 'indeling-dense-desktop');
    await captureLocator(page, page.getByTestId('actieve-wand-werkruimte'), 'indeling-active-workspace-dense-desktop', 'section');
    await captureLocator(page, page.locator('.indeling-object-rij').first(), 'indeling-object-row-dense-desktop', 'element');

    await goToPanelen(page);
    await assignPanelsForAllWalls(page, ['Achterwand', 'Linkerwand', 'Rechterwand']);
    await openPanelReview(page);
    await capturePage(page, 'panelen-review-dense-desktop');
    await captureLocator(page, page.locator('.paneel-review-samenvatting').first(), 'panelen-review-summary-dense-desktop', 'section');
    await captureLocator(page, page.locator('.paneel-review-weergave .card.shadow-sm').first(), 'panelen-review-group-dense-desktop', 'section');
    await captureLocator(page, page.locator('.paneel-review-item').first(), 'panelen-review-item-dense-desktop', 'element');

    await goToVerificatie(page);
    await capturePage(page, 'verificatie-tasklist-dense-desktop');
    await captureLocator(page, page.getByTestId('verificatie-taakgroep').first(), 'verificatie-taskgroup-dense-desktop', 'section');
    await captureLocator(page, page.getByTestId('verificatie-open-controle-button').first(), 'verificatie-open-button-dense-desktop', 'element');

    await goToBestellijst(page);
    await capturePage(page, 'bestellijst-dense-desktop');
    await captureLocator(page, page.locator('.bestellijst-overzicht-details').first(), 'bestellijst-breakdown-dense-desktop', 'section');
    await captureLocator(page, page.locator('.bestellijst-table-wrap').first(), 'bestellijst-table-dense-desktop', 'section');
    await captureLocator(page, page.locator('[data-testid="bestellijst-tabel"] tbody tr').first(), 'bestellijst-row-dense-desktop', 'element');

    await goToZaagplan(page);
    await capturePage(page, 'zaagplan-dense-desktop');

    await click(page.getByTestId('zaagplan-een-plaat-button'));
    await page.getByTestId('zaagplan-een-plaat-weergave').waitFor();
    await capturePage(page, 'zaagplan-focus-dense-desktop');
    await captureLocator(page, page.getByTestId('zaagplan-focus-toolbar'), 'zaagplan-focus-toolbar-dense-desktop', 'section');
    await captureLocator(page, page.getByTestId('zaagplan-volgende-plaat'), 'zaagplan-next-plate-dense-desktop', 'element');

    await page.getByTestId('zaagplan-plaatbreedte-input').fill('1000');
    await page.getByTestId('zaagplan-plaatbreedte-input').press('Tab');
    await page.getByTestId('zaagplan-plaathoogte-input').fill('1000');
    await page.getByTestId('zaagplan-plaathoogte-input').press('Tab');
    await page.getByTestId('zaagplan-niet-geplaatst-waarschuwing').waitFor();
    await capturePage(page, 'zaagplan-warning-desktop');
    await captureLocator(page, page.getByTestId('zaagplan-niet-geplaatst-waarschuwing'), 'zaagplan-warning-section-desktop', 'section');
  });
}

async function captureMobileSpotChecks() {
  await withFreshPage(mobileViewport, async (page) => {
    await createNormalProject(page);

    await gotoAndWait(page, '/', 'Ga verder met uw keukenproject');
    await capturePage(page, 'home-resume-mobile');
    await captureLocator(page, page.getByTestId('home-project-dashboard'), 'home-project-dashboard-mobile', 'section');
    await captureLocator(page, page.locator('.home-startflow .btn.btn-primary').first(), 'home-primary-cta-mobile', 'element');

    await gotoAndWait(page, '/kasten', 'Stap 1: Indeling');
    await openIndelingWorkspace(page, 'Achterwand');
    await capturePage(page, 'indeling-normal-mobile');
    await captureLocator(page, page.getByTestId('actieve-wand-werkruimte'), 'indeling-active-workspace-mobile', 'section');
    await captureLocator(page, page.getByTestId('open-kast-form-button'), 'indeling-add-cabinet-button-mobile', 'element');

    await goToPanelen(page);
    const workspace = await openPanelWand(page, 'Achterwand');
    await capturePage(page, 'panelen-workspace-mobile');
    await captureLocator(page, workspace, 'panelen-workspace-mobile', 'section');
    await captureLocator(page, page.getByTestId('paneel-stap-intro-compact'), 'panelen-compact-intro-mobile', 'element');
    await captureLocator(page, workspace.getByTestId('open-paneel-editor-button').first(), 'panelen-open-editor-mobile', 'element');

    await click(workspace.getByTestId('open-paneel-editor-button').first());
    await page.getByTestId('paneel-editor-drawer').waitFor();
    await capturePage(page, 'panelen-editor-mobile');
    await captureLocator(page, page.getByTestId('paneel-editor-drawer'), 'panelen-drawer-mobile', 'section');
    await captureLocator(page, page.getByTestId('close-paneel-editor-button'), 'panelen-close-editor-mobile', 'element');
  });
}

async function writeManifest() {
  const manifestPath = path.join(outputDir, 'manifest.json');
  const counts = summarizeCounts();
  await fs.writeFile(
    manifestPath,
    JSON.stringify(
      {
        baseURL,
        iterationLabel,
        phaseLabel,
        generatedAt: new Date().toISOString(),
        counts,
        captured,
      },
      null,
      2,
    ),
  );
}

function summarizeCounts() {
  return captured.reduce(
    (summary, shot) => {
      summary[shot.category] = (summary[shot.category] ?? 0) + 1;
      summary.total += 1;
      return summary;
    },
    { journey: 0, page: 0, section: 0, element: 0, total: 0 },
  );
}

async function withFreshPage(viewport, callback) {
  const context = await browser.newContext({ viewport });
  const page = await context.newPage();
  page.setDefaultTimeout(20_000);

  try {
    await gotoAndWait(page, '/', null);
    await page.evaluate(() => localStorage.clear());
    await gotoAndWait(page, '/', null);
    await callback(page);
  } finally {
    await context.close();
  }
}

async function createNormalProject(page) {
  await gotoAndWait(page, '/kasten', 'Stap 1: Indeling');
  await addWall(page, 'Achterwand');
  await openIndelingWorkspace(page, 'Achterwand');
  await addCabinet(page, 'Achterwand', {
    naam: 'Onderkast spoelbak',
    breedte: '600',
    hoogte: '720',
    diepte: '560',
  });
  await waitForPersistedProject(page, 1);
}

async function createDenseProject(page) {
  await gotoAndWait(page, '/kasten', 'Stap 1: Indeling');

  const walls = [
    {
      naam: 'Achterwand',
      kasten: [
        { naam: 'Spoelkast', breedte: '900', hoogte: '720', diepte: '560' },
        { naam: 'Lades links', breedte: '800', hoogte: '720', diepte: '560' },
        { naam: 'Vaatwasserpaneel', breedte: '600', hoogte: '720', diepte: '560' },
      ],
      apparaat: { naam: 'Vaatwasser', breedte: '600', hoogte: '820', diepte: '560' },
    },
    {
      naam: 'Linkerwand',
      kasten: [
        { naam: 'Hoge kast koelkast', breedte: '600', hoogte: '2100', diepte: '600' },
        { naam: 'Hoge kast oven', breedte: '600', hoogte: '2100', diepte: '600' },
        { naam: 'Voorraadkast', breedte: '500', hoogte: '2100', diepte: '600' },
      ],
      apparaat: { naam: 'Oven', breedte: '600', hoogte: '590', diepte: '560' },
    },
    {
      naam: 'Rechterwand',
      kasten: [
        { naam: 'Onderkast hoek', breedte: '900', hoogte: '720', diepte: '560' },
        { naam: 'Onderkast kookplaat', breedte: '800', hoogte: '720', diepte: '560' },
        { naam: 'Onderkast kruiden', breedte: '300', hoogte: '720', diepte: '560' },
      ],
      apparaat: null,
    },
  ];

  for (const wall of walls) {
    await addWall(page, wall.naam);
    await openIndelingWorkspace(page, wall.naam);

    for (const cabinet of wall.kasten) {
      await addCabinet(page, wall.naam, cabinet);
    }

    if (wall.apparaat) {
      await addAppliance(page, wall.naam, wall.apparaat);
    }
  }

  await waitForPersistedProject(page, 9);
}

async function addWall(page, wallName) {
  await page.getByTestId('nieuwe-wand-naam-input').fill(wallName);
  await click(page.getByTestId('wand-toevoegen-button'));
  await page.locator(`[data-wand-naam="${wallName}"]`).first().waitFor();
}

async function openIndelingWorkspace(page, wallName) {
  const workspace = page.locator(`[data-testid="actieve-wand-werkruimte"][data-wand-naam="${wallName}"]`);
  if (await workspace.isVisible().catch(() => false)) {
    return workspace;
  }

  const card = page.locator(`[data-testid="indeling-wand-card"][data-wand-naam="${wallName}"]`).first();
  await click(card.getByTestId('open-wand-workspace-button'));
  await workspace.waitFor();
  return workspace;
}

async function addCabinet(page, wallName, cabinet) {
  await openIndelingWorkspace(page, wallName);
  await click(
    page
      .locator(`[data-testid="actieve-wand-werkruimte"][data-wand-naam="${wallName}"]`)
      .getByTestId('open-kast-form-button'),
  );
  await page.getByTestId('kast-form').waitFor();

  await page.getByTestId('kast-naam-input').fill(cabinet.naam);
  await click(page.getByTestId('kast-form-volgende-button'));

  await page.getByTestId('kast-breedte-input').fill(cabinet.breedte);
  await page.getByTestId('kast-hoogte-input').fill(cabinet.hoogte);
  await page.getByTestId('kast-diepte-input').fill(cabinet.diepte);
  await click(page.getByTestId('kast-form-volgende-button'));
  await click(page.getByTestId('kast-form-volgende-button'));
  await click(page.getByTestId('kast-opslaan-button'));
  await page.getByTestId('kast-form').waitFor({ state: 'hidden' });
  await page
    .locator(`[data-testid="actieve-wand-werkruimte"][data-wand-naam="${wallName}"]`)
    .getByText(cabinet.naam)
    .first()
    .waitFor();
}

async function addAppliance(page, wallName, appliance) {
  await openIndelingWorkspace(page, wallName);
  await click(
    page
      .locator(`[data-testid="actieve-wand-werkruimte"][data-wand-naam="${wallName}"]`)
      .getByTestId('open-apparaat-form-button'),
  );
  await page.getByTestId('apparaat-form').waitFor();

  await page.getByTestId('apparaat-naam-input').fill(appliance.naam);
  await click(page.getByTestId('apparaat-form-volgende-button'));

  await page.getByTestId('apparaat-breedte-input').fill(appliance.breedte);
  await page.getByTestId('apparaat-hoogte-input').fill(appliance.hoogte);
  await page.getByTestId('apparaat-diepte-input').fill(appliance.diepte);
  await click(page.getByTestId('apparaat-form-volgende-button'));
  await click(page.getByRole('button', { name: /Apparaat toevoegen|Opslaan/ }));
  await page.getByTestId('apparaat-form').waitFor({ state: 'hidden' });
}

async function goToPanelen(page) {
  await click(page.getByTestId('stap-navigatie-volgende'));
  await page.waitForURL(/\/panelen$/);
  await page.getByRole('heading', { name: 'Stap 2: Panelen' }).waitFor();
}

async function openPanelWand(page, wallName) {
  const workspace = page.locator(`[data-testid="paneel-actieve-wand-werkruimte"][data-wand-naam="${wallName}"]`);
  if (!(await workspace.isVisible().catch(() => false))) {
    const card = page.locator(`[data-testid="paneel-wand-card"][data-wand-naam="${wallName}"]`).first();
    await click(card.getByTestId('open-paneel-wand-button'));
    await workspace.waitFor();
  }

  return workspace;
}

async function ensurePanelEditorOpen(page, wallName) {
  if (await page.getByTestId('paneel-editor-drawer').isVisible().catch(() => false)) {
    return;
  }

  const workspace = page.locator(`[data-testid="paneel-actieve-wand-werkruimte"][data-wand-naam="${wallName}"]`);
  const openButton = workspace.getByTestId('open-paneel-editor-button').first();
  await click(openButton);
  await page.getByTestId('paneel-editor-drawer').waitFor();
}

async function selectPanelCabinet(page, wallName, index) {
  const workspace = page.locator(`[data-testid="paneel-actieve-wand-werkruimte"][data-wand-naam="${wallName}"]`);
  const cabinet = workspace.locator('[data-testid="paneel-kast"]').nth(index);
  await cabinet.scrollIntoViewIfNeeded();
  await cabinet.click();
  await page.getByTestId('paneel-opslaan-button').waitFor();
}

async function savePanel(page) {
  await click(page.getByTestId('paneel-opslaan-button'));
  await page.getByTestId('paneel-editor-drawer').waitFor({ state: 'hidden' });
}

async function openPanelReview(page) {
  await click(page.getByTestId('paneel-review-weergave-tab'));
  await page.getByTestId('paneel-review-weergave').waitFor();
}

async function assignPanelsForAllWalls(page, wallNames) {
  for (const wallName of wallNames) {
    await click(page.getByTestId('paneel-editor-weergave-tab'));
    await page.getByTestId('paneel-editor-weergave').waitFor();
    const workspace = await openPanelWand(page, wallName);
    const cabinetCount = await workspace.locator('[data-testid="paneel-kast"]').count();

    for (let index = 0; index < cabinetCount; index += 1) {
      await ensurePanelEditorOpen(page, wallName);
      await selectPanelCabinet(page, wallName, index);
      await savePanel(page);
    }
  }
}

async function goToVerificatie(page) {
  await click(page.getByTestId('stap-navigatie-volgende'));
  await page.waitForURL(/\/verificatie$/);
  await page.getByRole('heading', { name: 'Stap 3: Verificatie' }).waitFor();
}

async function startVerification(page) {
  await click(page.getByTestId('verificatie-start-button'));
  await page.getByRole('heading', { name: 'Meet de maat in de opening na' }).waitFor();
}

async function completeVisibleVerificationChecks(page) {
  const checks = page.locator('.verificatie-check-cirkel');
  const count = await checks.count();

  for (let index = 0; index < count; index += 1) {
    await checks.nth(index).click();
    await page.waitForTimeout(100);
  }
}

async function goToBestellijst(page) {
  await click(page.getByTestId('stap-navigatie-volgende'));
  await page.waitForURL(/\/bestellijst$/);
  await page.getByRole('heading', { name: 'Stap 4: Bestellijst' }).waitFor();
}

async function openExportFlow(page) {
  await click(page.getByTestId('bestellijst-open-exportflow-button'));
  await page.getByTestId('bestellijst-export-drawer').waitFor();
}

async function fillExportBasics(page) {
  await page.getByTestId('bestellijst-paneeltype-input').fill('MDF wit');
  await page.getByTestId('bestellijst-dikte-input').fill('19');
  await click(page.getByTestId('bestellijst-export-type-excel'));
}

async function nextExportStep(page, testId) {
  await click(page.getByTestId('bestellijst-export-next-button'));
  await page.getByTestId(testId).waitFor();
}

async function goToZaagplan(page) {
  await click(page.getByTestId('stap-navigatie-volgende'));
  await page.waitForURL(/\/zaagplan$/);
  await page.getByRole('heading', { name: 'Stap 5: Zaagplan' }).waitFor();
}

async function closeExportFlow(page) {
  const closeButton = page.locator('.bestellijst-export-drawer .btn-close');
  if (await closeButton.isVisible().catch(() => false)) {
    await click(closeButton);
    await page.getByTestId('bestellijst-export-drawer').waitFor({ state: 'hidden' });
  }
}

async function gotoAndWait(page, route, headingText) {
  await page.goto(`${baseURL}${route}`, { waitUntil: 'networkidle' });
  if (headingText) {
    await page.getByRole('heading', { name: headingText }).waitFor();
  }
  await page.waitForTimeout(250);
}

async function waitForPersistedProject(page, expectedCabinets, expectedAssignments = null) {
  await page.waitForFunction(
    ({ expectedCabinets: minimumCabinets, expectedAssignments: minimumAssignments }) => {
      const json = localStorage.getItem('keuken-inmeten-data');
      if (!json) {
        return false;
      }

      try {
        const parsed = JSON.parse(json);
        const data = parsed.data ?? parsed;
        const cabinetCount = Array.isArray(data.kasten) ? data.kasten.length : 0;
        const assignmentCount = Array.isArray(data.toewijzingen) ? data.toewijzingen.length : 0;
        return (
          cabinetCount >= minimumCabinets
          && (minimumAssignments == null || assignmentCount >= minimumAssignments)
        );
      } catch {
        return false;
      }
    },
    { expectedCabinets, expectedAssignments },
  );

  await page.waitForTimeout(150);
}

async function capturePage(page, name, category = 'page') {
  const relativePath = path.join('.agent', 'screenshots', iterationLabel, phaseLabel, category, `${name}.png`);
  const absolutePath = path.resolve(relativePath);
  await page.screenshot({ path: absolutePath, fullPage: true });
  captured.push({ category, name, path: relativePath });
}

async function captureLocator(page, locator, name, category) {
  const target = locator.first();
  await target.waitFor({ state: 'visible' });
  await target.scrollIntoViewIfNeeded();
  await page.waitForTimeout(200);

  const relativePath = path.join('.agent', 'screenshots', iterationLabel, phaseLabel, category, `${name}.png`);
  const absolutePath = path.resolve(relativePath);
  await target.screenshot({ path: absolutePath, animations: 'disabled' });
  captured.push({ category, name, path: relativePath });
}

async function click(locator) {
  await locator.scrollIntoViewIfNeeded();
  await locator.click();
}
