import {test, expect} from '@playwright/test';
import axe from 'axe-core';
import {useBrowserAccount} from '../helpers/browser-account.mjs';

const paths = [
  '/', '/search', '/products/cordless-hammer-drill-1', '/cart', '/checkout',
  '/compare', '/favorites', '/profile', '/profile/orders', '/profile/addresses',
  '/profile/favorites', '/profile/coupons', '/profile/pricealerts', '/brands', '/brands/bosch',
  '/magazine', '/magazine/multimeter-symbols-guide', '/contact', '/about',
  '/faq', '/shipping', '/warranty', '/tracking', '/legal/terms',
  '/legal/privacy', '/legal/returns',
];
const accessiblePaths = [...paths, '/error', '/Error/404'];

test('main pages fit mobile, tablet and desktop widths', async ({page}) => {
  test.setTimeout(480000);
  await useBrowserAccount(page, {addressId: null});
  const widths = [320, 390, 768, 1024, 1440];
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

test('home carousel controls have accessible names', async ({page}) => {
  await page.goto('/');
  const controls = page.locator('.owl-carousel .owl-dot, .owl-carousel .owl-nav .owl-prev, .owl-carousel .owl-nav .owl-next');
  await expect(controls.first()).toHaveAttribute('aria-label', /.+/);
  for (const control of await controls.all()) await expect(control).toHaveAttribute('aria-label', /.+/);
});

test('home carousel indicators stay visually compact with usable touch areas', async ({page, isMobile}) => {
  await page.goto('/');

  const heroBullets = page.locator('#heroSwiper .swiper-pagination-bullet');
  await expect(heroBullets.first()).toBeVisible();
  const heroMetrics = await heroBullets.evaluateAll(elements => elements.map(element => {
    const rect = element.getBoundingClientRect();
    const style = getComputedStyle(element);
    const mark = getComputedStyle(element, '::before');
    return {
      width: rect.width,
      height: rect.height,
      visualWidth: mark.width,
      visualHeight: mark.height,
      background: style.backgroundColor,
    };
  }));
  expect(heroMetrics.length).toBeGreaterThan(0);
  for (const metric of heroMetrics) {
    expect(metric.width).toBe(44);
    expect(metric.height).toBe(44);
  }
  expect(heroMetrics.some(metric => metric.visualWidth === '26px' && metric.visualHeight === '8px')).toBe(true);
  expect(heroMetrics.some(metric => metric.visualWidth === '8px' && metric.visualHeight === '8px')).toBe(true);
  for (const metric of heroMetrics) expect(metric.background).toBe('rgba(0, 0, 0, 0)');

  const categoryDots = page.locator('#bestsellersSlider > .owl-dots');
  if (isMobile) {
    await expect(categoryDots).toBeHidden();
  } else {
    const dotMetrics = await categoryDots.locator('.owl-dot').evaluateAll(elements => elements.map(element => {
      const button = element.getBoundingClientRect();
      const mark = getComputedStyle(element.querySelector('span'));
      return {buttonWidth: button.width, buttonHeight: button.height, markWidth: mark.width, markPadding: mark.padding};
    }));
    expect(dotMetrics.length).toBeGreaterThan(0);
    for (const metric of dotMetrics) {
      expect(metric.buttonWidth).toBe(28);
      expect(metric.buttonHeight).toBe(28);
      expect(metric.markPadding).toBe('0px');
      expect(['8px', '20px']).toContain(metric.markWidth);
    }
  }
});

test('home hero and carousel motion respect reduced-motion preference', async ({page}) => {
  await page.emulateMedia({reducedMotion: 'reduce'});
  await page.goto('/');
  const hero = page.locator('#heroSwiper');
  await expect(hero).toBeVisible();
  const autoplay = await hero.evaluate(element => element.swiper?.params.autoplay);
  expect(autoplay === false || autoplay?.enabled === false).toBe(true);
  await expect(page.locator('#heroSwiper .swiper-pagination-bullet').first())
    .toHaveCSS('transition-duration', '0s');
});

test('main routes expose basic document and control names', async ({page}) => {
  test.setTimeout(120000);
  for (const path of accessiblePaths) {
    await page.goto(path, {waitUntil: 'domcontentloaded'});
    const issues = await page.evaluate(() => {
      const visible = element => {
        const style = getComputedStyle(element);
        return style.display !== 'none' && style.visibility !== 'hidden' && element.getClientRects().length > 0;
      };
      const nameOf = element => {
        const ids = element.getAttribute('aria-labelledby')?.split(/\s+/) || [];
        const referenced = ids.map(id => document.getElementById(id)?.textContent?.trim()).filter(Boolean).join(' ');
        const image = [...element.querySelectorAll('img[alt]')].map(item => item.alt).filter(Boolean).join(' ');
        return element.getAttribute('aria-label') || referenced || element.labels?.[0]?.textContent?.trim()
          || element.getAttribute('title') || element.innerText?.trim() || element.textContent?.trim() || image;
      };
      const result = [];
      if (!document.documentElement.lang) result.push('document missing language');
      if (document.documentElement.dir !== 'rtl') result.push('document direction should be rtl');
      if (!document.title.trim()) result.push('document missing title');
      if (document.querySelectorAll('main').length !== 1) result.push('expected exactly one main landmark');
      if (document.querySelectorAll('h1').length !== 1) result.push('expected exactly one H1 in document');
      const ids = [...document.querySelectorAll('[id]')].map(element => element.id);
      if (new Set(ids).size !== ids.length) result.push('duplicate id');
      for (const item of document.querySelectorAll('[aria-labelledby], [aria-describedby], [aria-controls], [aria-owns], [aria-activedescendant]')) {
        for (const attribute of ['aria-labelledby', 'aria-describedby', 'aria-controls', 'aria-owns', 'aria-activedescendant']) {
          const value = item.getAttribute(attribute);
          if (!value) continue;
          for (const id of value.trim().split(/\s+/)) {
            if (!document.getElementById(id)) result.push(`broken ${attribute} reference: ${id}`);
          }
        }
      }
      for (const item of document.querySelectorAll('img')) if (!item.hasAttribute('alt')) result.push(`image missing alt: ${item.currentSrc || item.src}`);
      for (const item of document.querySelectorAll('a[href],button,input:not([type=hidden]),select,textarea,[role=button],[role=link]')) {
        if (visible(item) && !nameOf(item)) result.push(`unnamed ${item.tagName.toLowerCase()}: ${item.outerHTML.slice(0, 140)}`);
      }
      return result;
    });
    expect(issues, `${path} basic accessibility`).toEqual([]);
  }
});

test('main routes pass automated WCAG accessibility rules', async ({page}) => {
  test.setTimeout(180000);
  await useBrowserAccount(page, {addressId: null});
  await page.addInitScript(axe.source);
  const allViolations = new Map();
  for (const path of accessiblePaths) {
    await page.goto(path, {waitUntil: 'domcontentloaded'});
    const results = await page.evaluate(async () => window.axe.run(document, {
      runOnly: {type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice']},
    }));
    for (const issue of results.violations) {
      for (const node of issue.nodes) {
        const target = node.target.join(' ');
        const key = `${issue.id}|${target}`;
        const finding = allViolations.get(key) || {id: issue.id, impact: issue.impact, target, summary: node.failureSummary, routes: []};
        finding.routes.push(path);
        allViolations.set(key, finding);
      }
    }
  }
  expect([...allViolations.values()], 'WCAG accessibility violations across main routes').toEqual([]);
});

test('shared skip link is the first keyboard stop and transfers focus to main content', async ({page}) => {
  for (const path of ['/', '/products/cordless-hammer-drill-1']) {
    await page.goto(path);
    await page.keyboard.press('Tab');
    const skipLink = page.locator('.skip-link');
    await expect(skipLink).toBeFocused();
    await expect(skipLink).toBeVisible();
    const skipFocus = await skipLink.evaluate(element => ({top: element.getBoundingClientRect().top, outline: getComputedStyle(element).outlineStyle}));
    expect(skipFocus.top).toBeGreaterThanOrEqual(0);
    expect(skipFocus.outline).toBe('solid');
    await page.keyboard.press('Enter');
    await expect(page.locator('#main-content')).toBeFocused();
    await expect(page.locator('#main-content')).toHaveCSS('outline-style', 'solid');
  }
  await page.goto('/profile');
  await expect(page.locator('#loginModal')).toBeVisible();
});

test('compatibility routes resolve to their current pages', async ({page}) => {
  const aliases = [
    ['/account', '/profile'],
    ['/profile/edit', '/profile'],
    ['/magazine/details/multimeter-symbols-guide', '/magazine/multimeter-symbols-guide'],
    ['/products/1', '/products/cordless-hammer-drill-1'],
    ['/Product?id=1', '/products/cordless-hammer-drill-1'],
  ];
  for (const [alias, canonical] of aliases) {
    await page.goto(alias);
    await expect(page).toHaveURL(new RegExp(`${canonical.replaceAll('/', '\\/')}(?:[?#].*)?$`));
    await expect(page.locator('main').first()).toBeVisible();
  }
});

test('error pages retain their status and fit all responsive widths', async ({page}) => {
  for (const width of [320, 390, 768, 1024, 1440]) {
    await page.setViewportSize({width, height: 800});
    for (const [path, status] of [['/error', 500], ['/Error/404', 404]]) {
      const response = await page.goto(path);
      expect(response.status(), `${path} at ${width}px`).toBe(status);
      await expect(page.locator('main').first()).toBeVisible();
      const dimensions = await page.evaluate(() => ({content: document.documentElement.scrollWidth, viewport: innerWidth}));
      expect(dimensions.content, `${path} at ${width}px`).toBeLessThanOrEqual(dimensions.viewport + 1);
    }
  }
});

test('discounted search prices fit narrow product cards', async ({page, isMobile}) => {
  if (!isMobile) test.skip();
  await page.setViewportSize({width: 320, height: 640});
  await page.goto('/search');
  const prices = page.locator('.search-page-card-price.has-discount');
  expect(await prices.count()).toBeGreaterThan(0);
  for (const price of await prices.all()) {
    const dimensions = await price.evaluate(element => ({content: element.scrollWidth, available: element.clientWidth}));
    expect(dimensions.content).toBeLessThanOrEqual(dimensions.available + 1);
  }
});

test('search sort choices wrap instead of clipping at narrow phone widths', async ({page}) => {
  for (const width of [320, 360, 390]) {
    await page.setViewportSize({width, height: 800});
    await page.goto('/search');
    const sortBox = page.locator('.sort-box');
    await expect(sortBox).toBeVisible();
    const layout = await sortBox.evaluate(element => {
      const box = element.getBoundingClientRect();
      const choices = [...element.querySelectorAll('.sort-option')].map(choice => {
        const rect = choice.getBoundingClientRect();
        return {left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom};
      });
      return {
        width: element.clientWidth,
        scrollWidth: element.scrollWidth,
        left: box.left,
        right: box.right,
        choices,
      };
    });
    expect(layout.scrollWidth, `${width}px sort panel`).toBeLessThanOrEqual(layout.width + 1);
    expect(layout.choices.length).toBeGreaterThan(2);
    for (const choice of layout.choices) {
      expect(choice.left, `${width}px choice left`).toBeGreaterThanOrEqual(layout.left - 1);
      expect(choice.right, `${width}px choice right`).toBeLessThanOrEqual(layout.right + 1);
    }
  }
});

test('page-owned placeholder colors override the shared fallback', async ({page}) => {
  for (const width of [390, 1440]) {
    await page.setViewportSize({width, height: 800});
    for (const [path, selector] of [
      ['/search', '.filter-search input'],
      ['/tracking', '.tracking-input input'],
    ]) {
      await page.goto(path);
      const placeholder = page.locator(selector).first();
      const actual = await placeholder.evaluate(element => getComputedStyle(element, '::placeholder').color);
      const expected = await page.evaluate(() => {
        const probe = document.createElement('span');
        probe.style.color = 'var(--industrial-muted)';
        document.body.append(probe);
        const color = getComputedStyle(probe).color;
        probe.remove();
        return color;
      });
      expect(actual, `${path} placeholder color at ${width}px`).toBe(expected);
    }
  }
});
