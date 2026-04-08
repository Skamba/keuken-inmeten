import { expect, type Page } from '@playwright/test';

export class VerificatiePage {
  constructor(private readonly page: Page) {}

  private taakGroep(wandNaam: string) {
    return this.page.locator(`[data-testid="verificatie-taakgroep"][data-wand-naam="${wandNaam}"]`);
  }

  async expectLoaded() {
    await expect(this.page.getByRole('heading', { name: 'Stap 3: Verificatie' })).toBeVisible();
  }

  async expectTaaklijst() {
    await expect(this.page.getByTestId('verificatie-taaklijst')).toBeVisible();
  }

  async expectTaakgroep(wandNaam: string) {
    await expect(this.taakGroep(wandNaam)).toBeVisible();
  }

  async startVerificatie() {
    const knop = this.page.getByTestId('verificatie-start-button');
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page.getByRole('heading', { name: 'Meet de maat in de opening na' })).toBeVisible();
  }

  async openControleVoorWand(wandNaam: string) {
    const knop = this.taakGroep(wandNaam).getByTestId('verificatie-open-controle-button').first();
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page.getByRole('heading', { name: 'Meet de maat in de opening na' })).toBeVisible();
  }

  async gaNaarBestellijst() {
    const knop = this.page.getByTestId('stap-navigatie-volgende');
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page).toHaveURL(/\/bestellijst$/);
  }
}
