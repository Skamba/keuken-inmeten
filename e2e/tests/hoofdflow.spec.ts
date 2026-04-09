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

test('invoerhulp blijft zichtbaar bij wand-, kast- en apparaatvelden', async ({ page }) => {
  const indeling = new IndelingPage(page);

  await indeling.goto();

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
  await expect(page.getByTestId('bestellijst-paneeltype-hint')).toBeVisible();
  await expect(page.getByTestId('bestellijst-dikte-hint')).toBeVisible();
  await page.getByTestId('bestellijst-paneeltype-input').fill('MDF wit');
  await page.getByTestId('bestellijst-dikte-input').fill('19');
  await expect(page.getByTestId('bestellijst-paneeltype-hint')).toBeVisible();
  await expect(page.getByTestId('bestellijst-dikte-hint')).toBeVisible();
  await bestellijst.selecteerExportType('excel');
  await bestellijst.gaNaarExportPreview();
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
