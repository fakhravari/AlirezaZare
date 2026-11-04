import {test, expect} from '@playwright/test';

test('FAQ answer and contact validation fit the viewport', async ({page, isMobile}) => {
  if (isMobile) await page.setViewportSize({width: 320, height: 640});
  await page.goto('/faq');
  const item = page.locator('.faq-item').first();
  await item.locator('summary').click();
  await expect(item).toHaveAttribute('open', '');
  await expect(item.locator('.faq-answer')).toBeVisible();
  const faqWidth = await page.evaluate(() => ({content: document.documentElement.scrollWidth, viewport: innerWidth}));
  expect(faqWidth.content).toBeLessThanOrEqual(faqWidth.viewport + 1);

  await page.goto('/contact');
  await page.locator('#contactForm [type="submit"]').click();
  await expect(page.locator('#contactStatus')).toBeVisible();
  await expect(page.locator('#contactName')).toHaveAttribute('aria-invalid', 'true');
  const contactWidth = await page.evaluate(() => ({content: document.documentElement.scrollWidth, viewport: innerWidth}));
  expect(contactWidth.content).toBeLessThanOrEqual(contactWidth.viewport + 1);
});
