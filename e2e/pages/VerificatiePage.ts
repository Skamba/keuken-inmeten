import { expect, type Page } from '@playwright/test';

export class VerificatiePage {
  constructor(private readonly page: Page) {}

  private taakGroep(wandNaam: string) {
    return this.page.locator(`[data-testid="verificatie-taakgroep"][data-wand-naam="${wandNaam}"]`);
  }

  private taakItem(wandNaam: string, kastNaam: string) {
    return this.taakGroep(wandNaam)
      .getByTestId('verificatie-taak-item')
      .filter({ hasText: kastNaam })
      .first();
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

  async expectUitlijnWaarschuwing(wandNaam?: string) {
    const waarschuwing = this.page.getByTestId('verificatie-uitlijn-waarschuwing');
    await expect(waarschuwing).toBeVisible();

    if (wandNaam) {
      await expect(
        this.page.locator(`[data-testid="verificatie-uitlijn-waarschuwing-item"][data-wand-naam="${wandNaam}"]`),
      ).toBeVisible();
    }
  }

  async expectGeenUitlijnWaarschuwing() {
    await expect(this.page.getByTestId('verificatie-uitlijn-waarschuwing')).toHaveCount(0);
  }

  async expectUitlijnTekst(tekst: string) {
    await expect(this.page.getByTestId('verificatie-uitlijn-waarschuwing')).toContainText(tekst);
  }

  async startVerificatie() {
    const knop = this.page.getByTestId('verificatie-start-button');
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page.locator('.verificatie-check').first()).toBeVisible();
  }

  async openControleVoorWand(wandNaam: string) {
    const knop = this.taakGroep(wandNaam).getByTestId('verificatie-open-controle-button').first();
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page.locator('.verificatie-check').first()).toBeVisible();
  }

  async vinkMatenCheckAf() {
    const knop = this.page.getByTestId('verificatie-check-toggle-maten');
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(knop).toHaveClass(/ok/);
  }

  async expectMatenCheckAfgevinkt() {
    await expect(this.page.getByTestId('verificatie-check-toggle-maten')).toHaveClass(/ok/);
  }

  async terugNaarTaaklijst() {
    const knop = this.page.getByRole('button', { name: /Terug naar taaklijst/ });
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await this.expectTaaklijst();
  }

  async expectTaakItemKlaar(wandNaam: string, kastNaam: string) {
    await expect(this.taakItem(wandNaam, kastNaam)).toContainText('Klaar');
  }

  async wachtTotAutomatischOpgeslagen() {
    await expect(this.page.getByText(/Automatisch opgeslagen om/)).toBeVisible();
  }

  async gaNaarBestellijst() {
    const knop = this.page.getByTestId('stap-navigatie-volgende');
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page).toHaveURL(/\/bestellijst$/);
  }

  async rondHuidigeVerificatieAf() {
    const toggles = this.page.locator('.verificatie-check-cirkel');
    const count = await toggles.count();

    for (let index = 0; index < count; index++) {
      const toggle = toggles.nth(index);
      await toggle.scrollIntoViewIfNeeded();
      await toggle.click();
    }

    const knop = this.page.getByRole('button', { name: /Afronden/ });
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page.getByTestId('verificatie-completion-card')).toBeVisible();
  }

  async gaNaarBestellijstViaAfronding() {
    const knop = this.page.getByTestId('verificatie-completion-primary-button');
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page).toHaveURL(/\/bestellijst$/);
  }
}
