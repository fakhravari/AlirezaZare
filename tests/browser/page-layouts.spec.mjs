import {test, expect} from '@playwright/test';

const paths = [
  '/', '/search', '/products/cordless-hammer-drill-1', '/cart', '/checkout',
  '/compare', '/favorites', '/profile', '/profile/orders', '/profile/addresses',
  '/profile/favorites', '/profile/coupons', '/brands', '/brands/bosch',
  '/magazine', '/magazine/multimeter-symbols-guide', '/contact', '/about',
  '/faq', '/shipping', '/warranty', '/tracking', '/legal/terms',
  '/legal/privacy', '/legal/returns',
];

test('main pages fit mobile, tablet and desktop widths', async ({page, isMobile}) => {
  test.setTimeout(180000);
  await page.addInitScript(() => localStorage.setItem('hn-demo-auth', '1'));
  const widths = isMobile ? [320, 768] : [1440];
  for (const width of widths) {
    await page.setViewportSize({width, height: 800});
    for (const path of paths) {
      const response = await page.goto(path, {waitUntil: 'domcontentloaded'});
      expect(response.status(), path).toBe(200);
      await expect(page.locator('main').first(), path).toBeVisible();
      const dimensions = await page.evaluate(() => ({content: document.documentElement.scrollWidth, viewport: innerWidth}));
      expect(dimensions.content, `${path} at ${width}px`).toBeLessThanOrEqual(dimensions.viewport + 1);
    }
  }
});
