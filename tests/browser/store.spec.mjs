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

test('hidden home category submenu opens on hover and hides on pointer leave', async ({page, isMobile}) => {
  test.skip(isMobile, 'The desktop category submenu is not rendered on mobile.');
  await page.goto('/');
  const submenu = page.locator('#electricalSubmenu');
  const firstCategory = page.locator('.electrical-category-menu button[data-category]').first();
  await expect(submenu).toBeHidden();
  await expect(submenu).toHaveCSS('display', 'none');
  await firstCategory.hover();
  await expect(submenu).toBeVisible();
  await expect(submenu).not.toHaveAttribute('hidden', '');
  const openDisplay = await submenu.evaluate(element => getComputedStyle(element).display);
  expect(openDisplay).toBe('grid');
  await page.mouse.move(1400, 900);
  await expect(submenu).toBeHidden();
  await expect(submenu).toHaveCSS('display', 'none');
});
test('inner category mega menu is hidden while navigation is collapsed and opens at 1200px', async ({page, isMobile}) => {
  test.skip(isMobile, 'The inner category mega menu is desktop-only.');
  await page.setViewportSize({width: 1199, height: 900});
  await page.goto('/magazine/choosing-work-drill');
  const menu = page.locator('#mainNav .category-nav-mega');
  const trigger = page.locator('#mainNav .category-nav-dropdown > .nav-link');
  await expect(menu).toBeHidden();

  await page.setViewportSize({width: 1200, height: 900});
  await trigger.click();
  await expect(menu).toBeVisible();
  await expect(menu).toHaveCSS('display', 'grid');
  await expect(menu.locator('.category-mega-sidebar > button').first()).toHaveCSS('border-radius', '8px');
  await expect(menu.locator('.category-mega-group > a:not(.category-mega-title)').first()).toHaveCSS('color', 'rgb(82, 101, 121)');
});
test('header toggle uses shared owner colors on mobile and tablet home and inner pages', async ({page, isMobile}) => {
  test.skip(isMobile, 'This regression sets both phone and tablet viewport widths.');
  for (const route of ['/', '/magazine/choosing-work-drill']) {
    await page.setViewportSize({width: 320, height: 844});
    await page.goto(route);
    const toggle = page.locator('#mainNav .navbar-toggler');
    const brand = page.locator('#mainNav .header-brand-lockup');
    for (const width of [320, 390, 768, 991]) {
      await page.setViewportSize({width, height: 844});
      await expect(toggle).toHaveCSS('border-width', '0px');
      await expect(toggle).toHaveCSS('background-color', 'rgba(255, 255, 255, 0.08)');
      await expect(toggle).toHaveCSS('color', 'rgb(246, 197, 21)');
      await expect(brand).toHaveCSS('display', 'none');
    }
    for (const width of [992, 1024, 1199]) {
      await page.setViewportSize({width, height: 900});
      await expect(toggle).toHaveCSS('border-width', '0px');
      await expect(toggle).toHaveCSS('background-color', 'rgb(255, 255, 255)');
      await expect(toggle).toHaveCSS('color', 'rgb(16, 42, 67)');
      await expect(brand).toBeVisible();
    }
  }
});
test('desktop and tablet headers keep actions inside the viewport around navigation breakpoints', async ({page, isMobile}) => {
  test.skip(isMobile, 'This regression covers tablet and desktop navigation.');
  for (const route of ['/', '/magazine/choosing-work-drill']) {
    await page.setViewportSize({width: 992, height: 900});
    await page.goto(route);
    for (const width of [992, 1024, 1100, 1199, 1200, 1280, 1359, 1360, 1440]) {
      await page.setViewportSize({width, height: 900});
      const layout = await page.evaluate(() => {
        const actions = document.querySelector('#mainNav .home-nav-action-group').getBoundingClientRect();
        const toggler = document.querySelector('#mainNav .navbar-toggler');
        const menu = document.querySelector('#mainNav .home-nav-menu-group');
        return {
          actionsLeft: actions.left,
          actionsRight: actions.right,
          togglerVisible: getComputedStyle(toggler).display !== 'none',
          menuVisible: getComputedStyle(menu).display !== 'none',
          viewport: innerWidth,
          document: document.documentElement.scrollWidth,
        };
      });
      expect(layout.actionsLeft, `${route} at ${width}px`).toBeGreaterThanOrEqual(-1);
      expect(layout.actionsRight, `${route} at ${width}px`).toBeLessThanOrEqual(width + 1);
      expect(layout.document, `${route} at ${width}px`).toBeLessThanOrEqual(width + 1);
      expect(layout.togglerVisible, `${route} at ${width}px`).toBe(width < 1200);
      expect(layout.menuVisible, `${route} at ${width}px`).toBe(width >= 1200);
    }
  }
  await page.setViewportSize({width: 1024, height: 900});
  await page.goto('/');
  await page.locator('#mainNav .navbar-toggler').click();
  await expect(page.locator('#mobileMenu')).toBeVisible();
});
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
  await expect(page.locator('#homeSalesPlans .home-sales-plan-grid .product-card')).toHaveCount(16);
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
      decoration: getComputedStyle(header, '::before').content,
    }));
    if (isMobile) {
      expect(planHeader.background).toBe('none');
      expect(planHeader.radius).toBe('0px');
    } else {
      expect(planHeader.background).toContain('linear-gradient');
      expect(planHeader.radius).toBe('18px');
    }
    expect(planHeader.titleColor).toBe('rgb(16, 42, 67)');
    expect(planHeader.decoration).toBe('\"\"');
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
    await expect(product.locator('.product-sales-plan-badge')).toHaveText(await product.getAttribute('data-sales-plan-name'));
    const countdown = section.locator('.home-sales-plan-countdown');
    await expect(countdown).toBeVisible();
    const planLabel = await page.evaluate(id => JSON.parse(document.querySelector('#storeSalesPlans').textContent).plans.find(plan => plan.id === id).label, planId);
    const mobileCountdownDescription = countdown.locator('.home-sales-plan-countdown-description');
    const headerDescription = section.locator('.home-sales-plan-header-description');
    await expect(headerDescription).toHaveText(planLabel);
    await expect(mobileCountdownDescription).toHaveText(planLabel);
    if (isMobile) {
      await expect(mobileCountdownDescription).toBeVisible();
      await expect(headerDescription).toBeHidden();
      const mobileHeaderPositions = await section.locator('.category-showcase-head').evaluate(header => {
        const title = header.querySelector('h2').getBoundingClientRect();
        const action = header.querySelector(':scope > a').getBoundingClientRect();
        const countdown = header.querySelector('.home-sales-plan-countdown').getBoundingClientRect();
        return {titleLeft: title.left, actionRight: action.right, titleTop: title.top, actionTop: action.top, countdownTop: countdown.top, countdownRight: countdown.right, actionLeft: action.left};
      });
      expect(mobileHeaderPositions.actionRight).toBeLessThan(mobileHeaderPositions.titleLeft);
      expect(mobileHeaderPositions.actionTop).toBeLessThan(mobileHeaderPositions.countdownTop);
      expect(mobileHeaderPositions.countdownRight).toBeGreaterThan(mobileHeaderPositions.actionLeft);
      const countdownLayout = await countdown.evaluate(element => {
        const label = element.querySelector('.home-sales-plan-countdown-label').getBoundingClientRect();
        const description = element.querySelector('.home-sales-plan-countdown-description').getBoundingClientRect();
        const time = element.querySelector('time').getBoundingClientRect();
        return {labelTop: label.top, descriptionTop: description.top, labelLeft: label.left, labelRight: label.right, descriptionRight: description.right, timeTop: time.top, timeLeft: time.left, timeRight: time.right};
      });
      expect(Math.abs(countdownLayout.timeTop - countdownLayout.labelTop)).toBeLessThan(4);
      expect(countdownLayout.descriptionTop).toBeGreaterThan(countdownLayout.labelTop);
      expect(countdownLayout.timeRight).toBeLessThan(countdownLayout.labelLeft);
      expect(Math.abs(countdownLayout.descriptionRight - countdownLayout.labelRight)).toBeLessThan(4);
    } else {
      await expect(mobileCountdownDescription).toBeHidden();
      await expect(headerDescription).toBeVisible();
    }
    await expect(countdown).toHaveCSS('border-radius', '15px');
    await expect(countdown.locator('time')).toHaveCSS('font-weight', '900');
    const countdownTime = countdown.locator('time');
    await expect(countdownTime).toHaveCSS('font-weight', '900');
    await expect(countdownTime).toHaveCSS('font-size', isMobile && planId === 'digi-time' ? '16px' : '17px');
    const remaining = await page.evaluate(id => {
      const plans = JSON.parse(document.querySelector('#storeSalesPlans').textContent).plans;
      return Date.parse(plans.find(plan => plan.id === id).endsAtUtc) - Date.now();
    }, planId);
    if (remaining >= 86400000) await expect(countdownTime).toContainText('روز');
    else await expect(countdownTime).not.toContainText('روز');
    await expect(countdownTime.locator('bdi[dir="ltr"]')).toHaveText(/[۰-۹]{2}:[۰-۹]{2}:[۰-۹]{2}/);
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

