import { expect, type Page } from '@playwright/test';

export class ZaagplanPage {
  constructor(private readonly page: Page) {}

  async expectLoaded() {
    await expect(this.page.getByRole('heading', { name: 'Stap 5: Zaagplan' })).toBeVisible();
  }

  async expectGeavanceerdeInstellingenGesloten() {
    await expect(this.page.getByTestId('zaagplan-geavanceerde-instellingen')).not.toHaveAttribute('open', '');
  }

  async expectAllePlatenWeergave(aantalPlaten: number) {
    await expect(this.page.getByTestId('zaagplan-alle-platen-weergave')).toBeVisible();
    await expect(this.page.getByTestId('zaagplan-plaat-card')).toHaveCount(aantalPlaten);
  }

  async kiesEenPlaatTegelijk() {
    await this.page.getByTestId('zaagplan-een-plaat-button').click();
    await expect(this.page.getByTestId('zaagplan-een-plaat-weergave')).toBeVisible();
  }

  async expectPlaatFocus(plaatNummer: number) {
    await expect(this.page.getByTestId('zaagplan-focus-toolbar')).toContainText(`Plaat ${plaatNummer}`);
    await expect(
      this.page.locator(`[data-testid="zaagplan-plaat-card"][data-plaat-nummer="${plaatNummer}"]`),
    ).toHaveCount(1);
  }

  async gaNaarVolgendePlaat() {
    await this.page.getByTestId('zaagplan-volgende-plaat').click();
  }
}
