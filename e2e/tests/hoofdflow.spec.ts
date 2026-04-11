import { readFile } from 'node:fs/promises';
import { expect, test } from '@playwright/test';
import { BestellijstPage } from '../pages/BestellijstPage';
import { IndelingPage } from '../pages/IndelingPage';
import { PanelenPage } from '../pages/PanelenPage';
import { ZaagplanPage } from '../pages/ZaagplanPage';
import { VerificatiePage } from '../pages/VerificatiePage';

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
  await expect(page.getByTestId('home-onboarding-help')).toBeVisible();
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

  await page.getByTestId('indeling-project-acties-summary').click();
  await page.getByRole('button', { name: 'Wis hele keuken' }).click();
  await page.getByRole('button', { name: 'Ja, wis alles' }).click();

  await expect(page.getByTestId('actie-feedback-toast')).toContainText('Het keukenproject is gewist.');
  await expect(page.getByText('Begin door een wand toe te voegen.')).toBeVisible();

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

test('actieve werkruimtes tonen minder overbodige uitleg in stap 1 en stap 2', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Keuken vanaf vaatwasser');
  await indeling.voegWandToe('Lades');
  await indeling.voegKastToeAanWand('Keuken vanaf vaatwasser', {
    naam: 'Onderkast links',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.voegKastToeAanWand('Lades', {
    naam: 'Ladekast',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });

  await indeling.openWandWerkruimte('Keuken vanaf vaatwasser');
  await expect(page.getByText('Maak wandindelingen aan en open daarna steeds één wand om maten, kasten en objecten rustig per werkruimte uit te werken.')).toHaveCount(0);
  await expect(page.getByText('U werkt nu in één actieve wand. Andere wanden blijven als compacte schakelaars beschikbaar, zodat de werkruimte rustig in beeld blijft.')).toHaveCount(0);
  await expect(page.getByText('Wissel hier snel van wand zonder dat de actieve werkruimte uit beeld verdwijnt.')).toHaveCount(0);
  await expect(page.getByText('WERKMODUS', { exact: true })).toHaveCount(0);

  await indeling.gaNaarPanelen();
  await panelen.expectLoaded();
  await panelen.openWandWerkruimte('Keuken vanaf vaatwasser');
  await expect(page.getByText('Wissel hier van wand zonder de actieve editorflow kwijt te raken.')).toHaveCount(0);
});

test('paneel-editor opent na sluiten weer met schone standaardwaarden', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });

  await indeling.gaNaarPanelen();
  await panelen.expectLoaded();
  await panelen.selecteerEersteKastOpWand('Achterwand');

  const blindpaneelKnop = page.getByTestId('paneel-type-button-BlindPaneel');
  const deurKnop = page.getByTestId('paneel-type-button-Deur');
  const linksKnop = page.getByTestId('paneel-scharnier-links-button');
  const rechtsKnop = page.getByTestId('paneel-scharnier-rechts-button');
  const potHartInput = page.getByTestId('paneel-pot-hart-input');

  await blindpaneelKnop.click();
  await expect(blindpaneelKnop).toHaveClass(/btn-primary/);
  await page.getByTestId('close-paneel-editor-button').click();

  await page.getByTestId('open-paneel-editor-button').click();
  await expect(deurKnop).toHaveClass(/btn-primary/);
  await expect(blindpaneelKnop).toHaveClass(/btn-outline-secondary/);

  await rechtsKnop.click();
  await potHartInput.fill('24.5');
  await expect(rechtsKnop).toHaveClass(/btn-primary/);
  await expect(potHartInput).toHaveValue('24.5');
  await page.getByTestId('close-paneel-editor-button').click();

  await page.getByTestId('open-paneel-editor-button').click();
  await expect(linksKnop).toHaveClass(/btn-primary/);
  await expect(rechtsKnop).toHaveClass(/btn-outline-secondary/);
  await expect(potHartInput).toHaveValue('22.5');
});

