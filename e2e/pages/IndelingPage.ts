import { expect, type Locator, type Page } from '@playwright/test';

export type KastGegevens = {
  naam: string;
  breedte: number;
  hoogte: number;
  diepte: number;
};

export class IndelingPage {
  constructor(private readonly page: Page) {}

  private wandCard(wandNaam: string): Locator {
    return this.page.locator(`[data-testid="indeling-wand-card"][data-wand-naam="${wandNaam}"]`);
  }

  private actieveWerkruimte(wandNaam: string): Locator {
    return this.page.locator(
      `[data-testid="actieve-wand-werkruimte"][data-wand-naam="${wandNaam}"]`,
    );
  }

  async goto() {
    await this.page.goto('/kasten');
    await expect(this.page.getByRole('heading', { name: 'Stap 1: Indeling' })).toBeVisible();
  }

  async voegWandToe(naam: string) {
    await this.page.getByTestId('nieuwe-wand-naam-input').fill(naam);
    await this.page.getByTestId('wand-toevoegen-button').click();
    await expect(this.wandCard(naam)).toBeVisible();
  }

  async openWandWerkruimte(wandNaam: string) {
    const wand = this.wandCard(wandNaam);
    const openKnop = wand.getByTestId('open-wand-workspace-button');

    if (await openKnop.isEnabled()) {
      await openKnop.click();
    }

    await this.expectActieveWerkruimte(wandNaam);
  }

  async expectActieveWerkruimte(wandNaam: string) {
    await expect(this.page.getByTestId('actieve-wand-werkruimte')).toHaveCount(1);
    await expect(this.actieveWerkruimte(wandNaam)).toBeVisible();
  }

  async voegKastToeAanWand(wandNaam: string, kast: KastGegevens) {
    await this.openWandWerkruimte(wandNaam);

    const werkruimte = this.actieveWerkruimte(wandNaam);
    await werkruimte.getByTestId('open-kast-form-button').click();

    const formulier = this.page.getByTestId('kast-form');
    await expect(formulier).toBeVisible();

    await this.page.getByTestId('kast-naam-input').fill(kast.naam);
    await this.page.getByTestId('kast-breedte-input').fill(kast.breedte.toString());
    await this.page.getByTestId('kast-hoogte-input').fill(kast.hoogte.toString());
    await this.page.getByTestId('kast-diepte-input').fill(kast.diepte.toString());
    await this.page.getByTestId('kast-opslaan-button').click();

    await expect(formulier).toBeHidden();
    await expect(werkruimte).toContainText(kast.naam);
  }

  async gaNaarPanelen() {
    const knop = this.page.getByTestId('stap-navigatie-volgende');
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page).toHaveURL(/\/panelen$/);
  }
}
