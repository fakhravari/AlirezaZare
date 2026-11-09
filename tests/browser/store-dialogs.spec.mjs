import {test, expect} from '@playwright/test';
import axe from 'axe-core';
import {useBrowserAccount} from '../helpers/browser-account.mjs';

async function expectNoA11yViolations(page, state, scope = page.locator('body')) {
  const results = await scope.evaluate(async element => window.axe.run(element, {
    runOnly: {type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice']},
  }));
  expect(results.violations.map(issue => ({id: issue.id, nodes: issue.nodes.map(node => node.target)})), state).toEqual([]);
}

async function expectDialogFits(dialog) {
  await expect(dialog).toBeVisible();
  const bounds = await dialog.evaluate(element => {
    const rect = element.getBoundingClientRect();
    return {left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom, viewportWidth: innerWidth, viewportHeight: innerHeight, contentWidth: element.scrollWidth, innerWidth: element.clientWidth};
  });
  expect(bounds.left).toBeGreaterThanOrEqual(0);
  expect(bounds.right).toBeLessThanOrEqual(bounds.viewportWidth);
  expect(bounds.top).toBeGreaterThanOrEqual(0);
  expect(bounds.bottom).toBeLessThanOrEqual(bounds.viewportHeight);
  expect(bounds.contentWidth).toBeLessThanOrEqual(bounds.innerWidth + 1);
}

test('product dialogs fit and close on small and large screens', async ({page, isMobile}) => {
  if (isMobile) await page.setViewportSize({width: 320, height: 640});
  await page.goto('/products/cordless-hammer-drill-1');

  await page.locator('[data-accessory-details-id]').first().click();
  await expectDialogFits(page.locator('.product-accessory-dialog'));
  await page.locator('.product-accessory-dialog [data-accessory-close]').first().click();
  await expect(page.locator('.product-accessory-dialog')).not.toBeVisible();

  await page.locator('#openProductGallery').click();
  await expectDialogFits(page.locator('.product-gallery-dialog'));
  await page.locator('[data-gallery-close]').click();
  await expect(page.locator('.product-gallery-dialog')).not.toBeVisible();

  await page.locator('[data-price-dialog="history"]').click();
  await expectDialogFits(page.locator('.detail-price-dialog'));
  await page.locator('[data-price-dialog-close]').click();
  await expect(page.locator('.detail-price-dialog')).not.toBeVisible();
});

test('product dialogs have accessible names and restore keyboard focus after Escape', async ({page}) => {
  await page.addInitScript(axe.source);
  await page.goto('/products/cordless-hammer-drill-1');

  const accessoryTrigger = page.locator('[data-accessory-details-id]').first();
  await accessoryTrigger.focus();
  await accessoryTrigger.press('Enter');
  const accessoryDialog = page.getByRole('dialog', {name: 'جزئیات متعلقات و خدمات'});
  await expect(accessoryDialog).toBeVisible();
  await expectNoA11yViolations(page, 'accessory dialog', accessoryDialog);
  await page.keyboard.press('Escape');
  await expect(accessoryDialog).not.toBeVisible();
  await expect(accessoryTrigger).toBeFocused();

  const galleryTrigger = page.locator('#openProductGallery');
  await galleryTrigger.focus();
  await galleryTrigger.press('Enter');
  const galleryTitle = (await page.locator('#productGalleryTitle').textContent()).trim();
  const galleryDialog = page.getByRole('dialog', {name: galleryTitle, exact: true});
  await expect(galleryDialog).toBeVisible();
  await expectNoA11yViolations(page, 'gallery dialog', galleryDialog);
  await page.keyboard.press('Escape');
  await expect(galleryDialog).not.toBeVisible();
  await expect(galleryTrigger).toBeFocused();

  const priceTrigger = page.locator('[data-price-dialog="history"]');
  await priceTrigger.focus();
  await priceTrigger.press('Enter');
  const priceTitle = (await page.locator('#productPriceDialogTitle').textContent()).trim();
  const priceDialog = page.getByRole('dialog', {name: priceTitle, exact: true});
  await expect(priceDialog).toBeVisible();
  await expect(priceDialog).toHaveAttribute('aria-labelledby', 'productPriceDialogTitle');
  await expectNoA11yViolations(page, 'price history dialog', priceDialog);
  await page.keyboard.press('Escape');
  await expect(priceDialog).not.toBeVisible();
  await expect(priceTrigger).toBeFocused();
});

test('article feedback confirmation fits and can be cancelled', async ({page, isMobile}) => {
  if (isMobile) await page.setViewportSize({width: 320, height: 640});
  await useBrowserAccount(page);
  await page.goto('/magazine/multimeter-symbols-guide');
  await expect(page.locator('#accountToggle')).toHaveClass(/logged-in/);
  const form = page.locator('[data-feedback-form]');
  await form.locator('[name="name"]').fill('کاربر آزمایشی');
  await form.locator('[name="text"]').fill('این دیدگاه برای بررسی نمایش فرم است.');
  await form.locator('[type="submit"]').click();
  await expectDialogFits(page.locator('.form-confirm-dialog'));
  await page.locator('.form-confirm-cancel').click();
  await expect(page.locator('.form-confirm-dialog')).not.toBeVisible();
  await expect(form.locator('[name="text"]')).toHaveValue('این دیدگاه برای بررسی نمایش فرم است.');
});

test('login and registration remain usable in a short viewport', async ({page, isMobile}) => {
  if (isMobile) await page.setViewportSize({width: 320, height: 640});
  await page.goto('/');
  await page.locator('#accountToggle').click();
  const modal = page.locator('#loginModal');
  await expect(modal).toBeVisible();
  const content = modal.locator('.modal-content');
  await expectDialogFits(content);
  await expect(modal.locator('#loginEmail')).toBeVisible();
  await modal.locator('#registerModeTab').click();
  await expect(modal.locator('#registerEmailStep')).toBeVisible();
  await expectDialogFits(content);
  await modal.locator('.login-close').click();
  await expect(modal).not.toBeVisible();
});
