import { expect, test, type Locator, type Page, type TestInfo } from '@playwright/test';
import { IndelingPage } from '../pages/IndelingPage';
import { PanelenPage } from '../pages/PanelenPage';
import { VerificatiePage } from '../pages/VerificatiePage';

const misalignedVerificationUrl =
  '/verificatie?s=v4.fVS7ctswEPwVDapk5gq8SapMkyLRpEuTUQFHSKJIJjUiJdvj8b_78CIBUnLDIXG7e3sLgK_kStYCyBNZ_3olLVmTzeVyJkD-kTWvKAVywAoFBhwESFCgoYIami0QEyrbNwjMb_ZysO3qalrzB59meDJ9b6MYo0mMcWACmBx5383O9oh6wJbSoUo4tsAe2M99BcbXc9cNKxaVG6b8AEwDsTgNB_KcpF68FJCTU1Ia-3IpoOECNBUgZDW5P9p9GxTFTBD5uSCnnCbWl-O-3cW4aixHVgEXcoQH4_y-8cIvrxSoKgvYWUzsuUnmu7J6NMlUYTLxZjYnQmZzY3f7y2MKWKuiE-6fI87CFTSL0vTDfxvbNVO7KctIrvTYMswmPtqANJgq008kXk-kCS30GIM7Z34mPGiMx4Pmx8_i8OthsJK3-nToTnv7-WP-gvyj3dnzqj919vhgDsFpzin2PZF-zi5PTlh6i3fnpqWIjrc1WrraeNBVg5G5qYPtbDs25m9rh3PngAMCwra4hKchBcJRGMso_BufEgR-91hyMm5FAQ8Inla0PyXurYYKa2hch-vk1txvJUczPsKZ0y5qMixEb-G43yx4u_IegFVxejHawB_OHel5YSFdAG5J0zjGQnpeWEgXgFvS4BA-_u3bOw';

const hogeKastNaam = 'Groot snap';
const kleineKastNaam = 'Klein snap';
const hogeKastHoogteMm = 1915;
const kleineKastHoogteMm = 315;
const stackFloorMm = hogeKastHoogteMm;
const buitenSnapOffsetMm = -40;
const buitenSnapFloorMm = 1960;

type StackScenario = {
  indeling: IndelingPage;
  wandNaam: string;
  werkruimte: Locator;
};

type DragDoel = {
  pixelsPerMm: number;
  targetCenterY: number;
};

async function screenshotVanLocator(testInfo: TestInfo, locator: Locator, name: string) {
  await testInfo.attach(name, {
    body: await locator.screenshot(),
    contentType: 'image/png',
  });
}

async function boundingBoxOf(locator: Locator, omschrijving: string) {
  const box = await locator.boundingBox();
  expect(box, `${omschrijving} heeft geen bounding box`).not.toBeNull();
  return box!;
}

function centerX(box: { x: number; width: number }) {
  return box.x + box.width / 2;
}

function centerY(box: { y: number; height: number }) {
  return box.y + box.height / 2;
}

async function maakStackScenario(page: Page, wandNaam: string) {
  const indeling = new IndelingPage(page);

  await indeling.goto();
  await indeling.voegWandToe(wandNaam);
  await indeling.voegKastToeAanWand(wandNaam, {
    naam: hogeKastNaam,
    breedte: 600,
    hoogte: hogeKastHoogteMm,
    diepte: 560,
  });
  await indeling.voegKastToeAanWand(wandNaam, {
    naam: kleineKastNaam,
    breedte: 600,
    hoogte: kleineKastHoogteMm,
    diepte: 560,
  });
  await indeling.openWandWerkruimte(wandNaam);

  return {
    indeling,
    wandNaam,
    werkruimte: indeling.werkruimte(wandNaam),
  } satisfies StackScenario;
}

async function leesKastPositie(scenario: StackScenario, kastNaam: string) {
  return {
    x: await scenario.indeling.leesKastXMm(scenario.wandNaam, kastNaam),
    floor: await scenario.indeling.leesKastFloorMm(scenario.wandNaam, kastNaam),
  };
}