test('paneelselectie laat dezelfde kast met een tweede klik weer los', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast selectie',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });

  await indeling.gaNaarPanelen();
  await panelen.expectLoaded();
  await panelen.openWandWerkruimte('Achterwand');

  const kastBody = page
    .locator('[data-testid="paneel-actieve-wand-werkruimte"][data-wand-naam="Achterwand"]')
    .locator('[data-testid="paneel-kast"]')
    .locator('rect')
    .first();

  await kastBody.scrollIntoViewIfNeeded();
  const box = await kastBody.boundingBox();
  expect(box).not.toBeNull();

  const clickX = box!.x + box!.width / 2;
  const clickY = box!.y + box!.height / 2;

  await page.mouse.click(clickX, clickY);
  await expect(page.getByTestId('paneel-opslaan-button')).toBeVisible();

  await kastBody.scrollIntoViewIfNeeded();
  const geselecteerdeBox = await kastBody.boundingBox();
  expect(geselecteerdeBox).not.toBeNull();

  await page.mouse.click(
    geselecteerdeBox!.x + geselecteerdeBox!.width / 2,
    geselecteerdeBox!.y + geselecteerdeBox!.height / 2,
  );
  await expect(page.getByText('Selecteer nu kast(en) in de tekening')).toBeVisible();
  await expect(page.getByTestId('paneel-opslaan-button')).toHaveCount(0);
});

test('paneel-editor deelt een geselecteerde kast op in meerdere fronten', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Ladeblok',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });

  await indeling.gaNaarPanelen();
  await panelen.expectLoaded();
  await panelen.selecteerEersteKastOpWand('Achterwand');

  await page.getByTestId('paneel-type-button-LadeFront').click();
  await panelen.deelGeselecteerdeKastOp([200, 220, 300]);

  await panelen.openReviewWeergave();
  const groep = page.locator('[data-testid="paneel-review-groep"][data-wand-naam="Achterwand"]');
  await expect(groep.locator('.paneel-review-item')).toHaveCount(3);

  await groep.getByRole('button', { name: 'Bewerk' }).first().click();
  await expect(page.getByTestId('paneel-editor-drawer')).toBeVisible();
  await expect(page.getByTestId('paneel-type-button-LadeFront')).toHaveClass(/btn-primary/);
});

