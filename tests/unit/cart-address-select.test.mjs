import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('long cart addresses stay inside the select on desktop and mobile', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  for (const width of [375, 1000]) {
    const page = await browser.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.setViewportSize({width, height: 700});
    await page.setContent(`<div dir="rtl" class="cart-address-section" style="width:${width - 40}px"><div class="address-picker"><div class="address-select-wrap"><select id="addressSelect"><option>بوشهر، خیابان آتش نشانی، کوچه ریحان، جنب مشاور املاک الف پلاک بسیار طولانی برای آزمایش آدرس</option></select><i></i></div><button>+</button></div></div>`);
    await page.addStyleTag({path: 'Shop/wwwroot/shop/vendor/select2/css/select2.min.css'});
    await page.addStyleTag({path: 'Shop/wwwroot/shop/css/core/select2.css'});
    await page.addStyleTag({path: 'Shop/wwwroot/shop/css/pages/checkout.css'});
    await page.addScriptTag({path: 'Shop/wwwroot/shop/vendor/jquery/jquery.min.js'});
    await page.addScriptTag({path: 'Shop/wwwroot/shop/vendor/jquery/jquery-migrate.min.js'});
    await page.addScriptTag({path: 'Shop/wwwroot/shop/vendor/select2/js/select2.min.js'});
    await page.addScriptTag({path: 'Shop/wwwroot/shop/vendor/select2/js/i18n/fa.js'});
    await page.addScriptTag({path: 'Shop/wwwroot/shop/js/core/selects.js'});
    assert.equal(await page.evaluate(() => typeof window.jQuery?.fn?.select2), 'function');
    assert.ok(await page.locator('.address-select-wrap > .select2-container').count(), errors.join('; '));
    const geometry = await page.locator('.address-select-wrap').evaluate(wrap => {
      const container = wrap.querySelector('.select2-container');
      const text = container.querySelector('.select2-selection__rendered');
      return {wrap: wrap.getBoundingClientRect().width, container: container.getBoundingClientRect().width, text: text.getBoundingClientRect().width, overflow: getComputedStyle(text).textOverflow};
    });
    assert.ok(geometry.container <= geometry.wrap + 1);
    assert.ok(geometry.text <= geometry.container);
    assert.equal(geometry.overflow, 'ellipsis');
    await page.locator('.select2-selection').click();
    assert.equal(await page.locator('.address-select-wrap .select2-dropdown').count(), 1);
    await page.close();
  }
});

test('selecting an address enables checkout and sends the selected address', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.goto('about:blank');
  await page.setContent(`<div class="cart-address-section" dir="rtl"><div class="address-select-wrap"><select id="addressSelect"></select></div><div id="addressPreview" hidden></div><p id="addressStatus"></p><button id="newAddress"></button></div>
    <div class="cart-payment-section"><input type="radio" name="paymentMethod" value="zarinpal"><p id="paymentMethodStatus"></p></div>
    <div id="cartView"><a class="cart-checkout-link" href="https://example.test/Checkout">پیش‌فاکتور</a></div>
    <dialog id="addressDialog"></dialog><button id="closeAddress"></button><form id="addressForm"><label><input name="phone"></label><label><input name="postal"></label></form><div id="addressFormStatus"><i></i><span class="address-form-status-message"></span></div>`);
  await page.evaluate(() => {
    const addresses = [{id: '42', title: 'خانه', recipient: 'مشتری', phone: '09120000000', province: 'تهران', city: 'تهران', street: 'خیابان اول', postal: '1234567890'}];
    window.HNUI = {
      ready: callback => callback(), mount: (_name, _root, callback) => callback(),
      storage: {get: key => key === 'hn-delivery-addresses' ? JSON.stringify(addresses) : '', hydrate() {}},
      cart: {read: () => [{id: '1', quantity: 1, price: 100}]},
      auth: {isLoggedIn: () => false}, setMessage() {}, setMessageType() {}
    };
    HTMLFormElement.prototype.submit = function () { window.checkoutSubmission = Object.fromEntries(new FormData(this)); };
  });
  await page.addScriptTag({path: 'Shop/wwwroot/shop/vendor/jquery/jquery.min.js'});
  await page.addScriptTag({path: 'Shop/wwwroot/shop/vendor/jquery/jquery-migrate.min.js'});
  await page.addScriptTag({path: 'Shop/wwwroot/shop/vendor/select2/js/select2.min.js'});
  await page.addScriptTag({path: 'Shop/wwwroot/shop/vendor/select2/js/i18n/fa.js'});
  await page.addScriptTag({path: 'Shop/wwwroot/shop/js/core/selects.js'});
  await page.addScriptTag({path: 'Shop/wwwroot/shop/js/pages/checkout.js'});
  assert.equal(await page.locator('.cart-checkout-link').getAttribute('aria-disabled'), 'true');
  await page.locator('.select2-selection').click();
  await page.getByRole('option', {name: /خانه/}).click();
  assert.equal(await page.locator('.cart-checkout-link').getAttribute('aria-disabled'), 'false');
  assert.match(await page.locator('#addressPreview').innerText(), /خیابان اول/);
  await page.locator('.cart-checkout-link').click();
  assert.deepEqual(await page.evaluate(() => window.checkoutSubmission), {addressId: '42', paymentMethod: 'zarinpal'});
});