test('homepage magazine and purchase benefits keep their layout across breakpoints', async ({page}) => {
  for (const width of [320, 380, 381, 390, 640, 767, 768, 991, 992, 1199, 1200]) {
    await page.setViewportSize({width, height: 900});
    await page.goto('/');
    await expect(page.locator('#services')).toHaveClass(/purchase-benefits/);
    expect(await page.locator('#services .service, #services.services').count()).toBe(0);
    const layout = await page.evaluate(() => {
      const list = document.querySelector('#journal .journal-list');
      const title = list.querySelector('h4');
      const stats = list.querySelector('.journal-list-stats');
      const benefits = document.querySelector('#services .benefits-intro p');
      return {
        listDisplay: getComputedStyle(list).display,
        listColumns: getComputedStyle(list).gridTemplateColumns.split(' ').length,
        listOverflow: getComputedStyle(list).overflow,
        titleFontSize: getComputedStyle(title).fontSize,
        titleWhiteSpace: getComputedStyle(title).whiteSpace,
        statsGridArea: getComputedStyle(stats).gridArea,
        benefitsWhiteSpace: getComputedStyle(benefits).whiteSpace
      };
    });
    expect(layout).toMatchObject({
      listDisplay: 'grid',
      listColumns: 1,
      listOverflow: 'visible',
      titleFontSize: '15px',
      titleWhiteSpace: 'normal',
      statsGridArea: 'reading',
      benefitsWhiteSpace: width <= 1199 ? 'normal' : 'nowrap'
    });
  }
});

