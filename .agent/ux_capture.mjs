import { chromium } from '@playwright/test';
import fs from 'node:fs/promises';
import path from 'node:path';

const baseURL = process.env.BASE_URL ?? 'http://127.0.0.1:4173';
const iterationLabel = process.argv[2] ?? 'iteration-1';
const phaseLabel = process.argv[3] ?? 'before';
const outputDir = path.resolve('.agent', 'screenshots', iterationLabel, phaseLabel);
const desktopViewport = { width: 1440, height: 1200 };
const mobileViewport = { width: 390, height: 844 };
const captured = [];

await fs.mkdir(outputDir, { recursive: true });

const browser = await chromium.launch({ headless: true });

try {
  await captureDesktopScenarios();
  await captureMobileSpotChecks();
  console.log(JSON.stringify({ outputDir, captured }, null, 2));
} finally {
  await browser.close();
}

async function captureDesktopScenarios() {
  await withFreshPage(desktopViewport, async (page) => {
    await gotoAndWait(page, '/', 'Van keukenmaat naar paneel, boorgat en zaagplan');
    await screenshot(page, 'home-empty-desktop');

    await gotoAndWait(page, '/kasten', 'Stap 1: Indeling');
    await screenshot(page, 'indeling-empty-desktop');

    await addWall(page, 'Achterwand');
    await openIndelingWorkspace(page, 'Achterwand');
    await click(page.getByTestId('open-kast-form-button'));
    await page.getByTestId('kast-form').waitFor();
    await screenshot(page, 'indeling-kast-form-validation-desktop');
    await click(page.getByRole('button', { name: 'Annuleren' }).last());

    await page.goto(`${baseURL}/panelen`, { waitUntil: 'networkidle' });
    await page.getByRole('alert').waitFor();
    await page.getByText('Stap 2: Panelen is nog niet beschikbaar').waitFor();
    await page.waitForTimeout(250);
    await screenshot(page, 'panelen-route-gate-desktop');

    await gotoAndWait(page, '/?share=invalid', 'Van keukenmaat naar paneel, boorgat en zaagplan');
    await screenshot(page, 'home-invalid-share-desktop');
  });

  await withFreshPage(desktopViewport, async (page) => {
    await createNormalProject(page);

    await gotoAndWait(page, '/', 'Ga verder met uw keukenproject');
    await screenshot(page, 'home-resume-desktop');

    await gotoAndWait(page, '/kasten', 'Stap 1: Indeling');
    await screenshot(page, 'indeling-normal-desktop');

    await goToPanelen(page);
    await openPanelWorkspace(page, 'Achterwand');
    await selectPanelCabinet(page, 'Achterwand', 0);
    await screenshot(page, 'panelen-editor-normal-desktop');
    await savePanel(page);
    await openPanelReview(page);
    await screenshot(page, 'panelen-review-normal-desktop');

    await goToVerificatie(page);
    await screenshot(page, 'verificatie-tasklist-normal-desktop');
    await startVerification(page);
    await screenshot(page, 'verificatie-detail-normal-desktop');
    await completeVisibleVerificationChecks(page);
    await click(page.getByRole('button', { name: /Afronden/ }));
    await page.getByRole('heading', { name: /Alle 1 kastmetingen gecontroleerd|1 van 1 panelen afgevinkt/ }).waitFor();
    await screenshot(page, 'verificatie-complete-desktop');

    await goToBestellijst(page);
    await screenshot(page, 'bestellijst-normal-desktop');
    await openExportFlow(page);
    await fillExportBasics(page);
    await nextExportStep(page, 'bestellijst-export-step-preview');
    await nextExportStep(page, 'bestellijst-export-step-bevestig');
    await screenshot(page, 'bestellijst-export-confirm-desktop');
    await closeExportFlow(page);

    await goToZaagplan(page);
    await screenshot(page, 'zaagplan-normal-desktop');
  });

  await withFreshPage(desktopViewport, async (page) => {
    await createDenseProject(page);

    await gotoAndWait(page, '/kasten', 'Stap 1: Indeling');
    await openIndelingWorkspace(page, 'Achterwand');
    await screenshot(page, 'indeling-dense-desktop');

    await goToPanelen(page);
    await assignPanelsForAllWalls(page, ['Achterwand', 'Linkerwand', 'Rechterwand']);
    await openPanelReview(page);
    await screenshot(page, 'panelen-review-dense-desktop');

    await goToVerificatie(page);
    await screenshot(page, 'verificatie-tasklist-dense-desktop');

    await goToBestellijst(page);
    await screenshot(page, 'bestellijst-dense-desktop');

    await goToZaagplan(page);
    await screenshot(page, 'zaagplan-dense-desktop');

    await click(page.getByTestId('zaagplan-een-plaat-button'));
    await page.getByTestId('zaagplan-een-plaat-weergave').waitFor();
    await screenshot(page, 'zaagplan-focus-dense-desktop');

    await page.getByTestId('zaagplan-plaatbreedte-input').fill('1000');
    await page.getByTestId('zaagplan-plaatbreedte-input').press('Tab');
    await page.getByTestId('zaagplan-plaathoogte-input').fill('1000');
    await page.getByTestId('zaagplan-plaathoogte-input').press('Tab');
    await page.getByTestId('zaagplan-niet-geplaatst-waarschuwing').waitFor();
    await screenshot(page, 'zaagplan-warning-desktop');
  });
}

