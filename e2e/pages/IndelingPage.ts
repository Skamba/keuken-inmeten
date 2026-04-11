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

  wandOpstelling(wandNaam: string): Locator {
    return this.actieveWerkruimte(wandNaam).getByTestId('wand-opstelling-svg');
  }

  werkruimte(wandNaam: string): Locator {
    return this.actieveWerkruimte(wandNaam);
  }

  eersteWandKast(wandNaam: string): Locator {
    return this.actieveWerkruimte(wandNaam).getByTestId('wand-kast').first();
  }

  wandKast(wandNaam: string, kastNaam: string): Locator {
    return this.actieveWerkruimte(wandNaam)
      .getByTestId('wand-kast')
      .filter({ hasText: kastNaam })
      .first();
  }

  eersteWandKastRect(wandNaam: string): Locator {
    return this.eersteWandKast(wandNaam).locator('rect').first();
  }

  wandKastRect(wandNaam: string, kastNaam: string): Locator {
    return this.wandKast(wandNaam, kastNaam).locator('rect').first();
  }

  eersteWandPlank(wandNaam: string): Locator {
    return this.actieveWerkruimte(wandNaam).getByTestId('wand-plank').first();
  }

  eersteWandPlankRect(wandNaam: string): Locator {
    return this.eersteWandPlank(wandNaam).locator('rect').first();
  }

  eersteWandPlankLabel(wandNaam: string): Locator {
    return this.eersteWandPlank(wandNaam).getByTestId('wand-plank-label');
  }

  async goto() {
    await this.page.goto('/kasten');
    await expect(this.page.getByRole('heading', { name: 'Stap 1: Indeling' })).toBeVisible();
  }

  async openWandToevoegenModal() {
    const openKnop = this.page.getByTestId('open-wand-toevoegen-modal-button').first();
    if (!(await openKnop.isVisible())) {
      const extraWandSamenvatting = this.page.getByTestId('indeling-extra-wand-summary');
      if (await extraWandSamenvatting.isVisible()) {
        await extraWandSamenvatting.click();
      }
    }

    await this.page.getByTestId('open-wand-toevoegen-modal-button').first().click();
    await expect(this.page.getByTestId('wand-toevoegen-modal')).toBeVisible();
  }

  async voegWandToe(naam: string) {
    await this.openWandToevoegenModal();
    await this.page.getByTestId('nieuwe-wand-naam-input').fill(naam);
    await this.page.getByTestId('wand-toevoegen-button').click();
    await expect(this.page.getByTestId('wand-toevoegen-modal')).toBeHidden();
    const resultaat = this.wandCard(naam).or(this.actieveWerkruimte(naam));
    if (!(await resultaat.isVisible())) {
      const overigeWandenSamenvatting = this.page.getByTestId('indeling-overige-wanden-summary');
      if (await overigeWandenSamenvatting.isVisible()) {
        await overigeWandenSamenvatting.click();
      }
    }

    await expect(resultaat).toBeVisible();
  }

  async openWandWerkruimte(wandNaam: string) {
    if (await this.actieveWerkruimte(wandNaam).isVisible()) {
      await this.expectActieveWerkruimte(wandNaam);
      return;
    }

    const wand = this.wandCard(wandNaam);
    const openKnop = wand.getByTestId('open-wand-workspace-button');

    if (!(await openKnop.isVisible())) {
      const overigeWandenSamenvatting = this.page.getByTestId('indeling-overige-wanden-summary');
      if (await overigeWandenSamenvatting.isVisible()) {
        await overigeWandenSamenvatting.click();
        await expect(openKnop).toBeVisible();
      }
    }

    await openKnop.click();

    await this.expectActieveWerkruimte(wandNaam);
  }

  async expectActieveWerkruimte(wandNaam: string) {
    await expect(this.page.getByTestId('actieve-wand-werkruimte')).toHaveCount(1);
    await expect(this.actieveWerkruimte(wandNaam)).toBeVisible();
  }

  async openKastFormulierVoorWand(wandNaam: string) {
    await this.openWandWerkruimte(wandNaam);

    const werkruimte = this.actieveWerkruimte(wandNaam);
    await werkruimte.getByTestId('open-kast-form-button').click();

    const formulier = this.page.getByTestId('kast-form');
    await expect(formulier).toBeVisible();
    return formulier;
  }

  async openApparaatFormulierVoorWand(wandNaam: string) {
    await this.openWandWerkruimte(wandNaam);

    const werkruimte = this.actieveWerkruimte(wandNaam);
    await werkruimte.getByTestId('open-apparaat-form-button').click();

    const formulier = this.page.getByTestId('apparaat-form');
    await expect(formulier).toBeVisible();
    return formulier;
  }

  async expectKastFormStap(stapLabel: string) {
    await expect(this.page.getByTestId('kast-form-stap-label')).toContainText(stapLabel);
  }

  async gaNaarVolgendeKastFormStap() {
    await this.page.getByTestId('kast-form-volgende-button').click();
  }

  async bevestigTechnischeKastControle() {
    const checkbox = this.page.getByTestId('kast-technische-controle-checkbox');
    await expect(checkbox).toBeVisible();
    if (!(await checkbox.isChecked())) {
      await checkbox.check();
    }
  }

  async gaNaarVolgendeApparaatFormStap() {
    await this.page.getByTestId('apparaat-form-volgende-button').click();
  }

  async voegKastToeAanWand(wandNaam: string, kast: KastGegevens) {
    const formulier = await this.openKastFormulierVoorWand(wandNaam);
    const werkruimte = this.actieveWerkruimte(wandNaam);

    await this.page.getByTestId('kast-naam-input').fill(kast.naam);
    await this.gaNaarVolgendeKastFormStap();

    await this.page.getByTestId('kast-breedte-input').fill(kast.breedte.toString());
    await this.page.getByTestId('kast-hoogte-input').fill(kast.hoogte.toString());
    await this.page.getByTestId('kast-diepte-input').fill(kast.diepte.toString());
    await this.gaNaarVolgendeKastFormStap();
    await this.bevestigTechnischeKastControle();
    await this.gaNaarVolgendeKastFormStap();

    await this.page.getByTestId('kast-opslaan-button').click();

    await expect(formulier).toBeHidden();
    await expect(werkruimte).toContainText(kast.naam);
  }

  async voegPlankToeAanEersteKast(wandNaam: string, relativeY = 0.55) {
    await this.openWandWerkruimte(wandNaam);

    const kastRect = this.eersteWandKastRect(wandNaam);
    await kastRect.scrollIntoViewIfNeeded();

    const box = await kastRect.boundingBox();
    expect(box).not.toBeNull();

    const clickX = box!.x + box!.width / 2;
    const clickY = box!.y + box!.height * relativeY;

    await this.page.mouse.click(clickX, clickY, { clickCount: 2, delay: 50 });
    await expect(this.eersteWandPlank(wandNaam)).toBeVisible();
  }

  async leesKastXMm(wandNaam: string, kastNaam: string) {
    return this.leesKastAttribuutAsGetal(wandNaam, kastNaam, 'data-x-mm');
  }

  async leesKastFloorMm(wandNaam: string, kastNaam: string) {
    return this.leesKastAttribuutAsGetal(wandNaam, kastNaam, 'data-floor-mm');
  }

  async klikKast(wandNaam: string, kastNaam: string) {
    const rect = this.wandKastRect(wandNaam, kastNaam);
    await rect.scrollIntoViewIfNeeded();

    const box = await rect.boundingBox();
    expect(box).not.toBeNull();

    await this.page.mouse.click(box!.x + box!.width / 2, box!.y + box!.height / 2);
  }

  async gaNaarPanelen() {
    const knop = this.page.getByTestId('stap-navigatie-volgende');
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page).toHaveURL(/\/panelen$/);
  }

  private async leesKastAttribuutAsGetal(wandNaam: string, kastNaam: string, attribuut: string) {
    const waarde = await this.wandKast(wandNaam, kastNaam).getAttribute(attribuut);
    expect(waarde, `${attribuut} ontbreekt op kast ${kastNaam}`).not.toBeNull();

    const getal = Number.parseFloat(waarde!);
    expect(Number.isFinite(getal), `${attribuut} is geen geldig getal: ${waarde}`).toBeTruthy();
    return getal;
  }
}
