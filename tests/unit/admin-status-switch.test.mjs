import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('status switches preserve native keyboard, form and disabled behavior', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent('<form><label>Active <input type="checkbox" name="active" data-admin-switch role="switch" checked></label><input type="checkbox" name="selection"><input type="checkbox" name="disabled" disabled></form>');
  await page.addStyleTag({path: 'Admin/wwwroot/css/admin-ui.css'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  const active = page.getByRole('switch', {name: 'Active'});
  assert.equal(await active.isChecked(), true);
  await active.focus();
  await page.keyboard.press('Space');
  assert.equal(await active.isChecked(), false);
  assert.equal(await page.evaluate(() => new FormData(document.querySelector('form')).has('active')), false);
  await active.click();
  assert.equal(await page.evaluate(() => new FormData(document.querySelector('form')).get('active')), 'on');
  await page.evaluate(() => window.AdminUI.initStatusSwitch(document.querySelector('[name=disabled]')));
  assert.equal(await page.locator('[name=disabled]').getAttribute('role'), 'switch');
  assert.equal(await page.locator('[name=disabled]').isDisabled(), true);
  assert.equal(await page.locator('[name=selection]').getAttribute('role'), null);
  assert.equal(await active.evaluate(element => getComputedStyle(element).appearance), 'none');
});
