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

test('all home collections render shared cards with one action of each kind', async ({page, isMobile}) => {
  await page.addInitScript(() => localStorage.setItem('hn-demo-auth', '1'));
  await page.clock.install({time: new Date('2026-11-01T10:00:00Z')});
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
  const planSections = page.locator('#homeSalesTime .home-sales-plan, #homeSalesPlans .home-sales-plan');
  await expect(planSections).toHaveCount(2);
  await expect(page.locator('#homeSalesTime .home-sales-plan-grid .product-card')).toHaveCount(7);
  await expect(page.locator('#homeSalesPlans .home-sales-plan-grid .product-card')).toHaveCount(13);
  await expect(page.locator('#homeSalesTime .home-sales-plan[data-sales-plan="digi-time"]')).toHaveCount(1);
  await expect(page.locator('#homeSalesPlans .home-sales-plan[data-sales-plan="digi-af"]')).toHaveCount(1);
  const timePlacement = await page.evaluate(() => {
    const follows = (first, second) => Boolean(first.compareDocumentPosition(second) & Node.DOCUMENT_POSITION_FOLLOWING);
    return follows(document.querySelector('#heroSwiper'), document.querySelector('#homeSalesTime')) &&
      follows(document.querySelector('#homeSalesTime'), document.querySelector('#collection'));
  });
  expect(timePlacement).toBe(true);
  for (const section of await planSections.all()) {
    const planId = await section.getAttribute('data-sales-plan');
    expect(['digi-af', 'digi-time']).toContain(planId);
    const planHeader = await section.locator('.category-showcase-head').evaluate(header => ({
      background: getComputedStyle(header).backgroundImage,
      radius: getComputedStyle(header).borderRadius,
      titleColor: getComputedStyle(header.querySelector('h2')).color,
    }));
    expect(planHeader.background).toContain('radial-gradient');
    expect(planHeader.radius).toBe(isMobile ? '16px' : '20px');
    expect(planHeader.titleColor).toBe('rgb(255, 255, 255)');
    const product = section.locator('.home-sales-plan-grid > div .product-card').first();
    await expect(product).toBeVisible();
    const planImage = product.locator('.product-image img');
    await expect.poll(() => planImage.evaluate(image => image.complete && image.naturalWidth > 0)).toBe(true);
    const imageLayout = await planImage.evaluate(image => {
      const style = getComputedStyle(image);
      const imageRect = image.getBoundingClientRect();
      const frameRect = image.parentElement.getBoundingClientRect();
      return {fit: style.objectFit, padding: style.padding, imageWidth: imageRect.width, frameWidth: frameRect.width, imageHeight: imageRect.height, frameHeight: frameRect.height};
    });
    expect(imageLayout.fit).toBe('cover');
    expect(imageLayout.padding).toBe('0px');
    expect(imageLayout.imageWidth).toBeCloseTo(imageLayout.frameWidth, 0);
    expect(imageLayout.imageHeight).toBeCloseTo(imageLayout.frameHeight, 0);
    const href = await product.locator('h3 a').getAttribute('href');
    expect(new URL(href, 'http://localhost').searchParams.get('salesPlanId')).toBe(planId);
    expect(await product.locator('.add-cart').getAttribute('data-sales-plan-id')).toBe(planId);
    await expect(product.locator('.product-sales-plan-badge')).toContainText(await product.getAttribute('data-sales-plan-name'));
    const countdown = section.locator('[data-sales-plan-countdown]');
    await expect(countdown).toBeVisible();
    if (planId === 'digi-time') await expect(countdown).not.toContainText('روز');
    else await expect(countdown).toContainText('روز');
    await expect(countdown.locator('bdi[dir="ltr"]')).toHaveText(/[۰-۹]{2}:[۰-۹]{2}:[۰-۹]{2}/);
    expect(await product.evaluate(element => element.parentElement.classList.contains('col-xl-2'))).toBe(true);
    const columnRatio = await product.evaluate(element => element.parentElement.getBoundingClientRect().width / element.parentElement.parentElement.getBoundingClientRect().width);
    expect(columnRatio).toBeCloseTo(isMobile ? 0.5 : 1 / 6, 1);
  }
  const planAdd = page.locator('#homeSalesTime .add-cart[data-sales-plan-id="digi-time"]').first();
  const planPrice = Number(await planAdd.getAttribute('data-price'));
  await planAdd.click();
  await expect.poll(() => page.evaluate(() => window.HNUI.cart.read().find(item => item.salesPlanId === 'digi-time')?.price)).toBe(planPrice);
  await expect(page.locator('#cartPanel .cart-sales-plan-badge')).toContainText(/دیجی‌تایم/);
  await noHorizontalOverflow(page);
});

