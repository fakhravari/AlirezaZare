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
  await page.locator('#closeAddress').click();
  await expect(page.locator('#addressDialog')).not.toBeVisible();

  await page.locator('#addressList [data-delete="sample-address"]').click();
  await assertFitsViewport(page.locator('.form-confirm-dialog'));
  await page.locator('.form-confirm-cancel').click();
  await expect(page.locator('#addressList [data-delete="sample-address"]')).toBeVisible();
});
