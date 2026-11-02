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