test('keyboard focus indicators and shared product actions keep their layout', async ({page}) => {
  await page.goto('/');
  const regularProductHref = await page.locator('#productGrid [data-product-card="v1"] h3 a').first().getAttribute('href');
  expect(new URL(regularProductHref, 'http://localhost').searchParams.has('salesPlanId')).toBe(false);
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

test('search includes active plan offers and preserves the selected offer in cart', async ({page}) => {
  await page.addInitScript(() => localStorage.setItem('hn-demo-auth', '1'));
  await page.clock.install({time: new Date('2026-11-01T10:00:00Z')});
  await page.goto('/search');
  const listing = await page.evaluate(() => {
    const plans = JSON.parse(document.querySelector('#storeSalesPlans').textContent);
    const offer = plans.offers.find(item => item.productId === 1 && item.salesPlanId === 'digi-time');
    const plan = plans.plans.find(item => item.id === offer.salesPlanId);
    return {offer, plan, product: window.HNCatalog.find(offer.productId)};
  });
  await page.locator('#productSearch').fill(listing.product.name);
  const offerCard = page.locator(`#productResults [data-product-id="${listing.offer.productId}"][data-sales-plan-id="${listing.plan.id}"]`);
  await expect(offerCard).toBeVisible();
  await expect(offerCard.locator('.product-sales-plan-badge')).toContainText(listing.plan.name);
  const expectedPrice = Math.round(listing.offer.priceAmount * (100 - listing.offer.discountPercent) / 100);
  await expect(offerCard.locator('.add-cart')).toHaveAttribute('data-price', String(expectedPrice));
  await offerCard.locator('.add-cart').click();
  await expect.poll(() => page.evaluate(() => window.HNUI.cart.read().find(item => item.salesPlanId === 'digi-time')?.price)).toBe(expectedPrice);
});

test('sales plan navigation opens search with that plan selected', async ({page, isMobile}) => {
  await page.clock.install({time: new Date('2026-11-01T10:00:00Z')});
  await page.goto('/');

  if (isMobile) {
    await page.locator('#mainNav .navbar-toggler').click();
    await page.locator('[data-bs-target="#mobileSalesPlans"]').click();
    await page.locator('#mobileSalesPlans a[href*="sales-plan=digi-time"]').click();
  } else {
    await page.locator('.sales-plans-dropdown > button').click();
    await page.locator('.sales-plan-menu a[href*="sales-plan=digi-time"]').click();
  }

  await expect(page).toHaveURL(/sales-plan=digi-time/);
  await expect(page.locator('#salesPlanFilters input[value="digi-time"]')).toBeChecked();
  await expect(page.locator('.filter-group[data-filter-slug="sales-plan"]')).toHaveClass(/open/);
  await expect(page.locator('#productResults .search-sales-plan-heading')).toHaveCount(0);
  const resultPlanIds = await page.locator('#productResults [data-product-card="v1"]').evaluateAll(cards =>
    cards.map(card => card.getAttribute('data-sales-plan-id')));
  expect(resultPlanIds.length).toBeGreaterThan(0);
  expect(resultPlanIds.every(id => id === 'digi-time')).toBe(true);
});

test('search plan and inventory filters allow one choice at a time', async ({page, isMobile}) => {
  await page.clock.install({time: new Date('2026-11-01T10:00:00Z')});
  await page.goto('/search?sales-plan=digi-time&sales-plan=digi-af');
  await expect(page.locator('#salesPlanFilters input[value="digi-af"]')).toBeChecked();
  await expect.poll(() => page.evaluate(() => new URL(location.href).searchParams.getAll('sales-plan'))).toEqual(['digi-af']);

  await page.goto('/search');
  if (isMobile) await page.locator('#mobileFilterToggle').click();

  const planGroup = page.locator('.filter-group[data-filter-slug="sales-plan"]');
  await planGroup.locator(':scope > button').click();
  const planRadios = page.locator('#salesPlanFilters input[name="salesPlanFilter"]');
  await expect(planRadios).toHaveCount(4);
  await expect(page.locator('#salesPlanFilters input[value="all"]')).toBeChecked();
  const planOptionRows = await planRadios.evaluateAll(inputs =>
    inputs.map(input => Math.round(input.closest('label').getBoundingClientRect().top)));
  expect(new Set(planOptionRows).size).toBe(planOptionRows.length);

  await page.locator('#salesPlanFilters input[value="digi-time"]').check();
  await expect(page).toHaveURL(/sales-plan=digi-time/);
  await expect(page.locator('#salesPlanFilters input[name="salesPlanFilter"]:checked')).toHaveCount(1);
  await page.locator('#salesPlanFilters input[value="regular"]').check();
  await expect(page).toHaveURL(/sales-plan=regular/);
  await expect(page.locator('#salesPlanFilters input[name="salesPlanFilter"]:checked')).toHaveCount(1);
  const regularCards = await page.locator('#productResults [data-product-card="v1"]').evaluateAll(cards =>
    cards.map(card => card.getAttribute('data-sales-plan-id')));
  expect(regularCards.length).toBeGreaterThan(0);
  expect(regularCards.every(planId => planId === null)).toBe(true);
  await page.locator('#salesPlanFilters input[value="all"]').check();
  await expect(page).not.toHaveURL(/sales-plan=/);

  const availability = page.locator('input[name="availability"]');
  await expect(availability).toHaveCount(3);
  await expect(page.locator('input[name="availability"][value="all"]')).toBeChecked();
  const availabilityRows = await availability.evaluateAll(inputs =>
    inputs.map(input => Math.round(input.closest('label').getBoundingClientRect().top)));
  expect(new Set(availabilityRows).size).toBe(availabilityRows.length);
  await page.locator('input[name="availability"][value="in-stock"]').check();
  await expect(page).toHaveURL(/available=1/);
  await expect(page.locator('input[name="availability"]:checked')).toHaveCount(1);
  await page.locator('input[name="availability"][value="out-of-stock"]').check();
  await expect(page).toHaveURL(/available=0/);
  await expect(page.locator('input[name="availability"]:checked')).toHaveCount(1);
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

test('product detail shows only its URL plan and recovers cleanly from an expired plan URL', async ({page}) => {
  await page.addInitScript(() => localStorage.setItem('hn-demo-auth', '1'));
  await page.clock.install({time: new Date('2026-11-01T10:00:00Z')});
  await page.goto('/products/cordless-hammer-drill-1');
  await expect(page.locator('.product-sales-plans, .product-sales-plan--fixed')).toHaveCount(0);
  const regularPrice = await page.evaluate(() => Number(
    window.HNUI.toEnglishDigits(document.querySelector('#productFinalPrice').textContent).replace(/\D/g, ''),
  ));
  expect(regularPrice).toBe(24850000);

  await page.goto('/products/cordless-hammer-drill-1?salesPlanId=regular');
  await expect(page).not.toHaveURL(/salesPlanId=/);
  await expect(page.locator('.product-sales-plans, .product-sales-plan--fixed')).toHaveCount(0);

  await page.goto('/products/cordless-hammer-drill-1?salesPlanId=digi-time');
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', /\/products\/cordless-hammer-drill-1$/);
  const underDayCountdown = page.locator('[data-plan-countdown]');
  await expect(underDayCountdown).toBeVisible();
  await expect(underDayCountdown).not.toContainText('روز');
  await expect(underDayCountdown.locator('bdi[dir="ltr"]')).toHaveText(/[۰-۹]{2}:[۰-۹]{2}:[۰-۹]{2}/);
  const selectedPrice = await page.evaluate(() => Number(
    window.HNUI.toEnglishDigits(document.querySelector('#productFinalPrice').textContent).replace(/\D/g, ''),
  ));
  expect(selectedPrice).toBe(23303823);
  await expect(page.locator('.product-sales-plan--fixed')).toContainText('دیجی‌تایم');
  await expect(page.locator('input[name="productSalesPlan"]')).toHaveCount(0);
  await page.locator('#addProduct').click();
  await expect.poll(() => page.evaluate(() => window.HNUI.cart.read()[0]?.price)).toBe(23303823);
  expect(await page.evaluate(() => window.HNUI.cart.read()[0]?.salesPlanId)).toBe('digi-time');
  await expect(page.locator('#cartPanel .cart-item .header-panel-product-link').first()).toHaveAttribute('href', /salesPlanId=digi-time/);
  expect(await page.evaluate(() => window.HNUI.cart.read()[0]?.salesPlanSnapshot?.accessoryPriceAmounts?.['shipping-insurance'])).toBe(245000);

  const digiAfUrl = await page.evaluate(() => {
    const plans = JSON.parse(document.querySelector('#storeSalesPlans').textContent);
    const offer = plans.offers.find(item => item.salesPlanId === 'digi-af');
    return `${window.HNCatalog.url(offer.productId)}?salesPlanId=digi-af`;
  });
  await page.goto(digiAfUrl);
  const overDayCountdown = page.locator('[data-plan-countdown]');
  await expect(overDayCountdown).toContainText('روز');
  await expect(overDayCountdown.locator('bdi[dir="ltr"]')).toHaveText(/[۰-۹]{2}:[۰-۹]{2}:[۰-۹]{2}/);

  await page.clock.setFixedTime(new Date('2027-01-01T00:00:00Z'));
  await page.goto('/products/cordless-hammer-drill-1?salesPlanId=digi-time');
  await expect(page.locator('.product-sales-plans, .product-sales-plan--fixed')).toHaveCount(0);
  const expiredPlanFallbackPrice = await page.evaluate(() => Number(
    window.HNUI.toEnglishDigits(document.querySelector('#productFinalPrice').textContent).replace(/\D/g, ''),
  ));
  expect(expiredPlanFallbackPrice).toBe(regularPrice);
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

test('search keeps one result grid while cart and checkout group by sales plan', async ({page}) => {
  await page.addInitScript(() => localStorage.setItem('hn-demo-auth', '1'));
  await page.clock.install({time: new Date('2026-11-01T10:00:00Z')});
  await page.goto('/search');
  await expect(page.locator('#productResults .search-sales-plan-heading')).toHaveCount(0);
  await expect(page.locator('#productResults [data-product-card]')).toHaveCount(9);
  await noHorizontalOverflow(page);

  await page.evaluate(() => {
    const addOffer = (productId, salesPlanId) => {
      const plans = JSON.parse(document.querySelector('#storeSalesPlans').textContent);
      const offer = plans.offers.find(item => item.productId === productId && item.salesPlanId === salesPlanId);
      const plan = plans.plans.find(item => item.id === salesPlanId);
      const product = window.HNCatalog.find(productId);
      const price = Math.round(offer.priceAmount * (100 - offer.discountPercent) / 100);
      window.HNUI.cart.add({...product, salesPlanId, price, salesPlanSnapshot: {
        id: salesPlanId, name: plan.name, unitAmount: price,
        accessoryPriceAmounts: offer.accessoryPriceAmounts,
      }});
    };
    window.HNUI.cart.add(window.HNCatalog.find(1));
    addOffer(3, 'digi-af');
    addOffer(1, 'digi-time');
  });

  await page.goto('/cart');
  const cartGroups = page.locator('#cartView .cart-plan-group');
  await expect(cartGroups).toHaveCount(3);
  await expect(cartGroups.nth(0)).toHaveAttribute('data-sales-plan-id', 'digi-time');
  await expect(cartGroups.nth(1)).toHaveAttribute('data-sales-plan-id', 'digi-af');
  await expect(cartGroups.nth(2)).toHaveAttribute('data-sales-plan-id', 'regular');
  await noHorizontalOverflow(page);

  await page.goto('/checkout');
  const invoiceGroups = page.locator('#invoiceItems .cart-plan-group');
  await expect(invoiceGroups).toHaveCount(3);
  await expect(invoiceGroups.nth(0)).toHaveAttribute('data-sales-plan-id', 'digi-time');
  await expect(invoiceGroups.nth(1)).toHaveAttribute('data-sales-plan-id', 'digi-af');
  await expect(invoiceGroups.nth(2)).toHaveAttribute('data-sales-plan-id', 'regular');
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

test('favorites and coupon pagination stay usable without mobile or desktop overflow', async ({page}) => {
  await page.addInitScript(() => {
    localStorage.setItem('hn-demo-auth', '1');
    localStorage.setItem('hn-favorites', JSON.stringify(['1', '3', '6', '11']));
  });
  await page.goto('/profile/favorites');
  await expect(page.locator('#favoritesView [data-product-card]').first()).toBeVisible();
  await expect(page.locator('#favoritesView [data-product-card]')).toHaveCount(4);
  await noHorizontalOverflow(page);

  await page.goto('/profile/coupons');
  const coupons = page.locator('#profileCouponsList .profile-coupon-card');
  expect(await page.evaluate(() => window.HNAccountData?.coupons?.length)).toBeGreaterThan(5);
  await expect(coupons.first()).toBeVisible();
  await expect(coupons).toHaveCount(5);
  await page.locator('#profileCouponsPagination [aria-label="صفحه ۲"]').click();
  await expect(page).toHaveURL(/couponPage=2/);
  await expect(page.locator('#profileCouponsTitle')).toBeFocused();
  await expect(coupons).toHaveCount(5);
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
