import { expect, test, type Locator, type TestInfo } from '@playwright/test';
import { IndelingPage } from '../pages/IndelingPage';

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

test('plank behoudt hetzelfde gatnummer tussen drag-preview en drop', async ({ page }, testInfo) => {
  const indeling = new IndelingPage(page);
  const wandNaam = 'Reprowand';

  await indeling.goto();
  await indeling.voegWandToe(wandNaam);
  await indeling.voegKastToeAanWand(wandNaam, {
    naam: 'Hoge kast repro',
    breedte: 600,
    hoogte: 2140,
    diepte: 560,
  });
  await indeling.voegPlankToeAanEersteKast(wandNaam, 0.68);

  const werkruimte = page.locator(
    `[data-testid="actieve-wand-werkruimte"][data-wand-naam="${wandNaam}"]`,
  );
  const plank = indeling.eersteWandPlank(wandNaam);
  const plankRect = plank.locator('rect').first();
  const plankLabel = indeling.eersteWandPlankLabel(wandNaam);
  const kastRect = indeling.eersteWandKast(wandNaam).locator('rect').first();

  await expect(plankLabel).toContainText('gat');
  await screenshotVanLocator(testInfo, werkruimte, 'plank-voor-drag');

  for (const [index, relativeY] of [0.18, 0.41, 0.79].entries()) {
    const plankBox = await boundingBoxOf(plankRect, 'Plank');
    const kastBox = await boundingBoxOf(kastRect, 'Kast');
    const startX = plankBox.x + plankBox.width / 2;
    const startY = plankBox.y + plankBox.height / 2;
    const targetY = kastBox.y + kastBox.height * relativeY;

    await page.mouse.move(startX, startY);
    await page.mouse.down();
    await page.mouse.move(startX, targetY, { steps: 18 });

    const previewLabel = (await plankLabel.textContent())?.trim() ?? '';
    expect(previewLabel).toMatch(/gat \d+/);
    await screenshotVanLocator(testInfo, werkruimte, `plank-drag-preview-${index + 1}`);

    await page.mouse.up();

    await expect
      .poll(async () => ((await plankLabel.textContent()) ?? '').trim(), {
        message: `Planklabel na drop moet overeenkomen met preview ${index + 1}`,
      })
      .toBe(previewLabel);

    await screenshotVanLocator(testInfo, werkruimte, `plank-na-drop-${index + 1}`);
  }
});
