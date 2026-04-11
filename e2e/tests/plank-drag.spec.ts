import { expect, test, type Locator, type Page, type TestInfo } from '@playwright/test';
import { IndelingPage } from '../pages/IndelingPage';

type PlankScenario = {
  indeling: IndelingPage;
  wandNaam: string;
  werkruimte: Locator;
  plank: Locator;
  plankRect: Locator;
  plankLabel: Locator;
  kastRect: Locator;
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

async function leesTekst(locator: Locator) {
  return ((await locator.textContent()) ?? '').trim();
}

async function maakPlankScenario(page: Page, wandNaam: string) {
  const indeling = new IndelingPage(page);

  await indeling.goto();
  await indeling.voegWandToe(wandNaam);
  await indeling.voegKastToeAanWand(wandNaam, {
    naam: 'Hoge kast repro',
    breedte: 600,
    hoogte: 2140,
    diepte: 560,
  });
  await indeling.voegPlankToeAanEersteKast(wandNaam, 0.68);

  return {
    indeling,
    wandNaam,
    werkruimte: indeling.werkruimte(wandNaam),
    plank: indeling.eersteWandPlank(wandNaam),
    plankRect: indeling.eersteWandPlankRect(wandNaam),
    plankLabel: indeling.eersteWandPlankLabel(wandNaam),
    kastRect: indeling.eersteWandKastRect(wandNaam),
  } satisfies PlankScenario;
}

async function startDragVanPlank(page: Page, scenario: PlankScenario) {
  const plankBox = await boundingBoxOf(scenario.plankRect, 'Plank');
  const startX = centerX(plankBox);
  const startY = centerY(plankBox);

  await page.mouse.move(startX, startY);
  await page.mouse.down();

  return { startX, startY };
}

async function sleepPlankNaarRelativeY(
  page: Page,
  scenario: PlankScenario,
  startX: number,
  relativeY: number,
  steps = 18,
) {
  const kastBox = await boundingBoxOf(scenario.kastRect, 'Kast');
  const targetY = kastBox.y + kastBox.height * relativeY;
  await page.mouse.move(startX, targetY, { steps });

  return { kastBox, targetY };
}

async function expectPlankOnderPointer(
  scenario: PlankScenario,
  targetY: number,
  tolerancePx = 3,
) {
  const plankBox = await boundingBoxOf(scenario.plankRect, 'Plank tijdens drag');
  expect(Math.abs(centerY(plankBox) - targetY)).toBeLessThanOrEqual(tolerancePx);
}

async function wachtTotPlankCommitKlaarIs(scenario: PlankScenario) {
  await expect
    .poll(async () => scenario.plank.getAttribute('transform'), {
      message: 'Planktransform moet verdwijnen nadat Blazor de drop heeft gecommit',
    })
    .toBeNull();
}

test.describe('plankdrag regressies', () => {
  test('plank blijft onder de pointer tijdens drag en toont live het preview-gat', async ({ page }, testInfo) => {
    const scenario = await maakPlankScenario(page, 'Pointerwand');

    await expect(scenario.plankLabel).toContainText('gat');
    await screenshotVanLocator(testInfo, scenario.werkruimte, 'plank-pointer-voor-drag');

    const { startX } = await startDragVanPlank(page, scenario);

    const eersteDoel = await sleepPlankNaarRelativeY(page, scenario, startX, 0.22);
    await expectPlankOnderPointer(scenario, eersteDoel.targetY);
    const eerstePreview = await leesTekst(scenario.plankLabel);
    expect(eerstePreview).toMatch(/gat \d+/);
    await screenshotVanLocator(testInfo, scenario.werkruimte, 'plank-pointer-preview-1');

    const tweedeDoel = await sleepPlankNaarRelativeY(page, scenario, startX, 0.63);
    await expectPlankOnderPointer(scenario, tweedeDoel.targetY);
    const tweedePreview = await leesTekst(scenario.plankLabel);
    expect(tweedePreview).toMatch(/gat \d+/);
    expect(tweedePreview).not.toBe(eerstePreview);
    await screenshotVanLocator(testInfo, scenario.werkruimte, 'plank-pointer-preview-2');

    await page.mouse.up();

    await wachtTotPlankCommitKlaarIs(scenario);
    await expect
      .poll(async () => leesTekst(scenario.plankLabel))
      .toBe(tweedePreview);
  });

  test('plank behoudt hetzelfde gatnummer tussen drag-preview en drop', async ({ page }, testInfo) => {
    const scenario = await maakPlankScenario(page, 'Reprowand');

    await expect(scenario.plankLabel).toContainText('gat');
    await screenshotVanLocator(testInfo, scenario.werkruimte, 'plank-voor-drag');

    for (const [index, relativeY] of [0.18, 0.41, 0.79].entries()) {
      const { startX, startY } = await startDragVanPlank(page, scenario);
      const { targetY } = await sleepPlankNaarRelativeY(page, scenario, startX, relativeY);

      await expectPlankOnderPointer(scenario, targetY);

      const previewLabel = await leesTekst(scenario.plankLabel);
      expect(previewLabel).toMatch(/gat \d+/);
      await screenshotVanLocator(testInfo, scenario.werkruimte, `plank-drag-preview-${index + 1}`);

      await page.mouse.up();

      await wachtTotPlankCommitKlaarIs(scenario);
      await expect
        .poll(async () => leesTekst(scenario.plankLabel), {
          message: `Planklabel na drop moet overeenkomen met preview ${index + 1}`,
        })
        .toBe(previewLabel);

      await screenshotVanLocator(testInfo, scenario.werkruimte, `plank-na-drop-${index + 1}`);

      expect(startY).toBeGreaterThan(0);
    }
  });

  test('plankdrag blijft binnen de kast en stabiel over meerdere herhaalde drags', async ({ page }, testInfo) => {
    const scenario = await maakPlankScenario(page, 'Stabiliteitswand');

    for (const [index, relativeY] of [0.14, 0.86, 0.35, 0.72].entries()) {
      const { startX } = await startDragVanPlank(page, scenario);
      const { kastBox, targetY } = await sleepPlankNaarRelativeY(page, scenario, startX, relativeY, 22);

      await expectPlankOnderPointer(scenario, targetY);

      const plankBox = await boundingBoxOf(scenario.plankRect, 'Plank tijdens stabiliteitsdrag');
      expect(centerY(plankBox)).toBeGreaterThanOrEqual(kastBox.y - 1);
      expect(centerY(plankBox)).toBeLessThanOrEqual(kastBox.y + kastBox.height + 1);

      const previewLabel = await leesTekst(scenario.plankLabel);
      expect(previewLabel).toMatch(/gat \d+/);

      await screenshotVanLocator(testInfo, scenario.werkruimte, `plank-stabiliteit-${index + 1}-preview`);
      await page.mouse.up();

      await wachtTotPlankCommitKlaarIs(scenario);
      await expect
        .poll(async () => leesTekst(scenario.plankLabel), {
          message: `Planklabel moet stabiel blijven na drag ${index + 1}`,
        })
        .toBe(previewLabel);
    }
  });
});