test('special-plan grid spacing and columns match the regular home grid across breakpoints', async ({page}) => {
  await page.clock.install({time: new Date('2026-11-01T10:00:00Z')});
  for (const [width, expectedColumns] of [[390, 2], [820, 3], [980, 4], [1280, 5], [1366, 5], [1440, 6], [1792, 6]]) {
    await page.setViewportSize({width, height: 900});
    await page.goto('/');
    await expect(page.locator('#homeSalesTime .home-sales-plan-grid')).toBeVisible();
    const gutters = await page.evaluate(() => {
      if (innerWidth >= 1200) document.body.classList.remove('mobile-page--home-index');
      const normal = getComputedStyle(document.querySelector('#productGrid'));
      const special = getComputedStyle(document.querySelector('#homeSalesTime .home-sales-plan-grid'));
      const rowBounds = selector => {
        const grid = document.querySelector(selector);
        const firstTop = grid.children[0].getBoundingClientRect().top;
        const cards = [...grid.children].filter(column => Math.abs(column.getBoundingClientRect().top - firstTop) < 1).map(column => column.querySelector('.product-card').getBoundingClientRect());
        return {columns: cards.length, left: Math.min(...cards.map(card => card.left)), right: Math.max(...cards.map(card => card.right))};
      };
      const collection = document.querySelector('#collection > .container').getBoundingClientRect();
      const specialPanel = document.querySelector('#homeSalesPlans .home-sales-plan').getBoundingClientRect();
      const specialHeader = document.querySelector('#homeSalesTime .home-sales-plan .category-showcase-head').getBoundingClientRect();
      return {mobileHome: document.body.classList.contains('mobile-page--home-index'), normal: [normal.getPropertyValue('--bs-gutter-x').trim(), normal.getPropertyValue('--bs-gutter-y').trim()], special: [special.getPropertyValue('--bs-gutter-x').trim(), special.getPropertyValue('--bs-gutter-y').trim()], regularBounds: rowBounds('#productGrid'), specialBounds: rowBounds('#homeSalesTime .home-sales-plan-grid'), specialHeader: {left: specialHeader.left, right: specialHeader.right}, collectionPanel: {left: collection.left, right: collection.right}, specialPanel: {left: specialPanel.left, right: specialPanel.right}};
    });
    expect(gutters.special).toEqual(gutters.normal);
    expect(gutters.specialBounds.columns).toBe(expectedColumns);
    if (width <= 767 && gutters.mobileHome) {
      expect(gutters.specialBounds.left).toBeCloseTo(gutters.regularBounds.left + 4, 0);
      expect(gutters.specialBounds.right).toBeCloseTo(gutters.regularBounds.right - 4, 0);
    } else {
      expect(gutters.specialBounds.left).toBeCloseTo(gutters.regularBounds.left, 0);
      expect(gutters.specialBounds.right).toBeCloseTo(gutters.regularBounds.right, 0);
    }
    expect(gutters.specialPanel.left).toBeCloseTo(gutters.collectionPanel.left, 0);
    expect(gutters.specialPanel.right).toBeCloseTo(gutters.collectionPanel.right, 0);
  }
});

