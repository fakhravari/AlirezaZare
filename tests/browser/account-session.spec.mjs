import net from 'node:net';
import {test, expect} from '@playwright/test';

// The SMTP listener and account are disposable test resources. No production
// authentication bypass is installed in the application.
const smtpPort = Number(process.env.SHOP_TEST_SMTP_PORT || 0);
const connection = process.env.ConnectionStrings__DefaultConnection || '';
test.skip(!smtpPort || !/server\s*=\s*(?:localhost|127\.0\.0\.1)(?:;|\\)/i.test(connection) ||
  !/database\s*=\s*ShopVerify_[^;]+/i.test(connection), 'requires an isolated local ShopVerify_ database and test SMTP');
let smtp, messages = [];

test.beforeAll(async () => {
  smtp = net.createServer(socket => {
    socket.on('error', () => {});
    socket.setEncoding('utf8');
    socket.write('220 localhost test SMTP\r\n');
    let buffer = '', data = false, message = '';
    socket.on('data', chunk => {
      buffer += chunk;
      while (buffer.includes('\r\n')) {
        const index = buffer.indexOf('\r\n');
        const line = buffer.slice(0, index);
        buffer = buffer.slice(index + 2);
        if (data) {
          if (line === '.') { messages.push(message); message = ''; data = false; socket.write('250 accepted\r\n'); }
          else message += line + '\n';
        } else if (/^EHLO /i.test(line)) socket.write('250-localhost\r\n250 SIZE 1000000\r\n');
        else if (/^HELO |^MAIL FROM:|^RCPT TO:|^RSET/i.test(line)) socket.write('250 OK\r\n');
        else if (/^DATA$/i.test(line)) { data = true; socket.write('354 send data\r\n'); }
        else if (/^QUIT$/i.test(line)) { socket.end('221 bye\r\n'); }
        else socket.write('250 OK\r\n');
      }
    });
  });
  await new Promise((resolve, reject) => smtp.once('error', reject).listen(smtpPort, '127.0.0.1', resolve));
});
test.afterAll(async () => { if (smtp?.listening) await new Promise(resolve => smtp.close(resolve)); });