test('panelenoverzicht en verificatie houden wanden met gelijke namen gescheiden', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);
  const verificatie = new VerificatiePage(page);

  async function openIndelingWand(wandId: string) {
    const kaart = page.locator(`[data-testid="indeling-wand-card"][data-wand-id="${wandId}"]`);
    const knop = kaart.getByTestId('open-wand-workspace-button');
    if (!(await knop.isVisible())) {
      const samenvatting = page.getByTestId('indeling-overige-wanden-summary');
      if (await samenvatting.isVisible()) {
        await samenvatting.click();
        await expect(knop).toBeVisible();
      }
    }

    await knop.click();
    await expect(page.locator(`[data-testid="actieve-wand-werkruimte"][data-wand-id="${wandId}"]`)).toBeVisible();
  }

  async function openPaneelWand(wandId: string) {
    const kaart = page.locator(`[data-testid="paneel-wand-card"][data-wand-id="${wandId}"]`);
    const knop = kaart.getByTestId('open-paneel-wand-button');
    if (!(await knop.isVisible())) {
      const samenvatting = page.getByTestId('paneel-overige-wanden-summary');
      if (await samenvatting.isVisible()) {
        await samenvatting.click();
        await expect(knop).toBeVisible();
      }
    }

    await knop.click();
    await expect(page.locator(`[data-testid="paneel-actieve-wand-werkruimte"][data-wand-id="${wandId}"]`)).toBeVisible();
  }

  await indeling.goto();
  await indeling.openWandToevoegenModal();
  await page.getByTestId('nieuwe-wand-naam-input').fill('Muur');
  await page.getByTestId('wand-toevoegen-button').click();
  await expect(page.getByTestId('wand-toevoegen-modal')).toBeHidden();

  await indeling.openWandToevoegenModal();
  await page.getByTestId('nieuwe-wand-naam-input').fill('Muur');
  await page.getByTestId('wand-toevoegen-button').click();
  await expect(page.getByTestId('wand-toevoegen-modal')).toBeHidden();

  const wandKaarten = page.locator('[data-testid="indeling-wand-card"][data-wand-naam="Muur"]');
  await expect(wandKaarten).toHaveCount(2);
  const eersteWandId = await wandKaarten.nth(0).getAttribute('data-wand-id');
  const tweedeWandId = await wandKaarten.nth(1).getAttribute('data-wand-id');

  expect(eersteWandId).toBeTruthy();
  expect(tweedeWandId).toBeTruthy();
  expect(eersteWandId).not.toBe(tweedeWandId);

  await openIndelingWand(eersteWandId!);
  await indeling.voegKastToeAanWand('Muur', {
    naam: 'Onderkast links',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await page.getByRole('button', { name: 'Werkruimte sluiten' }).click();

  await openIndelingWand(tweedeWandId!);
  await page.getByTestId('open-kast-form-button').click();
  await page.getByTestId('kast-naam-input').fill('Onderkast rechts');
  await indeling.gaNaarVolgendeKastFormStap();
  await page.getByTestId('kast-breedte-input').fill('600');
  await page.getByTestId('kast-hoogte-input').fill('720');
  await page.getByTestId('kast-diepte-input').fill('560');
  await indeling.gaNaarVolgendeKastFormStap();
  await indeling.bevestigTechnischeKastControle();
  await indeling.gaNaarVolgendeKastFormStap();
  await page.getByTestId('kast-opslaan-button').click();
  await page.getByRole('button', { name: 'Werkruimte sluiten' }).click();

  await indeling.gaNaarPanelen();
  await panelen.expectLoaded();

  await openPaneelWand(eersteWandId!);
  await page.locator('[data-testid="paneel-kast"]').first().click();
  await page.getByTestId('paneel-opslaan-button').click();
  await page.getByRole('button', { name: 'Werkruimte sluiten' }).click();

  await openPaneelWand(tweedeWandId!);
  await page.locator('[data-testid="paneel-kast"]').first().click();
  await page.getByTestId('paneel-opslaan-button').click();

  await panelen.openReviewWeergave();
  await expect(page.getByTestId('paneel-review-groep')).toHaveCount(2);
  await expect(page.locator(`[data-testid="paneel-review-groep"][data-wand-id="${eersteWandId}"]`)).toBeVisible();
  await expect(page.locator(`[data-testid="paneel-review-groep"][data-wand-id="${tweedeWandId}"]`)).toBeVisible();

  await panelen.gaNaarVerificatie();
  await verificatie.expectLoaded();
  await verificatie.expectTaaklijst();
  await expect(page.getByTestId('verificatie-taakgroep')).toHaveCount(2);
  await expect(page.locator(`[data-testid="verificatie-taakgroep"][data-wand-id="${eersteWandId}"]`)).toBeVisible();
  await expect(page.locator(`[data-testid="verificatie-taakgroep"][data-wand-id="${tweedeWandId}"]`)).toBeVisible();
});

test('stap 1 toont maar één actieve wandwerkruimte tegelijk', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegWandToe('Linkerwand');

  await indeling.openWandWerkruimte('Achterwand');
  await indeling.expectActieveWerkruimte('Achterwand');

  await indeling.openWandWerkruimte('Linkerwand');
  await indeling.expectActieveWerkruimte('Linkerwand');
});

test('wandenoverzicht toont geen extra toelichting onder de titel', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegWandToe('Linkerwand');

  await expect(page.getByRole('heading', { name: 'Wandenoverzicht' })).toBeVisible();
  await expect(
    page.getByText(
      'Kies daarna precies één wand om maten, kasten, objecten en de visualisatie per werkruimte te bewerken.',
    ),
  ).toHaveCount(0);
});

test('wandenoverzicht kaarten gebruiken compactere spacing', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegWandToe('Linkerwand');

  const compacteStijlen = await page.getByTestId('indeling-wand-card').first().evaluate((kaart) => {
    const body = kaart.querySelector('.indeling-wand-samenvatting-body');
    const button = kaart.querySelector('[data-testid="open-wand-workspace-button"]');

    if (!(body instanceof HTMLElement) || !(button instanceof HTMLElement)) {
      throw new Error('Expected compact wall card structure');
    }

    const bodyStyle = getComputedStyle(body);
    const buttonStyle = getComputedStyle(button);

    return {
      bodyPaddingTop: parseFloat(bodyStyle.paddingTop),
      bodyPaddingBottom: parseFloat(bodyStyle.paddingBottom),
      bodyRowGap: parseFloat(bodyStyle.rowGap),
      buttonPaddingTop: parseFloat(buttonStyle.paddingTop),
      buttonPaddingBottom: parseFloat(buttonStyle.paddingBottom),
    };
  });

  expect(compacteStijlen.bodyPaddingTop).toBeLessThan(14);
  expect(compacteStijlen.bodyPaddingBottom).toBeLessThan(14);
  expect(compacteStijlen.bodyRowGap).toBeLessThanOrEqual(6);
  expect(compacteStijlen.buttonPaddingTop).toBeLessThanOrEqual(4);
  expect(compacteStijlen.buttonPaddingBottom).toBeLessThanOrEqual(4);
});

