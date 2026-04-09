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
    if (!(await this.actieveWerkruimte(wandNaam).isVisible())) {
      const wand = this.wandCard(wandNaam);
      const openKnop = wand.getByTestId('open-paneel-wand-button');
      if (!(await openKnop.isVisible())) {
        const overigeWandenSamenvatting = this.page.getByTestId('paneel-overige-wanden-summary');
        if (await overigeWandenSamenvatting.isVisible()) {
          await overigeWandenSamenvatting.click();
          await expect(openKnop).toBeVisible();
        }
      }
      await openKnop.click();
    }

    await this.expectEditorWeergave();
    await this.expectActieveWerkruimte(wandNaam);
  }

  async expectEditorWeergave() {
    await expect(this.page.getByTestId('paneel-editor-weergave')).toBeVisible();
    await expect(this.page.getByTestId('paneel-review-weergave')).toHaveCount(0);
  }

  async openReviewWeergave() {
    await this.page.getByTestId('paneel-review-weergave-tab').click();
    await this.expectReviewWeergave();
  }

  async expectReviewWeergave() {
    await expect(this.page.getByTestId('paneel-review-weergave')).toBeVisible();
    await expect(this.page.getByTestId('paneel-editor-weergave')).toHaveCount(0);
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
    await expect(this.page.getByTestId('paneel-editor-drawer')).toHaveCount(0);
    await this.expectEditorWeergave();
    await expect(this.page.getByTestId('paneel-review-teaser')).toBeVisible();
  }

  async bewerkEerstePaneelInReview() {
    await this.page
      .getByTestId('paneel-review-weergave')
      .getByRole('button', { name: 'Bewerk' })
      .first()
      .click();
    await this.expectEditorWeergave();
    await expect(this.page.getByTestId('paneel-editor-drawer')).toBeVisible();
  }

  async gaNaarVerificatie() {
    const knop = this.page.getByTestId('stap-navigatie-volgende');
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page).toHaveURL(/\/verificatie$/);
  }
}
