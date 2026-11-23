import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { chromium } from 'playwright';

test('login and one-time code use field validation and clear corrected errors', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<form id="loginEmailForm"><div class="login-input"><input id="loginEmail" type="email" required></div><div class="login-input"><input id="loginPassword" type="password" required></div><button>ورود</button></form>
    <form id="emailOtpForm"><input id="loginEmailOtp" inputmode="numeric" pattern="[0-9۰-۹]{6}" required><button>تأیید</button></form>`);
  await page.evaluate(() => {
    window.HNUI = {mount: (_name, _root, callback) => callback(), setMessageType: () => {}, toEnglishDigits: value => value.replace(/[۰-۹]/g, digit => String('۰۱۲۳۴۵۶۷۸۹'.indexOf(digit)))};
    document.querySelectorAll('form').forEach(form => form.addEventListener('submit', event => { event.preventDefault(); form.dataset.submitted = 'true'; }));
  });
  await page.addScriptTag({path: 'Shop/wwwroot/shop/js/core/form-validation.js'});
  await page.locator('#loginEmailForm button').click();
  assert.equal(await page.locator('#loginEmail').getAttribute('aria-invalid'), 'true');
  assert.equal(await page.locator('#loginPassword').getAttribute('aria-invalid'), 'true');
  assert.equal(await page.locator('#form-error-loginEmail').evaluate(element => element.previousElementSibling.querySelector('input').id), 'loginEmail');
  await page.locator('#loginEmail').fill('user@example.com');
  assert.equal(await page.locator('#form-error-loginEmail').isHidden(), true);
  await page.locator('#loginPassword').fill('password');
  await page.locator('#loginEmailForm button').click();
  assert.equal(await page.locator('#loginEmailForm').getAttribute('data-submitted'), 'true');
  await page.locator('#loginEmailOtp').fill('۱۲۳');
  await page.locator('#emailOtpForm button').click();
  assert.equal(await page.locator('#loginEmailOtp').getAttribute('aria-invalid'), 'true');
  await page.locator('#loginEmailOtp').fill('۱۲۳۴۵۶');
  await page.locator('#emailOtpForm button').click();
  assert.equal(await page.locator('#emailOtpForm').getAttribute('data-submitted'), 'true');
});