test('wandenkaarten tonen geen extra toelichting onder de samenvatting', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegWandToe('Linkerwand');

  await expect(page.getByTestId('indeling-wand-card')).toHaveCount(2);
  await expect(page.getByTestId('indeling-wand-card-description')).toHaveCount(0);
  await expect(page.getByTestId('open-wand-workspace-button')).toHaveCount(2);
});

test('kastpopup werkt als een korte ministepper', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.openKastFormulierVoorWand('Achterwand');

  await indeling.expectKastFormStap('Basis');
  await page.getByTestId('kast-naam-input').fill('Testkast');
  await indeling.gaNaarVolgendeKastFormStap();

  await indeling.expectKastFormStap('Maten');
  await page.getByTestId('kast-breedte-input').fill('600');
  await page.getByTestId('kast-hoogte-input').fill('720');
  await page.getByTestId('kast-diepte-input').fill('560');
  await indeling.gaNaarVolgendeKastFormStap();

  await indeling.expectKastFormStap('Techniek');
  await expect(page.getByTestId('kast-form-volgende-button')).toBeDisabled();
  await indeling.bevestigTechnischeKastControle();
  await expect(page.getByTestId('kast-form-volgende-button')).toBeEnabled();
  await indeling.gaNaarVolgendeKastFormStap();

  await indeling.expectKastFormStap('Controle');
  await expect(page.getByText('Standaard bevestigd')).toBeVisible();
});

test('invoerhulp blijft zichtbaar bij wand-, kast- en apparaatvelden', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();

  await indeling.openWandToevoegenModal();
  await expect(page.getByTestId('nieuwe-wand-naam-hint')).toBeVisible();
  await page.getByTestId('nieuwe-wand-naam-input').fill('Achterwand');
  await expect(page.getByTestId('nieuwe-wand-naam-hint')).toBeVisible();
  await page.getByTestId('wand-toevoegen-button').click();

  await indeling.openWandWerkruimte('Achterwand');
  await expect(page.getByTestId('wand-breedte-hint')).toBeVisible();
  await expect(page.getByTestId('wand-hoogte-hint')).toBeVisible();
  await expect(page.getByTestId('wand-plint-hint')).toBeVisible();

  const kastForm = await indeling.openKastFormulierVoorWand('Achterwand');
  await expect(page.getByTestId('kast-naam-hint')).toBeVisible();
  await page.getByTestId('kast-naam-input').fill('Onderkast test');
  await expect(page.getByTestId('kast-naam-hint')).toBeVisible();
  await indeling.gaNaarVolgendeKastFormStap();
  await expect(page.getByTestId('kast-breedte-hint')).toBeVisible();
  await expect(page.getByTestId('kast-hoogte-hint')).toBeVisible();
  await expect(page.getByTestId('kast-diepte-hint')).toBeVisible();
  await kastForm.getByRole('button', { name: 'Annuleren' }).click();
  await expect(kastForm).toBeHidden();

  const apparaatForm = await indeling.openApparaatFormulierVoorWand('Achterwand');
  await expect(page.getByTestId('apparaat-naam-hint')).toBeVisible();
  await page.getByTestId('apparaat-naam-input').fill('Oven test');
  await expect(page.getByTestId('apparaat-naam-hint')).toBeVisible();
  await indeling.gaNaarVolgendeApparaatFormStap();
  await expect(page.getByTestId('apparaat-breedte-hint')).toBeVisible();
  await expect(page.getByTestId('apparaat-hoogte-hint')).toBeVisible();
  await expect(page.getByTestId('apparaat-diepte-hint')).toBeVisible();
  await apparaatForm.getByRole('button', { name: 'Annuleren' }).click();
  await expect(apparaatForm).toBeHidden();
});

