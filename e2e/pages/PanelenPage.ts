import { expect, type Locator, type Page } from '@playwright/test';

export class PanelenPage {
  constructor(private readonly page: Page) {}

  private wandCard(wandNaam: string): Locator {
    return this.page.locator(`[data-testid="paneel-wand-card"][data-wand-naam="${wandNaam}"]`);
  }

  async expectLoaded() {
    await expect(this.page.getByRole('heading', { name: 'Stap 2: Panelen' })).toBeVisible();
  }

  async selecteerEersteKastOpWand(wandNaam: string) {
    const kast = this.wandCard(wandNaam).locator('[data-testid="paneel-kast"]').first();
    await kast.scrollIntoViewIfNeeded();
    await kast.click();
    await expect(this.page.getByTestId('paneel-opslaan-button')).toBeEnabled();
  }

  async voegPaneelToe() {
    await this.page.getByTestId('paneel-opslaan-button').click();
    await expect(this.page.getByText('Paneel 1')).toBeVisible();
  }

  async gaNaarVerificatie() {
    const knop = this.page.getByTestId('stap-navigatie-volgende');
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page).toHaveURL(/\/verificatie$/);
  }
}
