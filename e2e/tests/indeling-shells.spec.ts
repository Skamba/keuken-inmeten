import { expect, test } from '@playwright/test';
import { IndelingPage } from '../pages/IndelingPage';
import { PanelenPage } from '../pages/PanelenPage';

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

test('stap 1 toont maar één actieve wandwerkruimte tegelijk', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegWandToe('Linkerwand');

  await expect(page.getByTestId('nav-indeling-wand-link')).toHaveCount(2);
  await expect(page.getByRole('heading', { name: 'Wandenoverzicht' })).toHaveCount(0);

  await indeling.openWandWerkruimte('Achterwand');
  await indeling.expectActieveWerkruimte('Achterwand');
  await expect(page.getByTestId('indeling-overige-wanden-summary')).toHaveCount(0);

  await indeling.openWandWerkruimte('Linkerwand');
  await indeling.expectActieveWerkruimte('Linkerwand');
});

test('stap 1 verplaatst wandnavigatie naar de navbar onder indeling', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegWandToe('Linkerwand');

  await expect(page.getByRole('heading', { name: 'Wandenoverzicht' })).toHaveCount(0);
  await expect(page.getByTestId('nav-indeling-wanden')).toBeVisible();
  await expect(page.getByTestId('nav-indeling-wand-link')).toHaveCount(2);
  await expect(page.locator('[data-testid="nav-indeling-wand-link"][data-wand-naam="Achterwand"]')).toBeVisible();
  await expect(page.locator('[data-testid="nav-indeling-wand-link"][data-wand-naam="Linkerwand"]')).toBeVisible();
});

test('stap 1 toont geen aparte andere-wanden schakelaar meer in de werkruimtekolom', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();
  await indeling.voegWandToe('Achterwand');
  await indeling.voegWandToe('Linkerwand');

  await indeling.openWandWerkruimte('Achterwand');
  await expect(page.getByTestId('indeling-overige-wanden-summary')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Andere wanden' })).toHaveCount(0);
  await expect(page.locator('[data-testid="nav-indeling-wand-link"][data-wand-naam="Linkerwand"]')).toBeVisible();
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
