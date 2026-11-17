import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('Persian date filters select Gregorian values and reset both controls', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent('<form><label for="from">From</label><input id="from" type="date" name="from"><button type="reset">Reset</button></form>');
  await page.addStyleTag({path: 'Admin/wwwroot/color_admin/assets/css/vendor.min.css'});
  await page.addScriptTag({path: 'Admin/wwwroot/lib/bootstrap/bootstrap.bundle.min.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/lib/persian-datetimepicker/mds.bs.datetimepicker.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-persian-datetime.js'});
  await page.locator('#from-persian').click();
  await page.locator('[data-mds-dtp].show').waitFor();
  const day = page.locator('[data-mds-dtp] [data-day]').first();
  // The picker attaches its delegated calendar listener 500 ms after opening.
  await page.waitForTimeout(600);
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
