import { expect, type Download, type Locator, type Page } from '@playwright/test';

export class ProjectPage {
  constructor(private readonly page: Page) {}

  private mobieleNavKnop(): Locator {
    return this.page.getByRole('button', { name: 'Navigatiemenu openen of sluiten' });
  }

  private navLink(): Locator {
    return this.page.getByTestId('nav-project-link');
  }

  async goto() {
    await this.page.goto('/project');
    await this.expectLoaded();
  }

  async openVanuitNavigatie() {
    const link = this.navLink();

    if (!(await link.isVisible())) {
      const navKnop = this.mobieleNavKnop();
      if (await navKnop.isVisible()) {
        await navKnop.click();
        await expect(link).toBeVisible();
      }
    }

    await link.click();
    await this.expectLoaded();
  }

  async expectLoaded() {
    await expect(this.page.getByRole('heading', { name: 'Projectinstellingen en opties' })).toBeVisible();
    await expect(this.page.getByTestId('project-page')).toBeVisible();
  }

  async exporteerProject(): Promise<Download> {
    const downloadPromise = this.page.waitForEvent('download');
    await this.page.getByTestId('project-export-button').click();
    return await downloadPromise;
  }

  async wisProject() {
    await this.page.getByTestId('project-delete-button').click();
    await expect(this.page.getByTestId('project-delete-confirmation')).toBeVisible();
    await this.page.getByTestId('project-delete-confirm-button').click();
  }
}