test('mobile homepage section containers keep an even eight-pixel outer gutter', async ({page, isMobile}) => {
  test.skip(!isMobile, 'The page-wide mobile container contract is specific to the mobile layout.');
  for (const width of [320, 360, 390, 430, 767]) {
    await page.setViewportSize({width, height: 900});
    await page.goto('/');
    await expect(page.locator('#homeSalesTime > .container')).toBeVisible();
    const gutters = await page.evaluate(() => ['#homeSalesTime > .container', '#collection > .container'].map(selector => {
      const rect = document.querySelector(selector).getBoundingClientRect();
      return {left: rect.left, right: innerWidth - rect.right, width: rect.width};
    }));
    for (const gutter of gutters) {
      expect(gutter.left).toBeCloseTo(8, 0);
      expect(gutter.right).toBeCloseTo(8, 0);
      expect(gutter.width).toBeCloseTo(width - 16, 0);
    }
  }
});

test('search initially displays 15 products', async ({page}) => {
  await page.goto('/Search');
  await expect(page.locator('#productResults [data-product-card]')).toHaveCount(15);
});

test('cart address preview stays empty before the first address is added', async ({page}) => {
  await page.addInitScript(() => {
    localStorage.setItem('hn-demo-auth', '1');
    localStorage.setItem('hn-delivery-addresses', '[]');
    localStorage.removeItem('hn-selected-address');
  });
  await page.goto('/search');
  await page.evaluate(() => window.HNUI.cart.add(window.HNCatalog.find(1)));
  await page.goto('/cart');
  await expect(page.locator('#addressPreview')).toBeEmpty();
  await expect(page.locator('.cart-address-section button[aria-label*="آدرس"]')).toBeVisible();
  await expect(page.locator('#cartView')).not.toContainText('برای ثبت اولین آدرس، دکمه + را بزنید.');
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
  await expect(offerCard.locator('.product-sales-plan-badge')).toHaveText(listing.plan.name);
  const expectedPrice = Math.round(listing.offer.priceAmount * (100 - listing.offer.discountPercent) / 100);
  await expect(offerCard.locator('.add-cart')).toHaveAttribute('data-price', String(expectedPrice));
  await offerCard.locator('.add-cart').click();
  await expect.poll(() => page.evaluate(() => window.HNUI.cart.read().find(item => item.salesPlanId === 'digi-time')?.price)).toBe(expectedPrice + listing.offer.accessoryPriceAmounts['shipping-insurance']);
});