test('wand toevoegen opent een modal en annuleren laat de pagina ongewijzigd', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();

  await expect(page.getByTestId('nieuwe-wand-naam-input')).toBeHidden();
  await indeling.openWandToevoegenModal();
  await page.getByTestId('nieuwe-wand-naam-input').fill('Achterwand');
  await page.getByTestId('wand-toevoegen-annuleren-button').click();

  await expect(page.getByTestId('wand-toevoegen-modal')).toBeHidden();
  await expect(page.getByTestId('indeling-wand-card')).toHaveCount(0);
  await expect(page.getByText('Begin door een wand toe te voegen.')).toBeVisible();
});

test('stap 2 toont maar één actieve wandwerkruimte tegelijk', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegWandToe('Linkerwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast achter',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.voegKastToeAanWand('Linkerwand', {
    naam: 'Onderkast links',
    breedte: 500,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.gaNaarPanelen();

  await panelen.expectLoaded();
  await panelen.openWandWerkruimte('Achterwand');
  await panelen.expectActieveWerkruimte('Achterwand');

  await panelen.openWandWerkruimte('Linkerwand');
  await panelen.expectActieveWerkruimte('Linkerwand');
});

test('stap 2 sluit de actieve wand bij een tweede klik op dezelfde wand', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast toggle',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.gaNaarPanelen();

  await panelen.expectLoaded();
  await panelen.openWandWerkruimte('Achterwand');
  await panelen.sluitActieveWand('Achterwand');
  await expect(page.getByTestId('paneel-editor-weergave')).toBeVisible();
  await expect(page.getByText('Open eerst één wand')).toBeVisible();
});

test('stap 2 scheidt editor en review expliciet', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast review',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.gaNaarPanelen();

  await panelen.expectLoaded();
  await panelen.selecteerEersteKastOpWand('Achterwand');
  await panelen.voegPaneelToe();
  await panelen.openReviewWeergave();
  await panelen.bewerkEerstePaneelInReview();
  await panelen.expectActieveWerkruimte('Achterwand');
});

test('stap 2 gebruikt de reviewtab zonder extra review-banner', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast reviewtab',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.gaNaarPanelen();

  await panelen.expectLoaded();
  await panelen.selecteerEersteKastOpWand('Achterwand');
  await panelen.voegPaneelToe();
  await expect(page.getByTestId('paneel-review-teaser')).toHaveCount(0);
  await panelen.openReviewWeergave();
});

test('stap 2 toont geen staphulp- of begrippenknoppen meer', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast hulploos',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.gaNaarPanelen();

  const staphulpKnop = page.getByRole('button', { name: 'Staphulp' });
  const begrippenKnop = page.getByRole('button', { name: 'Begrippen' });

  await panelen.expectLoaded();
  await expect(staphulpKnop).toHaveCount(0);
  await expect(begrippenKnop).toHaveCount(0);

  await panelen.openWandWerkruimte('Achterwand');
  await expect(staphulpKnop).toHaveCount(0);
  await expect(begrippenKnop).toHaveCount(0);

  await panelen.selecteerEersteKastOpWand('Achterwand');
  await panelen.voegPaneelToe();
  await panelen.openReviewWeergave();
  await expect(staphulpKnop).toHaveCount(0);
  await expect(begrippenKnop).toHaveCount(0);
});

test('focuskaart toont geen extra toelichting onder open eerst één wand', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast focus',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.gaNaarPanelen();

  await panelen.expectLoaded();
  await expect(page.getByText('Open eerst één wand')).toBeVisible();
  await expect(page.getByText('Begin met één wand. Het overzicht wordt pas nuttig zodra er panelen zijn.')).toHaveCount(0);
});

