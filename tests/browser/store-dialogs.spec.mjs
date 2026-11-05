import {test, expect} from '@playwright/test';

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

test('article feedback confirmation fits and can be cancelled', async ({page, isMobile}) => {
  if (isMobile) await page.setViewportSize({width: 320, height: 640});
  await page.goto('/magazine/multimeter-symbols-guide');
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
