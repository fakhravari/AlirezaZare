import {test, expect} from '@playwright/test';

test.beforeEach(async ({page}) => {
  page.__storeErrors = [];
  page.on('pageerror', error => page.__storeErrors.push(error.message));
});
test.afterEach(async ({page}) => { expect(page.__storeErrors).toEqual([]); });

async function noHorizontalOverflow(page) {
  const width = await page.evaluate(() => ({content: document.documentElement.scrollWidth, viewport: window.innerWidth}));
  expect(width.content).toBeLessThanOrEqual(width.viewport + 1);
}

test('all home collections render shared cards with one action of each kind', async ({page}) => {
  await page.goto('/');
  for (const selector of ['#productGrid', '#bestsellersSlider', '#newestSlider']) {
    const cards = page.locator(`${selector} [data-product-card="v1"]`);
    await expect(cards.first()).toBeVisible();
    const first = cards.first();
    await expect(first.locator('.favorite')).toHaveCount(1);
    await expect(first.locator('.compare-trigger')).toHaveCount(1);
    await expect(first.locator('.product-price-current')).toHaveCount(1);
    await expect(first.locator('a[href^="/products/"]').first()).toHaveAttribute('href', /-\d+$/);
  }
  await noHorizontalOverflow(page);
});

test('search URL filters survive reload and browser navigation', async ({page}) => {
  await page.goto('/search?color=yellow');
  await expect(page.locator('#productResults [data-product-card="v1"]').first()).toBeVisible();
  await page.locator('#productSearch').fill('دریل');
  await expect(page).toHaveURL(/q=/);
  const count = await page.locator('#productResults [data-product-card="v1"]').count();
  expect(count).toBeGreaterThan(0);
  await page.reload();
  await expect(page.locator('#productSearch')).toHaveValue('دریل');
  await expect(page.locator('#productResults [data-product-card="v1"]')).toHaveCount(count);
  await page.locator('#productResults [data-product-card="v1"] a[href^="/products/"]').first().click();
  await expect(page).toHaveURL(/\/products\//);
  await page.goBack();
  await expect(page.locator('#productSearch')).toHaveValue('دریل');
  await expect(page.locator('#productResults [data-product-card="v1"]')).toHaveCount(count);
  await noHorizontalOverflow(page);
});

test('product opens at the top, gallery closes, and detail tabs remain reachable', async ({page}) => {
  await page.goto('/products/cordless-hammer-drill-1');
  await expect(page.locator('#addProduct')).toBeAttached();
  expect(await page.evaluate(() => window.scrollY)).toBeLessThan(10);
  await expect(page.locator('#addProduct')).not.toBeFocused();
  await page.locator('#openProductGallery').click();
  await expect(page.locator('#productGalleryTitle')).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.locator('#productGalleryTitle')).not.toBeVisible();
  await page.locator('.product-detail-tabs a[href="#product-specifications"]').click();
  await expect(page.locator('#product-specifications')).toBeVisible();
  await noHorizontalOverflow(page);
});

test('cart defaults, required service and reload use one canonical price', async ({page}) => {
  // Disposable demo browser context; no account, network login or live payment.
  await page.addInitScript(() => localStorage.setItem('hn-demo-auth', '1'));
  await page.goto('/products/cordless-hammer-drill-1');
  await page.locator('#addProduct').click();
  await expect.poll(() => page.evaluate(() => window.HNUI.cart.totals().count)).toBe(1);
  expect(await page.evaluate(() => window.HNUI.cart.read()[0].price)).toBe(24850000);
  await page.reload();
  expect(await page.evaluate(() => window.HNUI.cart.read()[0].price)).toBe(24850000);
  await page.goto('/cart');
  await expect(page.locator('#cartView')).toContainText('بیمه');
  await noHorizontalOverflow(page);
});

test('desktop dropdown opens from its trigger and Escape closes it while focused', async ({page, isMobile}) => {
  test.skip(isMobile, 'Desktop navigation has a separate mobile drawer.');
  for (const route of ['/', '/products/cordless-hammer-drill-1']) {
    await page.goto(route);
    const trigger = page.locator('#mainNav .brand-dropdown > .nav-link');
    const menu = page.locator('#mainNav .brand-dropdown > .brand-menu');
    await page.locator('#cartToggle').hover();
    await expect(menu).not.toBeVisible();
    await trigger.hover();
    await expect(menu).toBeVisible();
    await trigger.focus();
    await trigger.press('ArrowDown');
    await page.keyboard.press('Escape');
    await expect(menu).not.toBeVisible();
    await expect(trigger).toHaveAttribute('aria-expanded', 'false');
  }
});