async function captureMobileSpotChecks() {
  await withFreshPage(mobileViewport, async (page) => {
    await createNormalProject(page);

    await gotoAndWait(page, '/', 'Ga verder met uw keukenproject');
    await screenshot(page, 'home-resume-mobile');

    await gotoAndWait(page, '/kasten', 'Stap 1: Indeling');
    await openIndelingWorkspace(page, 'Achterwand');
    await screenshot(page, 'indeling-normal-mobile');

    await goToPanelen(page);
    const workspace = page.locator('[data-testid="paneel-actieve-wand-werkruimte"][data-wand-naam="Achterwand"]');
    await click(page.locator('[data-testid="paneel-wand-card"][data-wand-naam="Achterwand"]').first().getByTestId('open-paneel-wand-button'));
    await workspace.waitFor();
    await screenshot(page, 'panelen-workspace-mobile');

    await click(workspace.getByTestId('open-paneel-editor-button'));
    await page.getByTestId('paneel-editor-drawer').waitFor();
    await screenshot(page, 'panelen-editor-mobile');
  });
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
    diepte: '560'
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
        { naam: 'Vaatwasserpaneel', breedte: '600', hoogte: '720', diepte: '560' }
      ],
      apparaat: { naam: 'Vaatwasser', breedte: '600', hoogte: '820', diepte: '560' }
    },
    {
      naam: 'Linkerwand',
      kasten: [
        { naam: 'Hoge kast koelkast', breedte: '600', hoogte: '2100', diepte: '600' },
        { naam: 'Hoge kast oven', breedte: '600', hoogte: '2100', diepte: '600' },
        { naam: 'Voorraadkast', breedte: '500', hoogte: '2100', diepte: '600' }
      ],
      apparaat: { naam: 'Oven', breedte: '600', hoogte: '590', diepte: '560' }
    },
    {
      naam: 'Rechterwand',
      kasten: [
        { naam: 'Onderkast hoek', breedte: '900', hoogte: '720', diepte: '560' },
        { naam: 'Onderkast kookplaat', breedte: '800', hoogte: '720', diepte: '560' },
        { naam: 'Onderkast kruiden', breedte: '300', hoogte: '720', diepte: '560' }
      ],
      apparaat: null
    }
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
    return;
  }

  const card = page.locator(`[data-testid="indeling-wand-card"][data-wand-naam="${wallName}"]`).first();
  await click(card.getByTestId('open-wand-workspace-button'));
  await workspace.waitFor();
}

async function addCabinet(page, wallName, cabinet) {
  await openIndelingWorkspace(page, wallName);
  await click(page.locator(`[data-testid="actieve-wand-werkruimte"][data-wand-naam="${wallName}"]`).getByTestId('open-kast-form-button'));
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
  await click(page.locator(`[data-testid="actieve-wand-werkruimte"][data-wand-naam="${wallName}"]`).getByTestId('open-apparaat-form-button'));
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

async function openPanelWorkspace(page, wallName) {
  const workspace = page.locator(`[data-testid="paneel-actieve-wand-werkruimte"][data-wand-naam="${wallName}"]`);
  if (!(await workspace.isVisible().catch(() => false))) {
    const card = page.locator(`[data-testid="paneel-wand-card"][data-wand-naam="${wallName}"]`).first();
    await click(card.getByTestId('open-paneel-wand-button'));
    await workspace.waitFor();
  }

  if (!(await page.getByTestId('paneel-editor-drawer').isVisible().catch(() => false))) {
    const openEditorButtons = workspace.getByTestId('open-paneel-editor-button');
    if (await openEditorButtons.count()) {
      await click(openEditorButtons.first());
    } else {
      await click(page.getByTestId('open-paneel-editor-button').first());
    }
  }

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
    await openPanelWorkspace(page, wallName);
    const workspace = page.locator(`[data-testid="paneel-actieve-wand-werkruimte"][data-wand-naam="${wallName}"]`);
    const cabinetCount = await workspace.locator('[data-testid="paneel-kast"]').count();

    for (let index = 0; index < cabinetCount; index += 1) {
      if (!(await page.getByTestId('paneel-editor-drawer').isVisible().catch(() => false))) {
        await click(page.getByTestId('open-paneel-editor-button').first());
        await page.getByTestId('paneel-editor-drawer').waitFor();
      }

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
        return cabinetCount >= minimumCabinets
          && (minimumAssignments == null || assignmentCount >= minimumAssignments);
      } catch {
        return false;
      }
    },
    { expectedCabinets, expectedAssignments }
  );

  await page.waitForTimeout(150);
}

async function screenshot(page, name) {
  const relativePath = path.join('.agent', 'screenshots', iterationLabel, phaseLabel, `${name}.png`);
  const absolutePath = path.resolve(relativePath);
  await page.screenshot({ path: absolutePath, fullPage: true });
  captured.push(relativePath);
}

async function click(locator) {
  await locator.scrollIntoViewIfNeeded();
  await locator.click();
}