async function sleepKastNaarStackDoel(
  page: Page,
  scenario: StackScenario,
  offsetVanafStackMm: number,
  steps = 24,
) {
  const groteRect = scenario.indeling.wandKastRect(scenario.wandNaam, hogeKastNaam);
  const kleineRect = scenario.indeling.wandKastRect(scenario.wandNaam, kleineKastNaam);
  await kleineRect.scrollIntoViewIfNeeded();
  const grootBox = await boundingBoxOf(groteRect, 'Grote kast');
  const kleinBox = await boundingBoxOf(kleineRect, 'Kleine kast');
  const pixelsPerMm = kleinBox.height / kleineKastHoogteMm;
  const targetCenterX = grootBox.x + kleinBox.width / 2;
  const stackedCenterY = grootBox.y - kleinBox.height / 2;
  const targetCenterY = stackedCenterY + offsetVanafStackMm * pixelsPerMm;

  await page.mouse.move(centerX(kleinBox), centerY(kleinBox));
  await page.mouse.down();
  await page.mouse.move(targetCenterX, targetCenterY, { steps });
  await page.mouse.up();

  return {
    pixelsPerMm,
    targetCenterY,
  } satisfies DragDoel;
}

async function expectKleineKastPositie(scenario: StackScenario, x: number, floor: number) {
  await expect.poll(async () => scenario.indeling.leesKastXMm(scenario.wandNaam, kleineKastNaam)).toBe(x);
  await expect.poll(async () => scenario.indeling.leesKastFloorMm(scenario.wandNaam, kleineKastNaam)).toBe(floor);
}

test.describe('kastsnapping regressies', () => {
  test('kastdrag snapt binnen 25 mm naar de stapelpositie', async ({ page }, testInfo) => {
    const scenario = await maakStackScenario(page, 'Snapwand drag');

    await scenario.werkruimte.scrollIntoViewIfNeeded();
    expect(await leesKastPositie(scenario, hogeKastNaam)).toEqual({ x: 0, floor: 0 });
    expect(await leesKastPositie(scenario, kleineKastNaam)).toEqual({ x: 600, floor: 0 });
    await screenshotVanLocator(testInfo, scenario.werkruimte, 'kast-snapping-start');

    await sleepKastNaarStackDoel(page, scenario, 24);

    await expectKleineKastPositie(scenario, 0, stackFloorMm);
    await screenshotVanLocator(testInfo, scenario.werkruimte, 'kast-snapping-binnen-25mm');
  });

  test('kastdrag buiten 25 mm blijft op het raster in plaats van alsnog te snappen', async ({ page }, testInfo) => {
    const scenario = await maakStackScenario(page, 'Snapwand drempel');

    await scenario.werkruimte.scrollIntoViewIfNeeded();
    const { pixelsPerMm, targetCenterY } = await sleepKastNaarStackDoel(page, scenario, buitenSnapOffsetMm);

    await expectKleineKastPositie(scenario, 0, buitenSnapFloorMm);

    const kleineBox = await boundingBoxOf(
      scenario.indeling.wandKastRect(scenario.wandNaam, kleineKastNaam),
      'Kleine kast na buiten-drempel drag',
    );
    expect(Math.abs(centerY(kleineBox) - targetCenterY)).toBeLessThanOrEqual(Math.max(2, 10 * pixelsPerMm));
    await screenshotVanLocator(testInfo, scenario.werkruimte, 'kast-snapping-buiten-25mm');
  });

  test('verificatie toont geen uitlijnwaarschuwing voor een netjes gesnapte stapeling', async ({ page }, testInfo) => {
    const scenario = await maakStackScenario(page, 'Snapwand uitgelijnd');
    const panelen = new PanelenPage(page);
    const verificatie = new VerificatiePage(page);

    await scenario.werkruimte.scrollIntoViewIfNeeded();
    await sleepKastNaarStackDoel(page, scenario, 24);
    await expectKleineKastPositie(scenario, 0, stackFloorMm);
    await screenshotVanLocator(testInfo, scenario.werkruimte, 'kast-uitgelijnd-voor-verificatie');

    await scenario.indeling.gaNaarPanelen();
    await panelen.expectLoaded();
    await panelen.selecteerEersteKastOpWand(scenario.wandNaam);
    await panelen.voegPaneelToe();
    await panelen.gaNaarVerificatie();

    await verificatie.expectLoaded();
    await verificatie.expectTaaklijst();
    await verificatie.expectGeenUitlijnWaarschuwing();
  });

  test('verificatie toont een waarschuwing bij verdacht kleine uitlijnverschillen', async ({ page }, testInfo) => {
    const verificatie = new VerificatiePage(page);

    await page.goto(misalignedVerificationUrl);
    await verificatie.expectLoaded();
    await verificatie.expectTaaklijst();
    await verificatie.expectUitlijnWaarschuwing('Muur');
    await verificatie.expectUitlijnTekst('Groot 1');
    await verificatie.expectUitlijnTekst('Klein');
    await verificatie.expectUitlijnTekst('5 mm');
    await screenshotVanLocator(
      testInfo,
      page.getByTestId('verificatie-uitlijn-waarschuwing'),
      'verificatie-uitlijn-waarschuwing',
    );
  });
});
