import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { chromium } from '@playwright/test';

test('sales plan pagination keeps the selected row connected to its editor', { skip: !existsSync(chromium.executablePath()) }, async t => {
  const browser = await chromium.launch({ headless: true });
  t.after(() => browser.close());
  const page = await browser.newPage();
  const plans = Array.from({ length: 55 }, (_, index) => ({ id: index + 1, displayName: `طرح ${index + 1}`, code: `code${index + 1}`, planType: 'special', isActive: false, isRegular: index === 0 }));
  await page.route('https://example.test/**', route => route.fulfill({ json: plans }));
  await page.goto('https://example.test/');
  await page.setContent(`<div data-store-variant-prices data-api="/api/prices" data-can-edit="true">
    <form data-plan-filter></form><form data-plan-create><button type="submit"></button></form>
    <span data-plan-count></span><table><tbody data-plan-list></tbody></table><span data-plan-status></span><div data-plan-pagination hidden></div>
    <div id="plan-settings-modal"><form data-plan-settings><select name="id"></select><span data-plan-selection></span><span data-plan-publication></span><fieldset data-plan-fields>
    <input name="code"><input name="displayName"><input name="planType"><input name="startsAtUtc"><input name="endsAtUtc"><input name="label"><input name="showCountdown" type="checkbox">
    <button type="button" data-plan-details-save></button><button type="button" data-plan-delete></button><button type="button" data-plan-publish></button><button type="button" data-plan-unpublish></button><button type="submit"></button></fieldset></form></div></div>`);
  await page.evaluate(() => { window.bootstrap = { Modal: { getOrCreateInstance: element => ({ show: () => { element.dataset.open = 'true'; } }) } }; });
  await page.addScriptTag({ path: 'Admin/wwwroot/js/admin-ui.js' });
  await page.addScriptTag({ path: 'Admin/wwwroot/js/admin-variant-prices.js' });
  await page.evaluate(() => window.AdminVariantPrices.initialize(document));
  await page.waitForSelector('[data-plan-pagination] button[aria-label="صفحهٔ ۲"]');
  assert.equal(await page.locator('[data-plan-list] tr').count(), 50);
  assert.equal(await page.getByRole('button', { name: 'ویرایش طرح 1', exact: true }).count(), 0);
  await page.locator('[data-plan-pagination] button[aria-label="صفحهٔ ۲"]').click();
  assert.equal(await page.locator('[data-plan-list] tr').count(), 5);
  await page.getByRole('button', { name: 'ویرایش طرح 55' }).click();
  assert.equal(await page.locator('[data-plan-settings] [name=code]').inputValue(), 'code55');
  assert.equal(await page.locator('#plan-settings-modal').getAttribute('data-open'), 'true');
});
