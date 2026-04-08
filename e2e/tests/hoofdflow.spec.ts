import { expect, test } from '@playwright/test';
import { BestellijstPage } from '../pages/BestellijstPage';
import { IndelingPage } from '../pages/IndelingPage';
import { PanelenPage } from '../pages/PanelenPage';
import { VerificatiePage } from '../pages/VerificatiePage';

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
  await indeling.gaNaarVolgendeKastFormStap();

  await indeling.expectKastFormStap('Controle');
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
