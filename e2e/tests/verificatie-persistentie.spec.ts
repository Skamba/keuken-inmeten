import { test } from '@playwright/test';
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
