import { expect, test } from '@playwright/test';
import { IndelingPage } from '../pages/IndelingPage';
import { PanelenPage } from '../pages/PanelenPage';

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

  await panelen.expectOverzichtVoorWand('Achterwand');
  const groep = page.locator('[data-testid="paneel-review-groep"][data-wand-naam="Achterwand"]');
  await expect(groep.locator('.paneel-review-item')).toHaveCount(3);

  await panelen.bewerkEerstePaneelInOverzicht('Achterwand');
  await expect(page.getByTestId('paneel-editor-drawer')).toBeVisible();
  await expect(page.getByTestId('paneel-type-button-LadeFront')).toHaveClass(/btn-primary/);
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

test('stap 2 combineert editor en overzicht zonder viewtoggle', async ({ page }) => {
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
  await expect(page.getByTestId('paneel-editor-weergave-tab')).toHaveCount(0);
  await expect(page.getByTestId('paneel-review-weergave-tab')).toHaveCount(0);
  await panelen.expectOverzichtVoorWand('Achterwand');
  await panelen.bewerkEerstePaneelInOverzicht('Achterwand');
  await panelen.expectActieveWerkruimte('Achterwand');
});

test('stap 2 toont het wandoverzicht direct in dezelfde werklaag', async ({ page }) => {
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
  await expect(page.getByTestId('paneel-editor-weergave-tab')).toHaveCount(0);
  await expect(page.getByTestId('paneel-review-weergave-tab')).toHaveCount(0);
  await panelen.expectOverzichtVoorWand('Achterwand');
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
  await panelen.expectOverzichtVoorWand('Achterwand');
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

test('stap 2 kan de paneelwerkbank fullscreen openen en weer sluiten', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast fullscreen',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });
  await indeling.gaNaarPanelen();

  await panelen.expectLoaded();
  await panelen.openWandWerkruimte('Achterwand');
  await expect(page.getByTestId('paneel-werkbank-visualisatie-fullscreen-toggle')).toBeVisible();
  await page.getByTestId('paneel-werkbank-visualisatie-fullscreen-toggle').click();

  const shell = page.getByTestId('paneel-werkbank-visualisatie-fullscreen-shell');
  await expect(shell).toBeVisible();
  await expect(shell.getByTestId('paneel-plaats-editor')).toBeVisible();

  await shell.getByTestId('paneel-werkbank-visualisatie-fullscreen-toggle').click();
  await expect(shell).toHaveCount(0);
  await expect(page.getByTestId('paneel-werkbank-visualisatie')).toBeVisible();
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

  test('stap 2 schuift het compacte overzicht op mobiel onder de visualisatie', async ({ page }) => {
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

    const visualisatie = page.getByTestId('paneel-werkbank-visualisatie');
    const overzicht = page.getByTestId('paneel-review-overzicht');
    const terminologieBlok = page.getByTestId('paneel-terminologie-blok');
    const visualisatieBox = await visualisatie.boundingBox();
    const overzichtBox = await overzicht.boundingBox();
    const terminologieBox = await terminologieBlok.boundingBox();

    expect(visualisatieBox).not.toBeNull();
    expect(overzichtBox).not.toBeNull();
    expect(terminologieBox).not.toBeNull();
    expect(overzichtBox!.y).toBeGreaterThan(visualisatieBox!.y + visualisatieBox!.height - 1);
    expect(terminologieBox!.y).toBeGreaterThan(overzichtBox!.y + overzichtBox!.height - 1);
  });
});
