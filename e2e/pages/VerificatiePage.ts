import { expect, type Page } from '@playwright/test';

export class VerificatiePage {
  constructor(private readonly page: Page) {}

  async expectLoaded() {
    await expect(this.page.getByRole('heading', { name: 'Stap 3: Verificatie' })).toBeVisible();
  }

  async startVerificatie() {
    const knop = this.page.getByTestId('verificatie-start-button');
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
