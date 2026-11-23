import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { chromium } from '@playwright/test';

test('home sales plans show countdown only when enabled and dated', { skip: !existsSync(chromium.executablePath()) }, async t => {
  const browser = await chromium.launch({ headless: true });
  t.after(() => browser.close());
  const page = await browser.newPage();
  const plans = [
    { id: 'disabled', name: 'بدون شمارش', type: 'digi-af', isEnabled: true, showCountdown: false, endsAtUtc: '2099-01-01T00:00:00Z' },
    { id: 'enabled', name: 'با شمارش', type: 'digi-af', isEnabled: true, showCountdown: true, endsAtUtc: '2099-01-01T00:00:00Z' },
    { id: 'undated', name: 'بدون تاریخ', type: 'digi-af', isEnabled: true, showCountdown: true }
  ];
  const offers = plans.map(plan => ({ productId: 1, salesPlanId: plan.id, isEnabled: true, stockQuantity: 1 }));
  await page.setContent(`<div id="homeSalesTime" hidden><div id="homeSalesTimeContent"></div></div><div id="homeSalesPlans" hidden><div id="homeSalesPlansContent"></div></div><script id="storeCatalog" type="application/json">${JSON.stringify({ products: [{ id: 1 }] })}</script><script id="storeSalesPlans" type="application/json">${JSON.stringify({ plans, offers })}</script>`);
  await page.evaluate(() => {
    window.HNCatalog = { find: id => ({ id }) };
    window.HNProductCard = { render: () => '<div>کالا</div>' };
    window.HNUI = { escapeHtml: String, formatNumber: String };
  });
  await page.addScriptTag({ path: 'Shop/wwwroot/shop/js/pages/home-sales-plans.js' });
  assert.equal(await page.locator('[data-sales-plan="disabled"] [data-sales-plan-countdown]').count(), 0);
  assert.equal(await page.locator('[data-sales-plan="undated"] [data-sales-plan-countdown]').count(), 0);
  assert.equal(await page.locator('[data-sales-plan="enabled"] [data-sales-plan-countdown]').count(), 1);
  assert.equal(await page.locator('[data-sales-plan="disabled"]').count(), 1);
});
