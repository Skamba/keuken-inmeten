import { expect, type Download, type Page } from '@playwright/test';

export class BestellijstPage {
  constructor(private readonly page: Page) {}

  async expectLoaded() {
    await expect(this.page.getByRole('heading', { name: 'Stap 4: Bestellijst' })).toBeVisible();
    await expect(this.page.getByTestId('bestellijst-tabel')).toBeVisible();
  }

  async exporteerExcel(): Promise<Download> {
    const knop = this.page.getByTestId('bestellijst-excel-export-button');
    await knop.scrollIntoViewIfNeeded();

    const [download] = await Promise.all([
      this.page.waitForEvent('download'),
      knop.click(),
    ]);

    await expect(this.page.getByTestId('actie-feedback-toast')).toContainText('Excel-bestand gedownload');
    return download;
  }
}
