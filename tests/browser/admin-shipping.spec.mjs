import {test, expect} from '@playwright/test';

const adminUrl = process.env.SHOP_TEST_ADMIN_URL;
const user = process.env.SHOP_TEST_ADMIN_USER;
const password = process.env.SHOP_TEST_ADMIN_PASSWORD;
const orderNumber = process.env.SHOP_TEST_SHIPPING_ORDER;
const connection = process.env.ConnectionStrings__DefaultConnection || '';

test('manager records tracking and shipping status on a verified order', async ({browser}) => {
  test.skip(!adminUrl || !user || !password || !orderNumber, 'Requires an isolated admin server and test order.');
  if (!['localhost', '127.0.0.1', '[::1]'].includes(new URL(adminUrl).hostname) ||
      !/(?:database|initial catalog)\s*=\s*ShopVerify_[^;]+/i.test(connection)) {
    throw new Error('Shipping test requires a local admin URL and a ShopVerify_ connection.');
  }
  const context = await browser.newContext({ignoreHTTPSErrors: true});
  const page = await context.newPage();
  try {
    await page.goto(`${adminUrl}/auth/login`);
    await page.locator('#UserName').fill(user);
    await page.locator('#Password').fill(password);
    await Promise.all([page.waitForURL(url => !url.pathname.includes('/auth/login')), page.locator('form button[type=submit]').click()]);
    await page.goto(`${adminUrl}/Store/Features?feature=orders`);
    const row = page.locator('[data-admin-results] tr').filter({hasText: orderNumber});
    await expect(row).toBeVisible();
    await row.getByRole('button', {name: 'رهگیری'}).click();
    const modal = page.locator('#order-tracking-modal');
    await expect(modal).toBeVisible();
    await modal.locator('[name=carrier]').fill('پست آزمایشی');
    await modal.locator('[name=trackingCode]').fill('TEST-12345');
    const trackingResponse = page.waitForResponse(response => response.url().endsWith('/tracking') && response.request().method() === 'PUT');
    await modal.getByRole('button', {name: 'ذخیره'}).click();
    expect((await trackingResponse).status()).toBe(200);
    await expect(modal).toBeHidden();
    page.once('dialog', dialog => dialog.accept());
    const statusResponse = page.waitForResponse(response => response.url().endsWith('/shipping-status') && response.request().method() === 'PUT');
    await row.getByRole('button', {name: 'ثبت ارسال'}).click();
    expect((await statusResponse).status()).toBe(200);
    await expect(row).toContainText('در حال ارسال');
    await row.getByRole('button', {name: 'نمایش جزئیات رکورد'}).click();
    const details = page.locator('#admin-record-modal');
    await expect(details).toContainText('TEST-12345');
    await expect(details).toContainText('پست آزمایشی');
  } finally {
    await context.close();
  }
});