test.describe('mobiele paneel-editor', () => {
  test.use({ viewport: { width: 390, height: 844 } });

  test('stap 2 laat op mobiel eerst kastselectie toe voordat de editor opent', async ({ page }) => {
    const indeling = new IndelingPage(page);
    const panelen = new PanelenPage(page);

    await indeling.goto();
    await indeling.voegWandToe('Achterwand');
    await indeling.voegKastToeAanWand('Achterwand', {
      naam: 'Onderkast mobiel',
      breedte: 600,
      hoogte: 720,
      diepte: 560,
    });
    await indeling.gaNaarPanelen();

    await panelen.expectLoaded();
    await panelen.openWandWerkruimte('Achterwand');
    await expect(page.getByTestId('paneel-editor-drawer')).toHaveCount(0);
    await panelen.selecteerEersteKastOpWand('Achterwand');
  });

  test('stap 2 schuift secundaire uitleg op mobiel onder de actieve werkruimte', async ({ page }) => {
    const indeling = new IndelingPage(page);
    const panelen = new PanelenPage(page);

    await indeling.goto();
    await indeling.voegWandToe('Achterwand');
    await indeling.voegKastToeAanWand('Achterwand', {
      naam: 'Onderkast mobiel',
      breedte: 600,
      hoogte: 720,
      diepte: 560,
    });
    await indeling.gaNaarPanelen();

    await panelen.expectLoaded();
    await panelen.openWandWerkruimte('Achterwand');
    await expect(page.getByTestId('paneel-stap-intro-compact')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Wandenoverzicht' })).toHaveCount(0);

    const actieveWerkruimte = page.getByTestId('paneel-actieve-wand-werkruimte');
    const terminologieBlok = page.getByTestId('paneel-terminologie-blok');
    const werkruimteBox = await actieveWerkruimte.boundingBox();
    const terminologieBox = await terminologieBlok.boundingBox();

    expect(werkruimteBox).not.toBeNull();
    expect(terminologieBox).not.toBeNull();
    expect(terminologieBox!.y).toBeGreaterThan(werkruimteBox!.y + werkruimteBox!.height - 1);
  });
});

test('stap 3 toont verificatie als taaklijst per wand', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);
  const verificatie = new VerificatiePage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegWandToe('Linkerwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast achter',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.voegKastToeAanWand('Linkerwand', {
    naam: 'Onderkast links',
    breedte: 500,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.gaNaarPanelen();

  await panelen.expectLoaded();
  await panelen.selecteerEersteKastOpWand('Achterwand');
  await panelen.voegPaneelToe();
  await panelen.selecteerEersteKastOpWand('Linkerwand');
  await panelen.voegPaneelToe();
  await panelen.gaNaarVerificatie();

  await verificatie.expectLoaded();
  await verificatie.expectTaaklijst();
  await verificatie.expectTaakgroep('Achterwand');
  await verificatie.expectTaakgroep('Linkerwand');
  await verificatie.openControleVoorWand('Linkerwand');
});

test('verificatie-afronding toont bestellijst als primaire vervolgstap', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);
  const verificatie = new VerificatiePage(page);
  const bestellijst = new BestellijstPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast verificatie',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.gaNaarPanelen();

  await panelen.expectLoaded();
  await panelen.selecteerEersteKastOpWand('Achterwand');
  await panelen.voegPaneelToe();
  await panelen.gaNaarVerificatie();

  await verificatie.expectLoaded();
  await verificatie.startVerificatie();
  await verificatie.rondHuidigeVerificatieAf();
  await expect(page.getByTestId('verificatie-completion-primary-button')).toHaveText(/Ga naar bestellijst/);
  await expect(page.getByTestId('verificatie-completion-primary-button')).toHaveAttribute('href', 'bestellijst');
  await verificatie.gaNaarBestellijstViaAfronding();

  await bestellijst.expectLoaded();
});

test('bestellijst export loopt via een aparte previewflow', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);
  const verificatie = new VerificatiePage(page);
  const bestellijst = new BestellijstPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast exportflow',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.gaNaarPanelen();

  await panelen.expectLoaded();
  await panelen.selecteerEersteKastOpWand('Achterwand');
  await panelen.voegPaneelToe();
  await panelen.gaNaarVerificatie();

  await verificatie.expectLoaded();
  await verificatie.startVerificatie();
  await verificatie.gaNaarBestellijst();

  await bestellijst.expectLoaded();
  await bestellijst.openExportFlow();
  await bestellijst.selecteerExportType('excel');
  await bestellijst.gaNaarExportPreview();
  await bestellijst.openExportMateriaalInstellingen();
  await expect(page.getByTestId('bestellijst-paneeltype-hint')).toBeVisible();
  await expect(page.getByTestId('bestellijst-dikte-hint')).toBeVisible();
  await page.getByTestId('bestellijst-paneeltype-input').fill('MDF wit');
  await page.getByTestId('bestellijst-dikte-input').fill('19');
  await expect(page.getByTestId('bestellijst-paneeltype-hint')).toBeVisible();
  await expect(page.getByTestId('bestellijst-dikte-hint')).toBeVisible();
  await bestellijst.expectExportPreview('Excel alleen lijst');
  await bestellijst.gaNaarExportBevestiging();
  await bestellijst.expectExportBevestiging('Excel alleen lijst');
});

