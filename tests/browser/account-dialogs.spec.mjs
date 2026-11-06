import {test, expect} from '@playwright/test';

test('account dialogs fit the viewport and remain usable', async ({page, isMobile}) => {
  if (isMobile) await page.setViewportSize({width: 320, height: 640});
  await page.addInitScript(() => {
    localStorage.setItem('hn-demo-auth', '1');
    localStorage.setItem('hn-delivery-addresses', JSON.stringify([{
      id: 'sample-address', title: 'خانه', recipient: 'کاربر آزمایشی',
      phone: '09123456789', province: 'تهران', city: 'تهران',
      street: 'خیابان نمونه', postal: '1234567890',
    }]));
  });
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
  await page.locator('#profileEditDialog [data-profile-edit-close]').click();
  await expect(page.locator('#profileEditDialog')).not.toBeVisible();

  await page.locator('#newAddress').click();
  await assertFitsViewport(page.locator('#addressDialog'));
  if (isMobile) {
    for (const field of await page.locator('#addressDialog #addressForm input:not([type="hidden"]), #addressDialog #addressForm select, #addressDialog #addressForm textarea').all()) {
      await expect(field).toHaveCSS('font-size', '16px');
    }
  }
  await page.locator('#closeAddress').click();
  await expect(page.locator('#addressDialog')).not.toBeVisible();

  await page.locator('#addressList [data-delete="sample-address"]').click();
  await assertFitsViewport(page.locator('.form-confirm-dialog'));
  await page.locator('.form-confirm-cancel').click();
  await expect(page.locator('#addressList [data-delete="sample-address"]')).toBeVisible();
});

test('order invoice and cart address dialogs fit the viewport', async ({page, isMobile}) => {
  if (isMobile) await page.setViewportSize({width: 320, height: 640});
  await page.addInitScript(() => localStorage.setItem('hn-demo-auth', '1'));
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
  await page.locator('[data-close-order-invoice]').click();
  await expect(page.locator('#orderInvoiceDialog')).not.toBeVisible();

  await page.goto('/search');
  await page.evaluate(() => window.HNUI.cart.add(window.HNCatalog.find(1)));
  await page.goto('/cart');
  await page.locator('#newAddress').click();
  await checkDialog(page.locator('#addressDialog'));
  await page.locator('#closeAddress').click();
  await expect(page.locator('#addressDialog')).not.toBeVisible();
});
