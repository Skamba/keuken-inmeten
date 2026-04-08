import { expect, type Locator, type Page } from '@playwright/test';

export class PanelenPage {
  constructor(private readonly page: Page) {}

  private wandCard(wandNaam: string): Locator {
    return this.page.locator(`[data-testid="paneel-wand-card"][data-wand-naam="${wandNaam}"]`);
  }

  private actieveWerkruimte(wandNaam: string): Locator {
    return this.page.locator(
      `[data-testid="paneel-actieve-wand-werkruimte"][data-wand-naam="${wandNaam}"]`,
    );
  }

  async expectLoaded() {
    await expect(this.page.getByRole('heading', { name: 'Stap 2: Panelen' })).toBeVisible();
  }

  async openWandWerkruimte(wandNaam: string) {
    const wand = this.wandCard(wandNaam);
    const openKnop = wand.getByTestId('open-paneel-wand-button');

    if (await openKnop.isEnabled()) {
      await openKnop.click();
    }

    await this.expectActieveWerkruimte(wandNaam);
    await expect(this.page.getByTestId('paneel-editor-drawer')).toBeVisible();
  }

  async expectActieveWerkruimte(wandNaam: string) {
    await expect(this.page.getByTestId('paneel-actieve-wand-werkruimte')).toHaveCount(1);
    await expect(this.actieveWerkruimte(wandNaam)).toBeVisible();
  }

  async selecteerEersteKastOpWand(wandNaam: string) {
    await this.openWandWerkruimte(wandNaam);

    const kast = this.actieveWerkruimte(wandNaam).locator('[data-testid="paneel-kast"]').first();
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
