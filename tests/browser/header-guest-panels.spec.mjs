import {test, expect} from '@playwright/test';
import axe from 'axe-core';

async function expectNoA11yViolations(page, state) {
  const results = await page.evaluate(async () => window.axe.run(document, {
    runOnly: {type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']},
  }));
  expect(results.violations.map(issue => ({id: issue.id, nodes: issue.nodes.map(node => node.target)})), state).toEqual([]);
}

const panels = [
  ['#favoritesToggle', '#favoritesPanel'],
  ['#compareToggle', '#comparePanel'],
  ['#cartToggle', '#cartPanel'],
];

test('empty guest header panels stay closed on hover', async ({page, isMobile}) => {
  test.skip(isMobile, 'Hover panels are a desktop interaction.');
  await page.goto('/');
  for (const [toggle, panel] of panels) {
    await page.locator(toggle).hover();
    await expect(page.locator(panel)).not.toHaveClass(/\bopen\b/);
    await expect(page.locator(panel)).toBeHidden();
    await expect(page.locator(toggle)).toHaveAttribute('aria-expanded', 'false');
    await expect(page.locator('#loginModal')).not.toBeVisible();
  }
  await page.locator('#cartToggle').click();
  await expect(page.locator('#loginModal')).toBeVisible();
});

test('mobile account label hides only below its compact-header breakpoint', async ({page}) => {
  for (const width of [320, 390, 575, 576, 767, 768]) {
    await page.setViewportSize({width, height: 900});
    await page.goto('/');
    const label = page.locator('#mainNav .account-btn > .account-label');
    const chevron = page.locator('#mainNav .account-btn > .bi-chevron-down');
    const display = await label.evaluate(element => getComputedStyle(element).display);
    const chevronDisplay = await chevron.evaluate(element => getComputedStyle(element).display);
    expect(display === 'none').toBe(width <= 575);
    expect(chevronDisplay === 'none').toBe(width <= 575);
    const documentWidth = await page.evaluate(() => document.documentElement.scrollWidth);
    expect(documentWidth).toBeLessThanOrEqual(width);
  }
});

test('desktop home brand caret is hidden by its header owner without important', async ({page, isMobile}) => {
  if (isMobile) test.skip();
  for (const width of [1200, 1359, 1360, 1440]) {
    await page.setViewportSize({width, height: 900});
    await page.goto('/');
    const caretDisplay = await page.locator('#mainNav .brand-dropdown > .nav-link').evaluate(element =>
      getComputedStyle(element, '::after').display,
    );
    expect(caretDisplay, `brand caret at ${width}px`).toBe('none');
  }
});

test('desktop help menu presents customer and company links in two columns', async ({page, isMobile}) => {
  test.skip(isMobile, 'The two-column menu is desktop-only; mobile keeps its accordion.');
  await page.goto('/');
  const dropdown = page.locator('#mainNav .quick-links-dropdown:not(.sales-plans-dropdown)');
  await dropdown.locator(':scope > button').click();
  const menu = dropdown.locator('.quick-links-menu');
  await expect(menu).toBeVisible();
  await expect(menu.locator('.quick-links-group')).toHaveCount(2);
  await expect(menu.locator('#quickCustomerLinks')).toBeVisible();
  await expect(menu.locator('#quickCompanyLinks')).toBeVisible();
  await expect(menu.locator('.quick-links-group a')).toHaveCount(10);
});

test('mobile help links remain in the existing accordion', async ({page, isMobile}) => {
  test.skip(!isMobile, 'The accordion is the mobile navigation pattern.');
  await page.goto('/');
  const drawer = page.locator('#mobileMenu');
  await expect(drawer.locator('.mobile-quick-links-accordion')).toBeAttached();
  await expect(drawer.locator('.quick-links-mobile-group')).toHaveCount(2);
  await expect(page.locator('#mainNav .quick-links-dropdown:not(.sales-plans-dropdown) .quick-links-menu')).toBeHidden();
});

test('mobile navigation drawer passes WCAG checks while open', async ({page, isMobile}) => {
  test.skip(!isMobile, 'The navigation drawer is a mobile interaction.');
  await page.addInitScript(axe.source);
  await page.goto('/');
  await page.locator('#mainNav .navbar-toggler').click();
  await expect(page.locator('#mobileMenu')).toBeVisible();
  await expectNoA11yViolations(page, 'mobile navigation drawer');
});

test('mobile drawer category icons keep their 31px layout in their component owner', async ({page, isMobile}) => {
  test.skip(!isMobile, 'Category icon sizing is specific to the mobile drawer.');
  await page.goto('/');
  await page.locator('#mainNav .navbar-toggler').click();
  const drawer = page.locator('#mobileMenu');
  await expect(drawer).toBeVisible();
  const icon = drawer.locator('.mobile-category-toggle > span > i').first();
  await expect(icon).toBeVisible();
  const layout = await icon.evaluate(element => {
    const style = getComputedStyle(element);
    return {
      display: style.display,
      width: style.width,
      height: style.height,
      placeItems: style.placeItems,
      borderRadius: style.borderRadius,
      fontSize: style.fontSize,
      boxShadow: style.boxShadow,
      marginLeft: style.marginLeft,
    };
  });
  expect(layout).toEqual({
    display: 'grid',
    width: '31px',
    height: '31px',
    placeItems: 'center',
    borderRadius: '9px',
    fontSize: '15px',
    boxShadow: 'none',
    marginLeft: '0px',
  });
});

test('guest can preview nonempty header panels on hover', async ({page, isMobile}) => {
  test.skip(isMobile, 'Hover panels are a desktop interaction.');
  await page.addInitScript(() => {
    localStorage.setItem('hn-favorites', JSON.stringify(['1']));
    localStorage.setItem('hn-compare', JSON.stringify(['1']));
    localStorage.setItem('hadi-norouzi-cart', JSON.stringify([{id: '1', name: 'Product', price: 100, quantity: 1}]));
  });
  await page.goto('/');
  for (const [toggle, panel] of panels) {
    await page.locator(toggle).hover();
    await expect(page.locator(panel)).toHaveClass(/\bopen\b/);
    await expect(page.locator(toggle)).toHaveAttribute('aria-expanded', 'true');
    await expect(page.locator('#loginModal')).not.toBeVisible();
  }
});

test('header cart footer stays compact and preserves room for the item list', async ({page, isMobile}) => {
  await page.addInitScript(() => {
    localStorage.setItem('hadi-norouzi-cart', JSON.stringify([{id: '1', name: 'Product', price: 100, quantity: 1}]));
  });
  await page.goto('/');
  await page.locator('#cartToggle').hover();

  const layout = await page.locator('#cartPanel').evaluate(panel => ({
    listMaxHeight: getComputedStyle(panel.querySelector('.cart-items')).maxHeight,
    summaryPadding: getComputedStyle(panel.querySelector('.cart-total-summary')).padding,
    summaryGap: getComputedStyle(panel.querySelector('.cart-total-summary')).rowGap,
    checkoutMinHeight: getComputedStyle(panel.querySelector('.cart-footer > a')).minHeight,
  }));
  expect(layout).toEqual({
    listMaxHeight: isMobile ? 'none' : '380px',
    summaryPadding: '8px 11px',
    summaryGap: '5px',
    checkoutMinHeight: '46px',
  });
});

test('touch header cart controls have 44px targets without panel overflow', async ({page}) => {
  await page.addInitScript(() => {
    localStorage.setItem('hadi-norouzi-cart', JSON.stringify([{id: '1', name: 'Product', price: 100, quantity: 2}]));
  });

  for (const width of [320, 390, 768, 991]) {
    await page.setViewportSize({width, height: 900});
    await page.goto('/');
    await page.evaluate(() => {
      document.querySelector('#cartWrap')?.classList.add('open');
      document.querySelector('#cartPanel')?.classList.add('open');
    });
    const controls = page.locator('#cartPanel .cart-item .hn-cart-controls');
    await expect(controls).toBeVisible();
    const metrics = await page.evaluate(() => {
      const panel = document.querySelector('#cartPanel');
      const buttons = [...panel.querySelectorAll('.hn-cart-controls button')];
      return {
        buttons: buttons.map(button => {
          const rect = button.getBoundingClientRect();
          return {width: rect.width, height: rect.height};
        }),
        panelWidth: panel.clientWidth,
        panelScrollWidth: panel.scrollWidth,
        documentWidth: document.documentElement.scrollWidth,
        viewportWidth: window.innerWidth,
      };
    });

    expect(metrics.buttons).toHaveLength(3);
    for (const button of metrics.buttons) {
      expect(button.width).toBeGreaterThanOrEqual(44);
      expect(button.height).toBeGreaterThanOrEqual(44);
    }
    expect(metrics.panelScrollWidth).toBeLessThanOrEqual(metrics.panelWidth);
    expect(metrics.documentWidth).toBeLessThanOrEqual(metrics.viewportWidth);
  }
});
