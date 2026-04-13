import { readFile } from 'node:fs/promises';
import { expect, test } from '@playwright/test';
import { IndelingPage } from '../pages/IndelingPage';

test('home toont een compactere start zonder extra uitlegblokken', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Stappen' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Voor u start' })).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Meer uitleg (optioneel)' })).toHaveCount(0);
  await expect(page.getByText('De volgorde en labels hieronder zijn gelijk aan de rest van de applicatie.')).toHaveCount(0);
  await expect(page.getByText('Bouw uw keuken op per wand en voeg kasten toe.')).toHaveCount(0);
  await expect(page.getByText('Wijs panelen toe en bepaal maat, plaatsing en scharnierzijde.')).toHaveCount(0);
  await expect(page.locator('#startflow-stappen').getByRole('heading', { name: 'Indeling' })).toBeVisible();
});

test('home toont een hervatdashboard zodra er projectdata bestaat', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast hervatten',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });

  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Ga verder met uw keukenproject' })).toBeVisible();
  await expect(page.getByTestId('home-project-dashboard')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Projectoverzicht' })).toBeVisible();
  await expect(page.getByTestId('home-onboarding-help')).toHaveCount(0);
});

test('navbar deelt een v4-link naar de huidige stap die in een schone sessie opnieuw laadt', async ({ page, context, browser }) => {
  const indeling = new IndelingPage(page);

  await context.grantPermissions(['clipboard-read', 'clipboard-write']);
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'share', { value: undefined, configurable: true });
    Object.defineProperty(navigator, 'canShare', { value: undefined, configurable: true });
  });

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast delen',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });

  await page.getByTestId('nav-share-button').click();

  await expect(page.getByTestId('actie-feedback-toast')).toContainText('De deellink is gekopieerd.');

  let deelUrl = '';
  await expect.poll(async () => {
    deelUrl = await page.evaluate(() => navigator.clipboard.readText());
    return deelUrl;
  }).toContain('/kasten?s=v4.');

  const schoneContext = await browser.newContext();
  try {
    const gedeeldePagina = await schoneContext.newPage();
    const gedeeldeIndeling = new IndelingPage(gedeeldePagina);

    await gedeeldePagina.goto(deelUrl);

    await expect(gedeeldePagina).toHaveURL(/\/kasten\?s=v4\./);
    await expect(gedeeldePagina.getByText('Achterwand')).toBeVisible();
    await gedeeldeIndeling.openWandWerkruimte('Achterwand');
    await expect(gedeeldePagina.locator('span.fw-semibold').filter({ hasText: 'Onderkast delen' })).toBeVisible();
  } finally {
    await schoneContext.close();
  }
});

test('navbar exporteert projectjson en stap 1 kan het project volledig wissen en via een importmodal terug laden', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast export',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });

  const downloadPromise = page.waitForEvent('download');
  await page.getByTestId('nav-export-button').click();
  const download = await downloadPromise;

  expect(download.suggestedFilename()).toMatch(/^keuken-inmeten-\d{8}-\d{4}\.json$/);

  const downloadPad = await download.path();
  expect(downloadPad).not.toBeNull();

  const jsonMetBom = await readFile(downloadPad!, 'utf8');
  const exportJson = jsonMetBom.replace(/^\uFEFF/, '');
  const exportData = JSON.parse(exportJson);

  expect(exportData.schemaVersion).toBeGreaterThan(0);
  expect(exportData.data.wanden).toHaveLength(1);

  await expect(page.getByTestId('indeling-project-acties-details')).toHaveCount(0);
  await page.getByTestId('nav-delete-button').click();
  await expect(page.getByTestId('nav-delete-confirmation')).toBeVisible();
  await page.getByTestId('nav-delete-confirm-button').click();
  await expect(page.getByTestId('nav-delete-confirmation')).toHaveCount(0);

  await expect(page.getByTestId('actie-feedback-toast')).toContainText('Het keukenproject is gewist.');
  await expect(page.getByText('Begin door een wand toe te voegen.')).toBeVisible();
  await expect(page.getByTestId('nav-delete-button')).toHaveCount(0);

  await expect(page.getByTestId('nav-import-modal')).toHaveCount(0);
  await page.getByTestId('nav-import-button').click();
  await expect(page.getByTestId('nav-import-modal')).toBeVisible();
  await expect(page.getByTestId('nav-import-confirm-button')).toBeDisabled();

  await page.getByTestId('nav-import-input').setInputFiles({
    name: 'keuken-project.json',
    mimeType: 'application/json',
    buffer: Buffer.from(jsonMetBom, 'utf8'),
  });

  await expect(page.getByTestId('nav-import-selected-file')).toContainText('keuken-project.json');
  await expect(page.getByTestId('nav-import-confirm-button')).toBeEnabled();
  await page.getByTestId('nav-import-confirm-button').click();

  await expect(page.getByTestId('actie-feedback-toast')).toContainText("Project 'keuken-project.json' is geladen.");
  await expect(page.getByTestId('nav-import-modal')).toHaveCount(0);

  await indeling.openWandWerkruimte('Achterwand');
  await expect(page.getByTestId('actieve-wand-werkruimte')).toContainText('Onderkast export');
});
