import { expect, test } from '@playwright/test';
import { BestellijstPage } from '../pages/BestellijstPage';
import { IndelingPage } from '../pages/IndelingPage';
import { PanelenPage } from '../pages/PanelenPage';
import { ZaagplanPage } from '../pages/ZaagplanPage';
import { VerificatiePage } from '../pages/VerificatiePage';

test('panelenoverzicht en verificatie houden wanden met gelijke namen gescheiden', async ({ page }) => {
  const indeling = new IndelingPage(page);
  const panelen = new PanelenPage(page);
  const verificatie = new VerificatiePage(page);

  async function openIndelingWand(wandId: string) {
    await page.locator(`[data-testid="nav-indeling-wand-link"][data-wand-id="${wandId}"]`).click();
    await expect(page.locator(`[data-testid="actieve-wand-werkruimte"][data-wand-id="${wandId}"]`)).toBeVisible();
  }

  async function openPaneelWandViaNavbar(wandId: string) {
    await page.locator(`[data-testid="nav-panelen-wand-link"][data-wand-id="${wandId}"]`).click();
    await expect(page).toHaveURL(new RegExp(`/panelen\\?wand=${wandId}$`));
    await expect(page.locator(`[data-testid="paneel-actieve-wand-werkruimte"][data-wand-id="${wandId}"]`)).toBeVisible();
  }

  async function focusVerificatieWandViaNavbar(wandId: string) {
    await page.locator(`[data-testid="nav-verificatie-wand-link"][data-wand-id="${wandId}"]`).click();
    await expect(page).toHaveURL(new RegExp(`/verificatie\\?wand=${wandId}$`));
    await expect(page.locator(`[data-testid="verificatie-taakgroep"][data-wand-id="${wandId}"]`)).toBeVisible();
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

  const wandLinks = page.locator('[data-testid="nav-indeling-wand-link"][data-wand-naam="Muur"]');
  await expect(wandLinks).toHaveCount(2);
  const eersteWandId = await wandLinks.nth(0).getAttribute('data-wand-id');
  const tweedeWandId = await wandLinks.nth(1).getAttribute('data-wand-id');

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
  await expect(page.getByTestId('nav-panelen-wand-link')).toHaveCount(2);
  await expect(page.getByTestId('nav-indeling-wanden')).toHaveCount(0);
  await expect(page.getByTestId('nav-verificatie-wanden')).toHaveCount(0);

  await openPaneelWandViaNavbar(eersteWandId!);
  await page.locator('[data-testid="paneel-kast"]').first().click();
  await page.getByTestId('paneel-opslaan-button').click();
  await page.getByRole('button', { name: 'Werkruimte sluiten' }).click();

  await openPaneelWandViaNavbar(tweedeWandId!);
  await page.locator('[data-testid="paneel-kast"]').first().click();
  await page.getByTestId('paneel-opslaan-button').click();

  await expect(page.getByTestId('paneel-review-groep')).toHaveCount(1);
  await expect(page.locator(`[data-testid="paneel-review-groep"][data-wand-id="${tweedeWandId}"]`)).toBeVisible();
  await expect(page.locator(`[data-testid="paneel-review-groep"][data-wand-id="${eersteWandId}"]`)).toHaveCount(0);

  await openPaneelWandViaNavbar(eersteWandId!);
  await expect(page.getByTestId('paneel-review-groep')).toHaveCount(1);
  await expect(page.locator(`[data-testid="paneel-review-groep"][data-wand-id="${eersteWandId}"]`)).toBeVisible();
  await expect(page.locator(`[data-testid="paneel-review-groep"][data-wand-id="${tweedeWandId}"]`)).toHaveCount(0);

  await panelen.gaNaarVerificatie();
  await verificatie.expectLoaded();
  await verificatie.expectTaaklijst();
  await expect(page.getByTestId('nav-verificatie-wand-link')).toHaveCount(2);
  await expect(page.getByTestId('nav-indeling-wanden')).toHaveCount(0);
  await expect(page.getByTestId('nav-panelen-wanden')).toHaveCount(0);
  await expect(page.getByTestId('verificatie-taakgroep')).toHaveCount(2);
  await expect(page.locator(`[data-testid="verificatie-taakgroep"][data-wand-id="${eersteWandId}"]`)).toBeVisible();
  await expect(page.locator(`[data-testid="verificatie-taakgroep"][data-wand-id="${tweedeWandId}"]`)).toBeVisible();

  await focusVerificatieWandViaNavbar(tweedeWandId!);
  await expect(page.getByTestId('verificatie-taakgroep')).toHaveCount(1);
  await expect(page.locator(`[data-testid="verificatie-taakgroep"][data-wand-id="${tweedeWandId}"]`)).toBeVisible();
  await expect(page.locator(`[data-testid="verificatie-taakgroep"][data-wand-id="${eersteWandId}"]`)).toHaveCount(0);

  await focusVerificatieWandViaNavbar(eersteWandId!);
  await expect(page.getByTestId('verificatie-taakgroep')).toHaveCount(1);
  await expect(page.locator(`[data-testid="verificatie-taakgroep"][data-wand-id="${eersteWandId}"]`)).toBeVisible();
  await expect(page.locator(`[data-testid="verificatie-taakgroep"][data-wand-id="${tweedeWandId}"]`)).toHaveCount(0);
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

test('hoofdflow van indeling tot excel- en pdf-export blijft werken', async ({ page }) => {
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
  const excelDownload = await bestellijst.exporteerExcel();
  expect(excelDownload.suggestedFilename()).toContain('bestellijst');
  expect(excelDownload.suggestedFilename()).toContain('.xls');

  const pdfDownload = await bestellijst.exporteerPdf();
  expect(pdfDownload.suggestedFilename()).toContain('bestellijst');
  expect(pdfDownload.suggestedFilename()).toContain('.pdf');
});
