import {test, expect} from '@playwright/test';

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

test('desktop help menu switches between level-two categories and level-three links', async ({page, isMobile}) => {
  test.skip(isMobile, 'The hierarchy menu is desktop-only; mobile keeps its accordion.');
  await page.goto('/');
  const dropdown = page.locator('#mainNav .quick-links-dropdown:not(.sales-plans-dropdown)');
  await dropdown.locator(':scope > button').click();
  const menu = dropdown.locator('.quick-links-hierarchy-menu');
  await expect(menu).toBeVisible();
  await expect(menu.locator('[data-quick-links-panel="customer"]')).toBeVisible();
  await expect(menu.locator('[data-quick-links-panel="customer"] a')).toHaveCount(5);

  const companyCategory = menu.locator('[data-quick-links-category="company"]');
  await companyCategory.click();
  await expect(companyCategory).toHaveAttribute('aria-expanded', 'true');
  await expect(menu.locator('[data-quick-links-panel="company"]')).toBeVisible();
  await expect(menu.locator('[data-quick-links-panel="company"] a')).toHaveCount(5);
  await expect(menu.locator('[data-quick-links-panel="customer"]')).toBeHidden();
});

test('mobile help links remain in the existing accordion', async ({page, isMobile}) => {
  test.skip(!isMobile, 'The accordion is the mobile navigation pattern.');
  await page.goto('/');
  const drawer = page.locator('#mobileMenu');
  await expect(drawer.locator('.mobile-quick-links-accordion')).toBeAttached();
  await expect(drawer.locator('.quick-links-mobile-group')).toHaveCount(2);
  await expect(page.locator('#mainNav .quick-links-hierarchy-menu')).toBeHidden();
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
