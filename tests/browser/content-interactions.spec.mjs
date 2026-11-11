import {test, expect} from '@playwright/test';

test('magazine cards and article hero use available product imagery', async ({page}) => {
  await page.goto('/');
  const homeImage = page.locator('.journal-card img').first();
  await homeImage.scrollIntoViewIfNeeded();
  await expect(homeImage).toHaveAttribute('src', /-main\.webp$/);
  await expect.poll(() => homeImage.evaluate(image => image.complete && image.naturalWidth > 0)).toBe(true);

  await page.goto('/magazine');
  const cardImage = page.locator('.journal-post img').first();
  await cardImage.scrollIntoViewIfNeeded();
  await expect(cardImage).toHaveAttribute('src', /-main\.webp$/);
  await expect.poll(() => cardImage.evaluate(image => image.complete && image.naturalWidth > 0)).toBe(true);

  await page.goto('/magazine/choosing-work-drill');
  const heroImage = page.locator('.article-hero img.hero-image');
  await expect(heroImage).toHaveAttribute('src', /-main\.webp$/);
  await expect.poll(() => heroImage.evaluate(image => image.complete && image.naturalWidth > 0)).toBe(true);
  const contentImage = page.locator('.article-body figure img');
  await contentImage.scrollIntoViewIfNeeded();
  await expect(contentImage).toHaveAttribute('src', /-main\.webp$/);
  await expect.poll(() => contentImage.evaluate(image => image.complete && image.naturalWidth > 0)).toBe(true);
});

test('FAQ answer and contact channels fit the viewport', async ({page, isMobile}) => {
  if (isMobile) await page.setViewportSize({width: 320, height: 640});
  await page.goto('/faq');
  const item = page.locator('.faq-item').first();
  await item.locator('summary').click();
  await expect(item).toHaveAttribute('open', '');
  await expect(item.locator('.faq-answer')).toBeVisible();
  const faqWidth = await page.evaluate(() => ({content: document.documentElement.scrollWidth, viewport: innerWidth}));
  expect(faqWidth.content).toBeLessThanOrEqual(faqWidth.viewport + 1);

  await page.goto('/contact');
  await expect(page.locator('.contact-panel a[href^="tel:"]').first()).toBeVisible();
  await expect(page.locator('.contact-panel a[href^="mailto:"]')).toBeVisible();
  await expect(page.locator('a[href^="https://wa.me/"]').first()).toBeVisible();
  const contactWidth = await page.evaluate(() => ({content: document.documentElement.scrollWidth, viewport: innerWidth}));
  expect(contactWidth.content).toBeLessThanOrEqual(contactWidth.viewport + 1);
});

test('article sidebar sticks during page scroll while its own scrollbar stays hidden', async ({page, isMobile}) => {
  test.skip(isMobile, 'The floating sidebar is a desktop layout.');
  await page.setViewportSize({width: 1440, height: 900});
  await page.goto('/magazine/choosing-work-drill');
  const sidebar = page.locator('#main-content .article-enhanced .article-sidebar');
  const scrollArea = page.locator('#main-content .article-enhanced .article-sidebar-scroll');
  const initial = await scrollArea.evaluate(element => ({
    scrollHeight: element.scrollHeight,
    clientHeight: element.clientHeight,
    scrollbarWidth: getComputedStyle(element).scrollbarWidth,
    webkitScrollbarWidth: getComputedStyle(element, '::-webkit-scrollbar').width,
    webkitScrollbarDisplay: getComputedStyle(element, '::-webkit-scrollbar').display,
  }));
  expect(initial.scrollHeight).toBeGreaterThan(initial.clientHeight);
  expect(initial.scrollbarWidth).toBe('none');
  expect(initial.webkitScrollbarWidth).toBe('0px');
  expect(initial.webkitScrollbarDisplay).toBe('none');

  await page.mouse.move(500, 700);
  await page.mouse.wheel(0, 560);
  await expect.poll(() => page.evaluate(() => scrollY)).toBeGreaterThan(0);
  const floatingTop = await sidebar.evaluate(element => ({
    top: element.getBoundingClientRect().top,
    position: getComputedStyle(element).position,
  }));
  expect(floatingTop.position).toBe('sticky');
  expect(floatingTop.top).toBeCloseTo(180, 0);

  await page.mouse.move(1200, 500);
  await page.mouse.wheel(0, 180);
  await expect.poll(() => scrollArea.evaluate(element => element.scrollTop)).toBeGreaterThan(0);
});

test('article sidebar switches cleanly between stacked and sticky breakpoints', async ({page}) => {
  for (const width of [320, 390, 768, 991, 992, 1024]) {
    await page.setViewportSize({width, height: 900});
    await page.goto('/magazine/choosing-work-drill');
    const layout = await page.evaluate(() => {
      const sidebar = document.querySelector('.article-enhanced .article-sidebar');
      const scrollArea = document.querySelector('.article-enhanced .article-sidebar-scroll');
      return {
        sidebarDisplay: getComputedStyle(sidebar).display,
        sidebarPosition: getComputedStyle(sidebar).position,
        scrollAreaDisplay: getComputedStyle(scrollArea).display,
        scrollAreaBoxHeight: scrollArea.getBoundingClientRect().height,
        scrollbarWidth: getComputedStyle(scrollArea).scrollbarWidth,
      };
    });
    if (width <= 991) {
      expect(layout.sidebarDisplay, `${width}px sidebar`).toBe('contents');
      expect(layout.scrollAreaDisplay, `${width}px scroll wrapper`).toBe('contents');
      expect(layout.scrollAreaBoxHeight, `${width}px scroll wrapper`).toBe(0);
    } else {
      expect(layout.sidebarPosition, `${width}px sidebar`).toBe('sticky');
      expect(layout.scrollbarWidth, `${width}px scrollbar`).toBe('none');
    }
  }
});
