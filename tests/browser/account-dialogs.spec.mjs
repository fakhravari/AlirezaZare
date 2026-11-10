import {test, expect} from '@playwright/test';
import axe from 'axe-core';
import {useBrowserAccount} from '../helpers/browser-account.mjs';

async function expectNoA11yViolations(page, state) {
  const results = await page.evaluate(async () => window.axe.run(document, {
    runOnly: {type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice']},
  }));
  expect(results.violations.map(issue => ({id: issue.id, nodes: issue.nodes.map(node => node.target)})), state).toEqual([]);
}

test('address dialog waits for geography when opened immediately', async ({page}) => {
  await useBrowserAccount(page);
  await page.route('**/api/store/geography', async route => {
    await new Promise(resolve => setTimeout(resolve, 1500));
    await route.continue();
  });
  await page.goto('/profile/addresses');
  await expect(page.locator('#accountToggle')).toHaveClass(/logged-in/);
  await page.locator('#newAddress').click();
  await expect(page.locator('#addressDialog')).toBeVisible();
  await expect(page.locator('#addressForm [name="provinceId"] option')).not.toHaveCount(0);
});

test('address form only offers Iranian provinces and cities', async ({page}) => {
  await useBrowserAccount(page);
  await page.route('**/api/store/geography', route => route.fulfill({json: {
    countries: [{id: 1, name: 'آلمان', nameEn: 'Germany', countryCode: 'DE'}, {id: 2, name: 'ایران', nameEn: 'Iran', countryCode: 'IR'}],
    provinces: [{id: 10, countryId: 1, name: 'برلین'}, {id: 20, countryId: 2, name: 'تهران'}],
    cities: [{id: 11, countryId: 1, provinceId: 10, name: 'برلین'}, {id: 21, countryId: 2, provinceId: 20, name: 'تهران'}],
  }}));
  await page.goto('/profile/addresses');
  await page.locator('#newAddress').click();
  const form = page.locator('#addressForm');
  await expect(form.locator('[name="countryId"]')).toHaveValue('2');
  await expect(form.locator('[name="provinceId"] option')).toHaveText(['انتخاب استان', 'تهران']);
  await form.locator('[name="provinceId"]').selectOption('20');
  await expect(form.locator('[name="cityId"] option')).toHaveText(['انتخاب شهر', 'تهران']);
  expect(await page.evaluate(() => window.HNAddressFields.set({countryId: 1}))).toBe(false);
  await expect(form.locator('[name="countryId"]')).toHaveValue('2');
});

test('account dialogs fit the viewport and remain usable', async ({page, isMobile}) => {
  await page.addInitScript(axe.source);
  if (isMobile) await page.setViewportSize({width: 320, height: 640});
  await useBrowserAccount(page);
  await page.goto('/profile/addresses');

  const assertFitsViewport = async dialog => {
    await expect(dialog).toBeVisible();
    const bounds = await dialog.evaluate(element => {
      const rect = element.getBoundingClientRect();
      return {left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom, width: innerWidth, height: innerHeight, contentWidth: element.scrollWidth, innerWidth: element.clientWidth};
    });
    expect(bounds.left).toBeGreaterThanOrEqual(0);
    expect(bounds.right).toBeLessThanOrEqual(bounds.width);
    expect(bounds.top).toBeGreaterThanOrEqual(0);
    expect(bounds.bottom).toBeLessThanOrEqual(bounds.height);
    expect(bounds.contentWidth).toBeLessThanOrEqual(bounds.innerWidth + 1);
  };

  await page.locator('[data-profile-edit-trigger]').first().click();
  await assertFitsViewport(page.locator('#profileEditDialog'));
  await expectNoA11yViolations(page, 'profile edit dialog');
  await page.locator('#profileEditDialog [data-profile-edit-close]').click();
  await expect(page.locator('#profileEditDialog')).not.toBeVisible();

  await page.locator('#newAddress').click();
  await assertFitsViewport(page.locator('#addressDialog'));
  await expectNoA11yViolations(page, 'address dialog');
  if (isMobile) {
    for (const field of await page.locator('#addressDialog #addressForm input:not([type="hidden"]), #addressDialog #addressForm select, #addressDialog #addressForm textarea').all()) {
      await expect(field).toHaveCSS('font-size', '16px');
    }
  }
  await page.locator('#closeAddress').click();
  await expect(page.locator('#addressDialog')).not.toBeVisible();

  await page.locator('#addressList [data-delete="sample-address"]').click();
  await assertFitsViewport(page.locator('.form-confirm-dialog'));
  await expectNoA11yViolations(page, 'address delete confirmation');
  await page.locator('.form-confirm-cancel').click();
  await expect(page.locator('#addressList [data-delete="sample-address"]')).toBeVisible();
});

test('order invoice and cart address dialogs fit the viewport', async ({page, isMobile}) => {
  await page.addInitScript(axe.source);
  if (isMobile) await page.setViewportSize({width: 320, height: 640});
  const account = await useBrowserAccount(page);
  const checkDialog = async dialog => {
    await expect(dialog).toBeVisible();
    const dimensions = await dialog.evaluate(element => {
      const bounds = element.getBoundingClientRect();
      return {left: bounds.left, right: bounds.right, top: bounds.top, bottom: bounds.bottom, width: innerWidth, height: innerHeight, scrollWidth: element.scrollWidth, clientWidth: element.clientWidth};
    });
    expect(dimensions.left).toBeGreaterThanOrEqual(0);
    expect(dimensions.right).toBeLessThanOrEqual(dimensions.width);
    expect(dimensions.top).toBeGreaterThanOrEqual(0);
    expect(dimensions.bottom).toBeLessThanOrEqual(dimensions.height);
    expect(dimensions.scrollWidth).toBeLessThanOrEqual(dimensions.clientWidth + 1);
  };
  await page.goto('/profile/orders');
  await page.locator('[data-open-order-invoice]').first().click();
  await checkDialog(page.locator('#orderInvoiceDialog'));
  await expectNoA11yViolations(page, 'order invoice dialog');
  await page.locator('[data-close-order-invoice]').click();
  await expect(page.locator('#orderInvoiceDialog')).not.toBeVisible();

  await page.goto('/search');
  await expect(page.locator('#accountToggle')).toHaveClass(/logged-in/);
  await page.evaluate(() => window.HNUI.cart.add(window.HNCatalog.find(1)));
  await expect.poll(() => account.cart.length).toBe(1);
  await page.goto('/cart');
  await expect(page.locator('#cartView .cart-product-row')).toHaveCount(1);
  await expect(page.locator('#addressSelect option')).toHaveCount(2);
  await page.locator('#newAddress').click();
  await checkDialog(page.locator('#addressDialog'));
  await expectNoA11yViolations(page, 'cart address dialog');
  await page.locator('#closeAddress').click();
  await expect(page.locator('#addressDialog')).not.toBeVisible();
});
