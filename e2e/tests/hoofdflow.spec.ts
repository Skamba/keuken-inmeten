import { expect, test } from '@playwright/test';
import { BestellijstPage } from '../pages/BestellijstPage';
import { IndelingPage } from '../pages/IndelingPage';
import { PanelenPage } from '../pages/PanelenPage';
import { VerificatiePage } from '../pages/VerificatiePage';

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
