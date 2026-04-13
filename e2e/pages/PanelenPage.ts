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

  private actieveWandToggle(wandNaam: string): Locator {
    return this.page.locator(
      `[data-testid="toggle-paneel-actieve-wand-button"][data-wand-naam="${wandNaam}"]`,
    );
  }

  private overzichtVoorWand(wandNaam: string): Locator {
    return this.page.locator(
      `[data-testid="paneel-review-overzicht"][data-wand-naam="${wandNaam}"]`,
    );
  }

  overzichtGroepVoorWandId(wandId: string): Locator {
    return this.page.locator(`[data-testid="paneel-review-groep"][data-wand-id="${wandId}"]`);
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
    await this.expectOverzichtVoorWand(wandNaam);
  }

  async expectEditorWeergave() {
    await expect(this.page.getByTestId('paneel-editor-weergave')).toBeVisible();
    await expect(this.page.getByTestId('paneel-editor-weergave-tab')).toHaveCount(0);
    await expect(this.page.getByTestId('paneel-review-weergave-tab')).toHaveCount(0);
  }

  async expectOverzichtVoorWand(wandNaam: string) {
    await expect(this.overzichtVoorWand(wandNaam)).toBeVisible();
  }

  async expectActieveWerkruimte(wandNaam: string) {
    await expect(this.page.getByTestId('paneel-actieve-wand-werkruimte')).toHaveCount(1);
    await expect(this.actieveWerkruimte(wandNaam)).toBeVisible();
  }

  async sluitActieveWand(wandNaam: string) {
    await this.expectActieveWerkruimte(wandNaam);
    await this.actieveWandToggle(wandNaam).click();
    await expect(this.page.getByTestId('paneel-actieve-wand-werkruimte')).toHaveCount(0);
    await expect(this.page.getByTestId('paneel-review-overzicht')).toHaveCount(0);
  }

  async selecteerEersteKastOpWand(wandNaam: string) {
    await this.openWandWerkruimte(wandNaam);

    const kast = this.actieveWerkruimte(wandNaam)
      .locator('[data-testid="paneel-kast"]')
      .locator('rect')
      .first();
    await kast.scrollIntoViewIfNeeded();
    await kast.click();
    await expect(this.page.getByTestId('paneel-opslaan-button')).toBeEnabled();
  }

  async deelGeselecteerdeKastOp(hoogtes: number[]) {
    await expect(this.page.getByTestId('open-paneel-opdelen-modal-button')).toBeEnabled();
    await this.page.getByTestId('open-paneel-opdelen-modal-button').click();
    await expect(this.page.getByTestId('paneel-opdelen-modal')).toBeVisible();

    const aantalKnop = this.page.getByTestId(`paneel-opdelen-aantal-button-${hoogtes.length}`);
    await expect(aantalKnop).toBeEnabled();
    await aantalKnop.click();

    for (let i = 0; i < hoogtes.length; i += 1) {
      await this.page.getByTestId(`paneel-opdelen-hoogte-input-${i}`).fill(`${hoogtes[i]}`);
    }

    await expect(this.page.getByTestId('paneel-opdelen-bevestigen-button')).toBeEnabled();
    await this.page.getByTestId('paneel-opdelen-bevestigen-button').click();
    await expect(this.page.getByTestId('paneel-opdelen-modal')).toHaveCount(0);
    await expect(this.page.getByTestId('paneel-editor-drawer')).toHaveCount(0);
    await this.expectEditorWeergave();
  }

  async voegPaneelToe() {
    await this.page.getByTestId('paneel-opslaan-button').click();
    await expect(this.page.getByTestId('paneel-editor-drawer')).toHaveCount(0);
    await this.expectEditorWeergave();
    await expect(this.page.getByTestId('paneel-review-groep')).toHaveCount(1);
  }

  async bewerkEerstePaneelInOverzicht(wandNaam: string) {
    await this.overzichtVoorWand(wandNaam).getByRole('button', { name: 'Bewerk' }).first().click();
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
