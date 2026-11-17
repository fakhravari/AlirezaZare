import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('filter selections submit existing handlers without submitting editors', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<form class="admin-filter-bar"><select id="filter" name="status"><option value="">All</option><option value="active">Active</option></select><input type="search" name="q"></form>
    <form class="admin-sales-plans__filter"><select id="plan"><option value="">All</option><option value="1">One</option></select></form>
    <form id="editor"><select id="edit"><option value="">None</option><option value="1">One</option></select></form>`);
  await page.evaluate(() => { window.submissions = []; document.querySelectorAll('form').forEach(form => form.addEventListener('submit', event => { event.preventDefault(); window.submissions.push(form.className || form.id); })); });
  await page.addScriptTag({path: 'Admin/wwwroot/color_admin/assets/plugins/jquery/dist/jquery.min.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/color_admin/assets/plugins/select2/dist/js/select2.min.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-selects.js'});
  await page.locator('#filter + .select2').click();
  await page.locator('.select2-results__option', {hasText: 'Active'}).click();
  assert.deepEqual(await page.evaluate(() => window.submissions), ['admin-filter-bar']);
  await page.locator('#plan').selectOption('1');
  await page.locator('#edit').selectOption('1');
  assert.deepEqual(await page.evaluate(() => window.submissions), ['admin-filter-bar', 'admin-sales-plans__filter']);
  await page.locator('[name=q]').fill('text');
  assert.equal(await page.evaluate(() => window.submissions.length), 2);
});
