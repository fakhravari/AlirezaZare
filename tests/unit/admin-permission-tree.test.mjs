import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('permission tree preserves explicit parent grants and sends only permission changes', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent('<div data-admin-users data-admin-users-page="permissions" data-api="/api/admin/users"><div data-users-status></div><select data-role-select></select><button data-role-save>save</button><div data-permission-tree></div></div>');
  await page.evaluate(() => {
    window.payloads = [];
    window.AdminUI = {
      notify() {},
      formatDateTime: value => value || '—',
      async getJson(_url, options) {
        if (options.method === 'PUT') { window.payloads.push(JSON.parse(options.body)); return {message: 'ok'}; }
        return {
          roles: [{id: 1, roleKey: 'test', name: 'role', isActive: true, permissionIds: [2, 3]}],
          permissions: [
            {id: 1, permissionKey: 'parent', name: 'parent', parentKey: null},
            {id: 2, permissionKey: 'child-a', name: 'a', parentKey: 'parent'},
            {id: 3, permissionKey: 'child-b', name: 'b', parentKey: 'parent'},
          ]
        };
      }
    };
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-users.js'});
  await page.evaluate(() => window.AdminUsers.initialize(document));
  const parent = page.locator('input[value="1"]');
  await parent.waitFor();
  assert.equal(await page.locator('[data-permission-tree] small').filter({hasText:'مسیر: parent / a'}).count(), 1);
  assert.equal(await parent.isChecked(), false);
  assert.equal(await parent.evaluate(input => input.indeterminate), true);
  await page.locator('[data-role-save]').click();
  await page.waitForFunction(() => window.payloads.length === 1);
  assert.deepEqual(await page.evaluate(() => window.payloads[0]), {permissionIds: [2, 3]});
  await parent.waitFor();
  await parent.check();
  await page.locator('.admin-permission-toggle').click();
  await page.locator('input[value="2"]').uncheck();
  assert.equal(await parent.isChecked(), false);
  assert.equal(await parent.evaluate(input => input.indeterminate), true);
});
