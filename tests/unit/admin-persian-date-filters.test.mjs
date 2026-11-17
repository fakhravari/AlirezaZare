import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('Persian date filters select Gregorian values and reset both controls', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage({viewport: {width: 375, height: 800}});
  await page.setContent('<form><label for="from">From</label><input id="from" type="date" name="from"><button type="reset">Reset</button><button type="button" id="outside" style="position:fixed;bottom:5px;left:5px">Outside</button></form>');
  await page.addStyleTag({path: 'Admin/wwwroot/color_admin/assets/css/vendor.min.css'});
  await page.addStyleTag({path: 'Admin/wwwroot/color_admin/assets/css/default/app-rtl.min.css'});
  await page.addStyleTag({path: 'Admin/wwwroot/lib/persian-datetimepicker/mds.bs.datetimepicker.style.css'});
  await page.addStyleTag({path: 'Admin/wwwroot/css/admin-ui.css'});
  await page.addScriptTag({path: 'Admin/wwwroot/lib/bootstrap/bootstrap.bundle.min.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/lib/persian-datetimepicker/mds.bs.datetimepicker.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-persian-datetime.js'});
  await page.locator('#from-persian').click();
  await page.locator('[data-mds-dtp].show').waitFor();
  const day = page.locator('[data-mds-dtp] [data-day]').first();
  // The picker attaches its delegated calendar listener 500 ms after opening.
  await page.waitForTimeout(900);
  assert.equal(await page.locator('[data-mds-dtp] .admin-row-actions').count(), 0);
  assert.equal(await page.locator('[data-mds-dtp] .admin-action-btn').count(), 0);
  const sizes = await page.locator('[data-mds-dtp]').evaluate(node => ({
    dialog: node.getBoundingClientRect().width,
    calendar: node.querySelector('.mds-bs-dtp-container').scrollWidth,
    available: node.querySelector('.popover-body').clientWidth,
    font: getComputedStyle(node.querySelector('[data-day]')).fontSize
  }));
  assert.ok(sizes.dialog <= 360 && sizes.dialog <= 375);
  assert.ok(sizes.calendar <= sizes.available, JSON.stringify(sizes));
  assert.equal(sizes.font, '14px');
  await page.locator('#outside').click();
  await page.locator('[data-mds-dtp]').waitFor({state: 'detached'});
  await page.waitForTimeout(200);
  await page.locator('#from-persian').click();
  await page.locator('[data-mds-dtp].show').waitFor();
  await page.waitForTimeout(900);
  const monthBefore = await page.locator('[data-mds-dtp-title]').textContent({timeout: 3000});
  await page.locator('[data-mds-dtp] button[title="ماه قبل"]').click();
  await page.waitForFunction(previous => document.querySelector('[data-mds-dtp-title]')?.textContent !== previous, monthBefore);
  assert.notEqual(await page.locator('[data-mds-dtp-title]').textContent({timeout: 3000}), monthBefore);
  await day.click();
  await page.waitForFunction(() => document.querySelector('#from').value);
  const value = await page.locator('#from').inputValue();
  assert.match(value, /^\d{4}-\d{2}-\d{2}$/);
  assert.match(await page.locator('#from-persian').inputValue(), /[۰-۹]/);
  assert.equal(await page.evaluate(() => new FormData(document.querySelector('form')).get('from')), value);
  await page.getByRole('button', {name: 'Reset'}).click();
  await page.waitForFunction(() => !document.querySelector('#from-persian').value && !document.querySelector('#from').value);
  assert.equal(await page.locator('#from').inputValue(), '');
});