test('zaagplan kan wisselen tussen alle platen en een plaat focus', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);
  const verificatie = new VerificatiePage(page);
  const bestellijst = new BestellijstPage(page);
  const zaagplan = new ZaagplanPage(page);

  await indeling.goto();

  const wandNamen = ['Achterwand', 'Linkerwand', 'Rechterwand'];
  for (const wandNaam of wandNamen) {
    await indeling.voegWandToe(wandNaam);
    await indeling.voegKastToeAanWand(wandNaam, {
      naam: `Paneelkast ${wandNaam}`,
      breedte: 1400,
      hoogte: 1600,
      diepte: 560,
    });
  }

  await indeling.gaNaarPanelen();
  await panelen.expectLoaded();

  for (const wandNaam of wandNamen) {
    await panelen.selecteerEersteKastOpWand(wandNaam);
    await panelen.voegPaneelToe();
  }

  await panelen.gaNaarVerificatie();
  await verificatie.expectLoaded();
  await verificatie.startVerificatie();
  await verificatie.gaNaarBestellijst();

  await bestellijst.expectLoaded();
  await bestellijst.gaNaarZaagplan();

  await zaagplan.expectLoaded();
  await zaagplan.expectGeavanceerdeInstellingenGesloten();
  await zaagplan.expectAllePlatenWeergave(3);
  await zaagplan.kiesEenPlaatTegelijk();
  await zaagplan.expectPlaatFocus(1);
  await zaagplan.gaNaarVolgendePlaat();
  await zaagplan.expectPlaatFocus(2);
});

test('zaagplan-waarschuwing noemt directe vervolgstappen bij te kleine plaat', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);
  const verificatie = new VerificatiePage(page);
  const bestellijst = new BestellijstPage(page);
  const zaagplan = new ZaagplanPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Hoge kast zaagplan',
    breedte: 600,
    hoogte: 2100,
    diepte: 600,
  });
  await indeling.gaNaarPanelen();

  await panelen.expectLoaded();
  await panelen.selecteerEersteKastOpWand('Achterwand');
  await panelen.voegPaneelToe();
  await panelen.gaNaarVerificatie();

  await verificatie.expectLoaded();
  await verificatie.startVerificatie();
  await verificatie.gaNaarBestellijst();

  await bestellijst.expectLoaded();
  await bestellijst.gaNaarZaagplan();

  await zaagplan.expectLoaded();
  await page.getByTestId('zaagplan-plaatbreedte-input').fill('1000');
  await page.getByTestId('zaagplan-plaatbreedte-input').press('Tab');
  await page.getByTestId('zaagplan-plaathoogte-input').fill('1000');
  await page.getByTestId('zaagplan-plaathoogte-input').press('Tab');

  const waarschuwing = page.getByTestId('zaagplan-niet-geplaatst-waarschuwing');
  await expect(waarschuwing).toBeVisible();
  await expect(waarschuwing).toContainText('Kies een grotere plaat');
  await expect(waarschuwing).toContainText('Terug naar bestellijst');
});

test('hoofdflow van indeling tot export blijft werken', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);
  const verificatie = new VerificatiePage(page);
  const bestellijst = new BestellijstPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast spoelbak',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.gaNaarPanelen();

  await panelen.expectLoaded();
  await panelen.selecteerEersteKastOpWand('Achterwand');
  await panelen.voegPaneelToe();
  await panelen.gaNaarVerificatie();

  await verificatie.expectLoaded();
  await verificatie.startVerificatie();
  await verificatie.gaNaarBestellijst();

  await bestellijst.expectLoaded();
  const download = await bestellijst.exporteerExcel();
  expect(download.suggestedFilename()).toContain('bestellijst');
});
