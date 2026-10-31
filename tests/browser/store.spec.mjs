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

test('keyboard focus indicators and shared product actions keep their layout', async ({page}) => {
  await page.goto('/');
  await page.keyboard.press('Tab');
  await page.keyboard.press('Tab');
  const focusStyle = await page.evaluate(() => {
    const element = document.activeElement;
    const style = getComputedStyle(element);
    return {
      visible: element.matches(':focus-visible'),
      outline: style.outlineStyle,
      width: style.outlineWidth,
    };
  });
  expect(focusStyle.visible).toBe(true);
  expect(focusStyle.outline).not.toBe('none');
  expect(focusStyle.width).toBe('3px');

  const addToCart = page.locator('#productGrid [data-product-card="v1"] .add-cart').first();
  await expect(addToCart).toBeVisible();
  expect(await addToCart.evaluate(element => getComputedStyle(element).flexShrink)).toBe('0');
  const cartIcon = addToCart.locator('i');
  await expect(cartIcon).toHaveCSS('color', 'rgb(246, 197, 21)');
  await addToCart.hover();
  await expect(cartIcon).toHaveCSS('color', 'rgb(16, 42, 67)');
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

test('product plan selection uses the matching quote and recovers cleanly from an expired plan URL', async ({page}) => {
  await page.addInitScript(() => localStorage.setItem('hn-demo-auth', '1'));
  await page.goto('/products/cordless-hammer-drill-1?salesPlanId=digi-time');
  const selectedPrice = await page.evaluate(() => Number(
    window.HNUI.toEnglishDigits(document.querySelector('#productFinalPrice').textContent).replace(/\D/g, ''),
  ));
  expect(selectedPrice).toBe(23303823);
  await expect(page.locator('input[name="productSalesPlan"][value="digi-time"]')).toBeChecked();
  await page.locator('#addProduct').click();
  await expect.poll(() => page.evaluate(() => window.HNUI.cart.read()[0]?.price)).toBe(23303823);
  expect(await page.evaluate(() => window.HNUI.cart.read()[0]?.salesPlanId)).toBe('digi-time');
  expect(await page.evaluate(() => window.HNUI.cart.read()[0]?.salesPlanSnapshot?.accessoryPriceAmounts?.['shipping-insurance'])).toBe(245000);

  await page.clock.install({time: new Date('2027-01-01T00:00:00Z')});
  await page.goto('/products/cordless-hammer-drill-1?salesPlanId=digi-time');
  await expect(page.locator('input[name="productSalesPlan"][value="regular"]')).toBeChecked();
  await expect(page.locator('#productStatus')).toContainText('طرح انتخاب‌شده در دسترس نیست');
  await expect(page).not.toHaveURL(/salesPlanId=/);
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

test('cart coupon applies only to products without an existing sale', async ({page, isMobile}) => {
  await page.addInitScript(() => localStorage.setItem('hn-demo-auth', '1'));
  await page.goto('/cart');
  await page.evaluate(() => {
    window.HNUI.cart.add(window.HNCatalog.find(1)); // 15% product discount
    window.HNUI.cart.add(window.HNCatalog.find(3)); // no product discount
  });
  await expect(page.locator('.cart-product-row')).toHaveCount(2);
  if (isMobile) {
    await page.locator('#cartPanel .cart-panel-close').click();
    await expect(page.locator('#cartPanel')).not.toHaveClass(/open/);
  }

  const expectedDiscount = await page.evaluate(() => {
    const items = window.HNUI.cart.read();
    const products = window.HNProducts || [];
    const eligibleTotal = items.reduce((sum, item) => {
      const product = products.find(candidate => String(candidate.id) === String(item.id));
      return Number(product?.discountPercent) > 0
        ? sum
        : sum + item.price * item.quantity;
    }, 0);
    return Math.floor(eligibleTotal * 10 / 100);
  });

  await page.locator('#couponCode').fill('BAZAR10');
  await page.locator('#couponForm button[type="submit"]').click();
  await expect(page.locator('#couponStatus')).toContainText('فقط روی کالاهای بدون تخفیف');
  const actualDiscount = await page.locator('.flow-discount > span:last-child').evaluate(element =>
    Number(window.HNUI.toEnglishDigits(element.textContent).replace(/\D/g, '')),
  );
  expect(actualDiscount).toBe(expectedDiscount);
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
