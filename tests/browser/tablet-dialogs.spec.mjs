import {test, expect} from '@playwright/test';
import {useBrowserAccount} from '../helpers/browser-account.mjs';

async function expectDialogInsideViewport(dialog) {
  await expect(dialog).toBeVisible();
  const bounds = await dialog.evaluate(element => {
    const rect = element.getBoundingClientRect();
    return {left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom,
      width: innerWidth, height: innerHeight, scrollWidth: element.scrollWidth, clientWidth: element.clientWidth};
  });
  expect(bounds.left).toBeGreaterThanOrEqual(0);
  expect(bounds.right).toBeLessThanOrEqual(bounds.width);
  expect(bounds.top).toBeGreaterThanOrEqual(0);
  expect(bounds.bottom).toBeLessThanOrEqual(bounds.height);
  expect(bounds.scrollWidth).toBeLessThanOrEqual(bounds.clientWidth + 1);
}

test('product dialogs fit at tablet width', async ({page}) => {
  await page.setViewportSize({width: 768, height: 800});
  await page.goto('/products/cordless-hammer-drill-1');
  await page.locator('[data-accessory-details-id]').first().click();
  await expectDialogInsideViewport(page.locator('.product-accessory-dialog'));
  await page.locator('.product-accessory-dialog [data-accessory-close]').first().click();

  await page.locator('#openProductGallery').click();
  await expectDialogInsideViewport(page.locator('.product-gallery-dialog'));
  await page.locator('[data-gallery-close]').click();

  await page.locator('[data-price-dialog="history"]').click();
  await expectDialogInsideViewport(page.locator('.detail-price-dialog'));
  await page.locator('[data-price-dialog-close]').click();
});

test('account dialogs fit at tablet width', async ({page}) => {
  await page.setViewportSize({width: 768, height: 800});
  await useBrowserAccount(page, {addressId: 'tablet-sample'});
  await page.goto('/profile/addresses');
  await page.locator('[data-profile-edit-trigger]').first().click();
  await expectDialogInsideViewport(page.locator('#profileEditDialog'));
  await page.locator('#profileEditDialog [data-profile-edit-close]').click();

  await page.locator('#newAddress').click();
  await expectDialogInsideViewport(page.locator('#addressDialog'));
  await page.locator('#closeAddress').click();

  await page.locator('#addressList [data-delete="tablet-sample"]').click();
  await expectDialogInsideViewport(page.locator('.form-confirm-dialog'));
  await page.locator('.form-confirm-cancel').click();

  await page.goto('/profile/orders');
  await page.locator('[data-open-order-invoice]').first().click();
  await expectDialogInsideViewport(page.locator('#orderInvoiceDialog'));
  await page.locator('[data-close-order-invoice]').click();

  await page.goto('/search');
  await expect(page.locator('#accountToggle')).toHaveClass(/logged-in/);
  await page.evaluate(() => window.HNUI.cart.add(window.HNCatalog.find(1)));
  await page.goto('/cart');
  await expect(page.locator('#cartView .cart-product-row')).toHaveCount(1);
  await expect(page.locator('#addressSelect option')).toHaveCount(2);
  await page.locator('#newAddress').click();
  await expectDialogInsideViewport(page.locator('#addressDialog'));
  await page.locator('#closeAddress').click();
});

test('article feedback and login dialogs fit at tablet width', async ({page}) => {
  await page.setViewportSize({width: 768, height: 800});
  await useBrowserAccount(page);
  await page.goto('/magazine/multimeter-symbols-guide');
  await expect(page.locator('#accountToggle')).toHaveClass(/logged-in/);
  const form = page.locator('[data-feedback-form]');
  await form.locator('[name="name"]').fill('کاربر آزمایشی');
  await form.locator('[name="text"]').fill('دیدگاه نمونه برای بازبینی اندازهٔ پنجره.');
  await form.locator('[type="submit"]').click();
  await expectDialogInsideViewport(page.locator('.form-confirm-dialog'));
  await page.locator('.form-confirm-cancel').click();

  await page.unrouteAll({behavior: 'wait'});
  await page.goto('/');
  await page.locator('#accountToggle').click();
  const login = page.locator('#loginModal');
  await expect(login).toBeVisible();
  await expectDialogInsideViewport(login.locator('.modal-content'));
  await expect(login.locator('#loginPasswordMethod')).toBeVisible();
  await expect(login.locator('#loginOtpMethod')).toBeVisible();
  await expect(login.locator('#registerModeTab')).toHaveCount(0);
  await login.locator('.login-close').click();
});