test('email OTP creates a real session and loads the account from SQL', async ({page}) => {
  test.setTimeout(60000);
  const email = `browser-${Date.now()}@example.test`;
  await page.goto('/');
  await page.locator('#accountToggle').click();
  await page.locator('#loginEmail').fill(email);
  await page.locator('#loginEmailForm button[type="submit"]').click();
  await expect.poll(() => messages.length).toBeGreaterThan(0);
  const mail = messages.at(-1);
  const body = mail.split(/\n\s*\n/).slice(1).join('\n');
  const decoded = Buffer.from(body.replace(/\s/g, ''), 'base64').toString('utf8');
  const code = decoded.match(/\b\d{6}\b/)?.[0];
  expect(code).toBeTruthy();
  await page.locator('#loginEmailOtp').fill(code);
  await page.locator('#emailOtpForm button[type="submit"]').click();
  await expect(page.locator('#loginModal')).not.toBeVisible();
  const state = await page.request.get('/api/account/state');
  expect(state.status()).toBe(200);
  expect((await state.json()).email).toBe(email);
  await page.goto('/profile');
  await expect(page.locator('#loginModal')).not.toBeVisible();
  await expect(page.locator('main')).toBeVisible();

  const geography = await (await page.request.get('/api/store/geography')).json();
  const country = geography.countries.find(item => item.nameEn === 'Iran');
  const province = geography.provinces.find(item => item.countryId === country.id);
  const city = geography.cities.find(item => item.provinceId === province.id);
  const savedAddress = await page.request.post('/api/account/addresses', {data: {
    title: 'آدرس آزمایشی', recipient: 'کاربر آزمایشی', phone: '09123456789',
    countryId: country.id, provinceId: province.id, cityId: city.id,
    street: 'خیابان آزمایشی', postal: '1234567890',
  }});
  expect(savedAddress.status(), await savedAddress.text()).toBe(200);
  const address = await savedAddress.json();
  await page.goto('/profile/addresses');
  await expect(page.locator('#addressList')).toContainText('آدرس آزمایشی');
  await page.locator(`#addressList [data-edit="${address.id}"]`).click();
  await expect(page.locator('#addressDialog')).toBeVisible();
  await page.locator('#closeAddress').click();
  await page.locator(`#addressList [data-delete="${address.id}"]`).click();
  await expect(page.locator('.form-confirm-dialog')).toBeVisible();
  await page.locator('.form-confirm-cancel').click();
  await expect(page.locator('#addressList')).toContainText('آدرس آزمایشی');

  await page.goto('/search');
  await expect(page.locator('#accountToggle')).toHaveClass(/logged-in/);
  const line = await page.evaluate(() => {
    const data = JSON.parse(document.querySelector('#storeSalesPlans').textContent);
    const plan = data.plans.find(item => item.isDefault);
    const offer = data.offers.find(item => item.salesPlanId === plan.id && item.isEnabled && item.stockQuantity > 0 &&
      !data.inventoryVariants.some(variant => variant.productId === item.productId));
    return offer && {productId: offer.productId, salesPlanId: plan.id, quantity: 1, options: {}};
  });
  expect(line).toBeTruthy();
  const savedCart = await page.request.put('/api/account/cart', {data: {items: [line]}});
  expect(savedCart.status(), await savedCart.text()).toBe(200);
  await page.goto('/cart');
  await expect(page.locator('#cartView .cart-product-row')).toHaveCount(1);
  await page.reload();
  await expect(page.locator('#cartView .cart-product-row')).toHaveCount(1);
  await expect(page.locator('#addressSelect option')).toHaveCount(2);
  await page.locator('#addressSelect').selectOption(String(address.id));
  await page.goto('/checkout');
  await expect(page.locator('#finalPayment')).toBeEnabled({timeout: 15000});
  const checkoutResponse = page.waitForResponse(response => response.url().endsWith('/api/account/checkout') && response.request().method() === 'POST');
  await page.locator('#finalPayment').click();
  const checkout = await checkoutResponse;
  expect(checkout.status()).toBe(200);
  await expect(page).toHaveURL(/\/payment\/simulated\/\d+$/);
  const createdState = await (await page.request.get('/api/account/state')).json();
  const order = createdState.orders[0];
  expect(order.status).toBe('pending-payment');
  await expect(page).toHaveURL(new RegExp(`/payment/simulated/${order.id}$`));
  await expect(page.locator('[name="result"][value="approved"]')).toBeChecked();
  await page.locator('[name="result"][value="cancelled"]').check();
  await page.locator('main form button[type="submit"]').click();
  await expect(page.locator('main [role="status"]')).toContainText('لغو شد');
  await page.goto('/profile/orders');
  await page.locator(`a[href="/payment/simulated/${order.id}"]`).click();
  await expect(page.locator('[name="result"][value="approved"]')).toBeChecked();
  const token = await page.locator('input[name="__RequestVerificationToken"]').inputValue();
  await page.locator('main form button[type="submit"]').click();
  await expect(page.locator('main [role="status"]')).toContainText('پرداخت آزمایشی تأیید شد');
  const duplicate = await page.request.post(`/payment/simulated/${order.id}/return`, {
    form: {result: 'approved', __RequestVerificationToken: token}, maxRedirects: 0,
  });
  expect(duplicate.status()).toBe(302);
  const updatedState = await (await page.request.get('/api/account/state')).json();
  expect(updatedState.orders.find(item => item.id === order.id).status).toBe('simulated-payment-confirmed');
  const noCsrf = await page.request.post(`/payment/simulated/${order.id}/return`, {form: {result: 'approved'}, maxRedirects: 0});
  expect(noCsrf.status()).toBeGreaterThanOrEqual(400);
  const unknownOrder = await page.request.get(`/payment/simulated/${Number(order.id) + 1000000}`);
  expect(unknownOrder.status()).toBe(404);
  await page.goto('/profile/orders');
  const displayNumber = order.number.replace(/\d/g, digit => '۰۱۲۳۴۵۶۷۸۹'[Number(digit)]);
  await expect(page.locator('main')).toContainText(displayNumber);
  await expect(page.locator('#statOrders')).toHaveText('۱');
  await page.locator('.profile-order-card [data-open-order-invoice]').first().click();
  await expect(page.locator('#orderInvoiceDialog')).toBeVisible();
  await expect(page.locator('#orderInvoiceDialog [data-invoice-number]')).toHaveText(order.number);
});