test('distant sales-plan expiry does not repeatedly rebuild search results', async ({page}) => {
  // Keep the demo plan active with its expiry more than 24.8 days away.
  await page.addInitScript(() => { Date.now = () => Date.parse('2026-11-01T10:00:00Z'); });
  await page.goto('/search?sales-plan=digi-time');
  const result = page.locator('#productResults [data-sales-plan-id="digi-time"]').first();
  await expect(result).toBeVisible();
  const originalCard = await result.elementHandle();
  await page.waitForTimeout(800);
  expect(await originalCard.evaluate(element => element.isConnected)).toBe(true);
});

test('search shares header query rules, filters on final plan price, and removes an offer at expiry', async ({page}, testInfo) => {
  await page.goto('/search');
  const expiry = await page.evaluate(() => Date.parse(JSON.parse(document.querySelector('#storeSalesPlans').textContent).plans.find(plan => plan.id === 'digi-time').endsAtUtc));
  await page.clock.install({time: new Date(expiry - 60000)});
  await page.reload();
  await page.locator('#productSearch').fill('DIGI-TIME');
  await page.clock.runFor(300);
  const planResults = page.locator('#productResults [data-sales-plan-id="digi-time"]');
  await expect(planResults.first()).toBeVisible();
  await page.locator('input[name="productSort"][value="low"]').evaluate(input => { input.checked = true; input.dispatchEvent(new Event('change', {bubbles: true})); });
  const orderedPlanPrices = await page.locator('#productResults [data-sales-plan-id="digi-time"] .add-cart').evaluateAll(buttons => buttons.map(button => Number(button.dataset.price)));
  expect(orderedPlanPrices).toEqual([...orderedPlanPrices].sort((a, b) => a - b));
  await page.evaluate(() => {
    const maxPrice = document.querySelector('#maxPrice');
    maxPrice.value = '24';
    maxPrice.dispatchEvent(new Event('input', {bubbles: true}));
  });
  await expect(planResults.first()).toBeVisible();
  const discountedDrill = page.locator('#productResults [data-product-id="1"][data-sales-plan-id="digi-time"]');
  await expect(discountedDrill).toBeVisible();
  await expect(discountedDrill.locator('.add-cart')).toHaveAttribute('data-price', '23058823');

  await page.clock.fastForward(61000);
  await expect(page.locator('#productResults [data-sales-plan-id="digi-time"]')).toHaveCount(0);

  if (testInfo.project.name === 'desktop') {
    await page.clock.setSystemTime(new Date(expiry - 60000));
    await page.goto('/');
    const headerInput = page.locator('.home-header-search .header-search-input');
    await headerInput.fill('DIGI-TIME');
    const suggestions = page.locator('.header-search-suggestions');
    await page.clock.runFor(300);
    await expect(suggestions).toBeVisible();
    await expect(suggestions.locator('.header-search-plan-badge').first()).toContainText('دیجی');
    await suggestions.locator('a.header-search-all').click();
    await expect(page).toHaveURL(/\/Search\?q=DIGI-TIME/i);
    await expect(page.locator('#productResults [data-sales-plan-id="digi-time"]').first()).toBeVisible();

    await page.goto('/');
    const reopenedHeaderInput = page.locator('.home-header-search .header-search-input');
    const reopenedSuggestions = page.locator('.header-search-suggestions');
    await reopenedHeaderInput.fill('DIGI-TIME');
    await page.clock.runFor(300);
    await expect(reopenedSuggestions.locator('.header-search-plan-badge').first()).toBeVisible();
    await reopenedSuggestions.hover();
    await page.clock.fastForward(61000);
    await expect(reopenedSuggestions.locator('.header-search-plan-badge')).toHaveCount(0);
  }
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

test('desktop sales plan menu keeps the same layout on home and inner pages', async ({page, isMobile}) => {
  test.skip(isMobile);
  await page.addInitScript(() => localStorage.setItem('hn-demo-auth', '1'));

  const readMenuLayout = async path => {
    await page.goto(path);
    await page.locator('.sales-plans-dropdown > button').click();
    const menu = page.locator('.sales-plan-menu');
    await expect(menu).toBeVisible();
    return menu.evaluate(element => {
      const style = getComputedStyle(element);
      return {
        width: style.width,
        columns: style.gridTemplateColumns,
        padding: style.padding,
        borderRadius: style.borderRadius,
      };
    });
  };

  const homeLayout = await readMenuLayout('/');
  const innerLayout = await readMenuLayout('/cart');
  expect(innerLayout).toEqual(homeLayout);
  expect(homeLayout.columns.trim().split(/\s+/)).toHaveLength(1);
});

test('mobile header search suggestions use readable type sizes', async ({page, isMobile}) => {
  test.skip(!isMobile);
  await page.goto('/');
  await page.locator('#mainNav .navbar-toggler').click();
  const suggestions = page.locator('#mobileMenuSearchSuggestions');
  await expect(suggestions).toBeHidden();
  await expect(suggestions).toHaveCSS('display', 'none');
  const input = page.locator('.mobile-menu-search .header-search-input');
  await input.fill('DIGI-TIME');
  await expect(suggestions).toBeVisible();
  const title = page.locator('.mobile-menu-search-suggestion-copy > strong').first();
  await expect(title).toBeVisible();
  const fontSizes = await page.locator('.mobile-menu-search-suggestion-copy').first().evaluate(copy => ({
    title: getComputedStyle(copy.querySelector(':scope > strong')).fontSize,
    details: getComputedStyle(copy.querySelector(':scope > small')).fontSize,
    badge: getComputedStyle(copy.querySelector('.mobile-menu-search-plan-badge')).fontSize,
    price: getComputedStyle(copy.querySelector(':scope > b')).fontSize,
  }));
  expect(fontSizes).toEqual({title: '13px', details: '11px', badge: '10px', price: '12px'});
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

  const availabilityGroup = page.locator('.filter-group[data-filter-slug="availability"]');
  await availabilityGroup.locator(':scope > button').click();
  const availability = page.locator('input[name="availability"]');
  await expect(availability).toHaveCount(3);
  await expect(page.locator('input[name="availability"][value="all"]')).toBeChecked();
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

test('product detail shows only its URL plan without mixing browser and server clocks', async ({page}) => {
  await page.addInitScript(() => localStorage.setItem('hn-demo-auth', '1'));
  await page.goto('/products/cordless-hammer-drill-1');
  await expect(page.locator('.product-sales-plans, .product-special-sale')).toHaveCount(0);
  const regularPrice = await page.evaluate(() => Number(
    window.HNUI.toEnglishDigits(document.querySelector('#productFinalPrice').textContent).replace(/\D/g, ''),
  ));
  expect(regularPrice).toBe(24850000);

  const digiTimeEndsAt = await page.evaluate(() => Date.parse(JSON.parse(document.querySelector('#storeSalesPlans').textContent).plans.find(plan => plan.id === 'digi-time').endsAtUtc));
  if (Date.now() >= digiTimeEndsAt) {
    await page.goto('/products/cordless-hammer-drill-1?salesPlanId=digi-time');
    await expect(page).not.toHaveURL(/salesPlanId=/);
    await expect(page.locator('.product-sales-plans, .product-special-sale')).toHaveCount(0);
    return;
  }

  await page.goto('/products/cordless-hammer-drill-1?salesPlanId=regular');
  await expect(page).not.toHaveURL(/salesPlanId=/);
  await expect(page.locator('.product-sales-plans, .product-special-sale')).toHaveCount(0);

  await page.goto('/products/cordless-hammer-drill-1?salesPlanId=digi-time');
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', /\/products\/cordless-hammer-drill-1$/);
  const underDayCountdown = page.locator('[data-plan-countdown]');
  await expect(underDayCountdown).toBeVisible();
  await expect(underDayCountdown.locator('bdi[dir="ltr"]')).toHaveText(/[۰-۹]{2}:[۰-۹]{2}:[۰-۹]{2}/);
  const selectedPrice = await page.evaluate(() => Number(
    window.HNUI.toEnglishDigits(document.querySelector('#productFinalPrice').textContent).replace(/\D/g, ''),
  ));
  expect(selectedPrice).toBe(23303823);
  await expect(page.locator('.product-special-sale')).toContainText('دیجی‌تایم');
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
  await expect(overDayCountdown.locator('bdi[dir="ltr"]')).toHaveText(/[۰-۹]{2}:[۰-۹]{2}:[۰-۹]{2}/);

  await noHorizontalOverflow(page);
});

test('products show their own plan-specific features, accessory choices and prices', async ({page}) => {
  const samples = [
    {path: '/products/cordless-hammer-drill-1', feature: 'نوع سه‌نظام', time: 'sds-bit-set', af: 'drill-case'},
    {path: '/products/digital-multimeter-6', feature: 'نوع نمایش', time: 'probe-set', af: 'meter-test-lead'},
    {path: '/products/digital-soldering-station-11', feature: 'کنترل فرایند', time: 'solder-tips', af: 'solder-stand-kit'},
  ];
  const readPlanDetails = async (sample, planId) => {
    await page.goto(`${sample.path}?salesPlanId=${planId}`);
    await expect(page.locator('.product-special-sale')).toContainText(planId === 'digi-time' ? 'دیجی‌تایم' : 'دیجی‌اف');
    return page.evaluate((feature) => ({
      specification: [...document.querySelectorAll('#product-specifications dt')]
        .find(item => item.textContent.trim() === feature)?.nextElementSibling?.textContent.trim(),
      accessories: [...document.querySelectorAll('.product-accessory')]
        .map(item => ({id: item.querySelector('[data-accessory-id]').dataset.accessoryId,
          price: item.querySelector('.product-accessory-price strong').textContent.trim()})),
    }), sample.feature);
  };

  for (const sample of samples) {
    const digiTime = await readPlanDetails(sample, 'digi-time');
    const digiAf = await readPlanDetails(sample, 'digi-af');
    expect(digiTime.specification).toBeTruthy();
    expect(digiAf.specification).toBeTruthy();
    expect(digiAf.specification).not.toBe(digiTime.specification);
    expect(digiTime.accessories.map(item => item.id)).toContain(sample.time);
    expect(digiTime.accessories.map(item => item.id)).not.toContain(sample.af);
    expect(digiAf.accessories.map(item => item.id)).toContain(sample.af);
    expect(digiAf.accessories.map(item => item.id)).not.toContain(sample.time);
    const timeAccessory = digiTime.accessories.find(item => item.id === sample.time);
    const afAccessory = digiAf.accessories.find(item => item.id === sample.af);
    expect(afAccessory.price).not.toBe(timeAccessory.price);
  }

  const invalidAccessory = await page.evaluate(async () => {
    const response = await fetch('/api/store/sales-plans/quote', {
      method: 'POST', headers: {'Content-Type': 'application/json'},
      body: JSON.stringify({productId: 1, salesPlanId: 'digi-time', quantity: 1,
        variants: {battery: '2ah', chuck: '10mm'}, accessoryIds: ['shipping-insurance', 'drill-case']}),
    });
    return {status: response.status, body: await response.json()};
  });
  expect(invalidAccessory.status).toBe(400);
  expect(invalidAccessory.body.code).toBe('invalid-product-options');
});

test('cart opens preview after address selection even if validation fails', async ({page}) => {
  await page.addInitScript(() => {
    localStorage.setItem('hn-demo-auth', '1');
    localStorage.setItem('hn-delivery-addresses', JSON.stringify([{id: 'test-address', title: 'Home', recipient: 'Test User', phone: '09123456789', province: 'Test', city: 'Test', street: 'Test street', postal: '1234567890'}]));
    localStorage.setItem('hn-selected-address', 'test-address');
  });
  await page.route('**/api/store/sales-plans/validate-cart', route => route.fulfill({status: 409, contentType: 'application/json', body: JSON.stringify({code: 'unavailable'})}));
  await page.goto('/search');
  await page.evaluate(() => window.HNUI.cart.add(window.HNCatalog.find(1)));
  await page.goto('/cart');
  const link = page.locator('#cartView .cart-checkout-link');
  await expect(link).toHaveAttribute('aria-disabled', 'false');
  await link.click();
  await expect(page).toHaveURL(/\/checkout/i);
  await expect(page.locator('#finalPayment')).toBeDisabled();
  await expect(page.locator('#paymentStatus')).toContainText('قیمت یا موجودی');
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

test('cart storage changes sync without reloading the page', async ({page, context}) => {
  await page.addInitScript(() => {
    localStorage.setItem('hn-demo-auth', '1');
    window.__cartPageBootId = crypto.randomUUID();
  });
  await page.goto('/cart');
  const bootId = await page.evaluate(() => window.__cartPageBootId);
  const navigations = [];
  page.on('framenavigated', frame => {
    if (frame === page.mainFrame()) navigations.push(frame.url());
  });

  const otherTab = await context.newPage();
  await otherTab.goto('/');
  await otherTab.evaluate(() => localStorage.setItem('hn-favorites', JSON.stringify(['1'])));
  await otherTab.evaluate(() => window.HNUI.cart.add(window.HNCatalog.find(1)));

  await expect(page.locator('#cartView .cart-product-row')).toHaveCount(1);
  expect(await page.evaluate(() => window.__cartPageBootId)).toBe(bootId);
  expect(navigations).toEqual([]);
  await otherTab.close();
});

test('search keeps one result grid while cart and checkout group by sales plan', async ({page}) => {
  await page.addInitScript(() => localStorage.setItem('hn-demo-auth', '1'));
  await page.clock.install({time: new Date('2026-11-01T10:00:00Z')});
  await page.goto('/search');
  await expect(page.locator('#productResults .search-sales-plan-heading')).toHaveCount(0);
  await expect(page.locator('#productResults [data-product-card]')).toHaveCount(15);
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
  await expect(cartGroups.nth(0)).toHaveAttribute('data-sales-plan-id', 'regular');
  await expect(cartGroups.nth(1)).toHaveAttribute('data-sales-plan-id', 'digi-time');
  await expect(cartGroups.nth(2)).toHaveAttribute('data-sales-plan-id', 'digi-af');
  await noHorizontalOverflow(page);

  await page.goto('/checkout');
  const invoiceGroups = page.locator('#invoiceItems .cart-plan-group');
  await expect(invoiceGroups).toHaveCount(3);
  await expect(invoiceGroups.nth(0)).toHaveAttribute('data-sales-plan-id', 'regular');
  await expect(invoiceGroups.nth(1)).toHaveAttribute('data-sales-plan-id', 'digi-time');
  await expect(invoiceGroups.nth(2)).toHaveAttribute('data-sales-plan-id', 'digi-af');
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
