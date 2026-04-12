import { expect, type Download, type Page } from '@playwright/test';

type ExportType = 'pdf' | 'excel';

export class BestellijstPage {
  constructor(private readonly page: Page) {}

  private async sluitFeedbackToastAlsNodig() {
    const toast = this.page.getByTestId('actie-feedback-toast');
    if (!(await toast.isVisible())) {
      return;
    }

    await toast.getByRole('button', { name: 'Sluiten' }).click();
    await expect(toast).toBeHidden();
  }

  async expectLoaded() {
    await expect(this.page.getByRole('heading', { name: 'Stap 4: Bestellijst' })).toBeVisible();
    await expect(this.page.getByTestId('bestellijst-tabel').first()).toBeVisible();
    await expect(this.page.getByTestId('bestellijst-open-exportflow-button')).toBeVisible();
  }

  async openExportFlow() {
    await this.sluitFeedbackToastAlsNodig();
    await this.page.getByTestId('bestellijst-open-exportflow-button').click();
    await expect(this.page.getByTestId('bestellijst-export-drawer')).toBeVisible();
    await expect(this.page.getByTestId('bestellijst-export-step-kies')).toBeVisible();
  }

  async selecteerExportType(type: ExportType) {
    await this.page.getByTestId(type === 'pdf' ? 'bestellijst-export-type-pdf' : 'bestellijst-export-type-excel').click();
  }

  async openExportMateriaalInstellingen() {
    const paneeltypeInput = this.page.getByTestId('bestellijst-paneeltype-input');
    if (await paneeltypeInput.isVisible()) {
      return;
    }

    const samenvatting = this.page.getByTestId('bestellijst-export-instellingen-summary');
    await samenvatting.scrollIntoViewIfNeeded();
    await samenvatting.click();
    await expect(paneeltypeInput).toBeVisible();
  }

  async gaNaarExportPreview() {
    await this.page.getByTestId('bestellijst-export-next-button').click();
    await expect(this.page.getByTestId('bestellijst-export-step-preview')).toBeVisible();
  }

  async expectExportPreview(label: string) {
    await expect(this.page.getByTestId('bestellijst-export-step-preview')).toContainText(label);
    await expect(this.page.getByTestId('bestellijst-export-preview')).toBeVisible();
  }

  async gaNaarExportBevestiging() {
    await this.page.getByTestId('bestellijst-export-next-button').click();
    await expect(this.page.getByTestId('bestellijst-export-step-bevestig')).toBeVisible();
  }

  async expectExportBevestiging(label: string) {
    await expect(this.page.getByTestId('bestellijst-export-step-bevestig')).toContainText(label);
    await expect(this.page.getByTestId('bestellijst-export-confirm-button')).toBeVisible();
  }

  async exporteerExcel(): Promise<Download> {
    await this.openExportFlow();
    await this.selecteerExportType('excel');
    await this.gaNaarExportPreview();
    await this.gaNaarExportBevestiging();

    const knop = this.page.getByTestId('bestellijst-export-confirm-button');
    await knop.scrollIntoViewIfNeeded();

    const [download] = await Promise.all([
      this.page.waitForEvent('download'),
      knop.click(),
    ]);

    const toast = this.page.getByTestId('actie-feedback-toast');
    await expect(toast).toContainText('Excel-bestand gedownload');
    await this.sluitFeedbackToastAlsNodig();
    return download;
  }

  async exporteerPdf(): Promise<Download> {
    await this.openExportFlow();
    await this.selecteerExportType('pdf');
    await this.gaNaarExportPreview();
    await this.gaNaarExportBevestiging();

    const knop = this.page.getByTestId('bestellijst-export-confirm-button');
    await knop.scrollIntoViewIfNeeded();

    const [download] = await Promise.all([
      this.page.waitForEvent('download'),
      knop.click(),
    ]);

    const toast = this.page.getByTestId('actie-feedback-toast');
    await expect(toast).toContainText('PDF-bestand gedownload');
    await this.sluitFeedbackToastAlsNodig();
    return download;
  }

  async gaNaarZaagplan() {
    const knop = this.page.getByTestId('stap-navigatie-volgende');
    await knop.scrollIntoViewIfNeeded();
    await knop.click();
    await expect(this.page).toHaveURL(/\/zaagplan$/);
  }
}
