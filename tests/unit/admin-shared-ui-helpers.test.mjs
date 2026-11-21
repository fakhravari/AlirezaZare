import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('shared admin helpers preserve text safety and status options', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent('<div id="status"></div>');
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  const result = await page.evaluate(() => {
    const node = AdminUI.createElement('td', '<img src=x onerror=alert(1)>', 'cell');
    const status = document.querySelector('#status');
    AdminUI.setStatus(status, 'خطا', true, {compact: true, hideEmpty: true});
    const error = [status.textContent, status.classList.contains('text-danger'), status.classList.contains('text-muted'), status.classList.contains('small'), status.classList.contains('d-none')];
    AdminUI.setStatus(status, '', false, {compact: true, hideEmpty: true});
    const empty = [status.classList.contains('text-danger'), status.classList.contains('text-muted'), status.classList.contains('d-none')];
    AdminUI.setStatus(null, 'ignored');
    return {text: node.textContent, children: node.children.length, cls: node.className, error, empty};
  });
  assert.deepEqual(result, {text: '<img src=x onerror=alert(1)>', children: 0, cls: 'cell', error: ['خطا', true, false, true, false], empty: [false, true, true]});
});

test('contact messages render and report loading errors through shared helpers', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<section data-admin-contact-messages data-api="/messages">
    <form data-contact-filter></form><div data-contact-status></div><span data-contact-count></span>
    <div data-contact-empty></div><table><tbody data-contact-rows></tbody></table><div data-contact-pagination></div>
    <button data-contact-refresh></button></section>`);
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.evaluate(() => { window.AdminUI = {...AdminUI, getJson: async () => {
    if (window.fail) throw Error('خطای آزمایشی');
    return {items: [{id: 1, createdAtUtc: '2026-11-21', name: '<نام>', phone: '123', subjectKey: 'other', message: 'متن پیام', status: 'new'}], total: 1, pageSize: 50};
  }}; });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-contact-messages.js'});
  await page.evaluate(() => AdminContactMessages.initialize(document));
  await page.getByText('<نام>', {exact: true}).waitFor();
  assert.equal(await page.locator('[data-contact-rows] tr').count(), 1);
  assert.equal(await page.locator('[data-contact-rows] td').nth(1).locator('*').count(), 0);
  await page.evaluate(() => { window.fail = true; });
  await page.locator('[data-contact-refresh]').click();
  await page.getByText('خطای آزمایشی').waitFor();
  assert.equal(await page.locator('[data-contact-status]').evaluate(node => node.classList.contains('text-danger')), true);
});
