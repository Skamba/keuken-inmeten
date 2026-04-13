import { expect, test } from '@playwright/test';
import { IndelingPage } from '../pages/IndelingPage';
import { PanelenPage } from '../pages/PanelenPage';
import { VerificatiePage } from '../pages/VerificatiePage';

test('verificatie bewaart afgevinkte checks bij navigeren en herladen', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);
  const verificatie = new VerificatiePage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegKastToeAanWand('Achterwand', {
    naam: 'Onderkast controle',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });

  await indeling.gaNaarPanelen();
  await panelen.expectLoaded();
  await panelen.selecteerEersteKastOpWand('Achterwand');
  await page.getByTestId('paneel-type-button-BlindPaneel').click();
  await panelen.voegPaneelToe();
  await panelen.gaNaarVerificatie();

  await verificatie.expectLoaded();
  await verificatie.openControleVoorWand('Achterwand');
  await verificatie.vinkMatenCheckAf();
  await verificatie.wachtTotAutomatischOpgeslagen();
  await verificatie.terugNaarTaaklijst();
  await verificatie.expectTaakItemKlaar('Achterwand', 'Onderkast controle');

  await page.goto('/panelen');
  await panelen.expectLoaded();

  await page.goto('/verificatie');
  await verificatie.expectLoaded();
  await verificatie.expectTaaklijst();
  await verificatie.expectTaakItemKlaar('Achterwand', 'Onderkast controle');

  await page.reload();
  await verificatie.expectLoaded();
  await verificatie.expectTaaklijst();
  await verificatie.expectTaakItemKlaar('Achterwand', 'Onderkast controle');

  await verificatie.openControleVoorWand('Achterwand');
  await verificatie.expectMatenCheckAfgevinkt();
});

test('verificatiestatus zit ook in de sharing link', async ({ page, context, browser }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);
  const verificatie = new VerificatiePage(page);

  await context.grantPermissions(['clipboard-read', 'clipboard-write']);
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'share', { value: undefined, configurable: true });
    Object.defineProperty(navigator, 'canShare', { value: undefined, configurable: true });
  });

  await indeling.goto();
  await indeling.voegWandToe('Sharewand');
  await indeling.voegKastToeAanWand('Sharewand', {
    naam: 'Onderkast share',
    breedte: 600,
    hoogte: 720,
    diepte: 560,
  });

  await indeling.gaNaarPanelen();
  await panelen.expectLoaded();
  await panelen.selecteerEersteKastOpWand('Sharewand');
  await page.getByTestId('paneel-type-button-BlindPaneel').click();
  await panelen.voegPaneelToe();
  await panelen.gaNaarVerificatie();

  await verificatie.expectLoaded();
  await verificatie.openControleVoorWand('Sharewand');
  await verificatie.vinkMatenCheckAf();
  await verificatie.rondHuidigeVerificatieAf();

  await page.getByTestId('verificatie-share-button').click();
  await expect(page.getByTestId('actie-feedback-toast')).toContainText('De deellink is gekopieerd.');

  let deelUrl = '';
  await expect.poll(async () => {
    deelUrl = await page.evaluate(() => navigator.clipboard.readText());
    return deelUrl;
  }).toMatch(/\/verificatie(?:\?wand=[^&]+)?(?:&|\?)s=v4\./);

  const schoneContext = await browser.newContext();
  try {
    const gedeeldePagina = await schoneContext.newPage();
    const gedeeldeVerificatie = new VerificatiePage(gedeeldePagina);

    await gedeeldePagina.goto(deelUrl);
    await gedeeldeVerificatie.expectLoaded();
    await gedeeldeVerificatie.expectTaaklijst();
    await gedeeldeVerificatie.expectTaakItemKlaar('Sharewand', 'Onderkast share');
    await gedeeldeVerificatie.openControleVoorWand('Sharewand');
    await gedeeldeVerificatie.expectMatenCheckAfgevinkt();
  } finally {
    await schoneContext.close();
  }
});
