import test, {before, after} from 'node:test';
import assert from 'node:assert/strict';
import {spawn} from 'node:child_process';
import {request as httpRequest} from 'node:http';
import net from 'node:net';
import path from 'node:path';
import {setTimeout as delay} from 'node:timers/promises';
import {root, source} from '../helpers/runtime.mjs';

let server, base, logs = '', exited = false;
before(async () => {
  const probe = net.createServer();
  await new Promise(resolve => probe.listen(0, '127.0.0.1', resolve));
  const port = probe.address().port;
  await new Promise(resolve => probe.close(resolve));
  base = `http://127.0.0.1:${port}`;
  const appDll = process.env.SHOP_TEST_DLL ? path.resolve(root, process.env.SHOP_TEST_DLL) : path.join(root, 'Shop/bin/Debug/net10.0/Shop.dll');
  server = spawn('dotnet', [appDll, '--urls', base], {
    cwd: path.join(root, 'Shop'), env: {...process.env, ASPNETCORE_ENVIRONMENT: 'Development'}, stdio: ['ignore', 'pipe', 'pipe'],
  });
  server.on('exit', () => { exited = true; });
  server.on('error', error => { logs += String(error); exited = true; });
  for (const stream of [server.stdout, server.stderr]) stream.on('data', data => { logs = (logs + data).slice(-10000); });
  for (let attempt = 0; attempt < 150; attempt++) {
    try { const response = await fetch(base, {signal: AbortSignal.timeout(1000)}); if (response.status === 200) return; } catch {}
    if (exited) break;
    await delay(200);
  }
  server.kill(); throw new Error('HTTP smoke server did not start:\n' + logs);
});
after(async () => {
  if (!server || exited) return;
  const stopped = new Promise(resolve => server.once('exit', resolve));
  server.kill(); await stopped;
});

test('account coupons and public offers share values without publishing order history', async () => {
  const home = await (await fetch(base + '/')).text();
  const profile = await (await fetch(base + '/profile/coupons')).text();
  const parse = (html, id) => JSON.parse(html.match(new RegExp(`<script type="application/json" id="${id}">([\\s\\S]*?)</script>`))[1]);
  const offers = parse(home, 'commerceData');
  const account = parse(profile, 'accountData');
  assert.equal(offers.schemaVersion, 1);
  assert.equal(offers.currency, 'IRT');
  assert.deepEqual(offers.couponOffers, Object.fromEntries(account.coupons.filter(c => c.active).map(c => [c.code, c.discountPercent])));
  assert.equal(account.coupons.length, 10);
  assert.ok(account.coupons.some(c => c.uses.length > 0));
  assert.ok(!home.includes('id="accountData"'));
  assert.deepEqual(Object.keys(offers).sort(), ['couponOffers', 'currency', 'schemaVersion']);
  assert.equal((await fetch(base + '/App_Data/account-demo.json')).status, 404);
});

test('sales-plan quotes validate nullable options, plan-specific accessory prices and cart identity', async () => {
  const quote = async body => fetch(base + '/api/store/sales-plans/quote', {
    method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify(body),
  });
  const noOptions = await quote({productId: 7, salesPlanId: 'regular', quantity: 1, variants: {}, accessoryIds: []});
  assert.equal(noOptions.status, 200, logs.slice(-2000));
  const invalidOption = await quote({productId: 7, salesPlanId: 'regular', quantity: 1, variants: {unknown: 'x'}, accessoryIds: []});
  assert.equal(invalidOption.status, 400);

  const planQuote = await quote({productId: 1, salesPlanId: 'digi-time', quantity: 1,
    variants: {battery: '2ah', chuck: '10mm'}, accessoryIds: ['shipping-insurance']});
  assert.equal(planQuote.status, 200, logs.slice(-2000));
  const priced = await planQuote.json();
  assert.equal(priced.accessoryAmounts['shipping-insurance'], 245000);

  const duplicateLine = await fetch(base + '/api/store/sales-plans/validate-cart', {
    method: 'POST', headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({lines: [
      {productId: 7, salesPlanId: 'regular', quantity: 1, variants: {}, accessoryIds: []},
      {productId: 7, salesPlanId: 'regular', quantity: 1, variants: {}, accessoryIds: []},
    ]}),
  });
  assert.equal(duplicateLine.status, 400);
  const nullLine = await fetch(base + '/api/store/sales-plans/validate-cart', {
    method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({lines: [null]}),
  });
  assert.equal(nullLine.status, 400);
});

test('home, search, product, cart, account and content routes serve the same validated catalog', async () => {
  const routes = ['/', '/search', '/products/cordless-hammer-drill-1', '/products/digital-multimeter-6', '/cart', '/checkout', '/compare', '/favorites', '/profile', '/profile/orders', '/profile/addresses', '/brands', '/brands/bosch', '/contact', '/about'];
  for (const route of routes) {
    const response = await fetch(base + route);
    assert.equal(response.status, 200, route + '\n' + logs.slice(-2000));
    const html = await response.text();
    const match = html.match(/<script type="application\/json" id="storeCatalog">([\s\S]*?)<\/script>/);
    assert.ok(match, route + ': shared catalog missing');
    assert.deepEqual(JSON.parse(match[1]), source(), route);
    assert.ok(html.includes('shop/js/store/product-card.js'));
  }
});

test('canonical redirects resolve in one hop and missing products return 404', async () => {
  for (const route of ['/products/1', '/products/incorrect-1', '/Product?id=1']) {
    const response = await fetch(base + route, {redirect: 'manual'});
    assert.equal(response.status, 301, route);
    assert.equal(response.headers.get('location'), '/products/cordless-hammer-drill-1', route);
  }
  const response = await fetch(base + '/products/missing-999');
  assert.equal(response.status, 404);
  const html = await response.text();
  assert.match(html, /صفحه پیدا نشد/);
  assert.match(html, /name="robots" content="noindex, nofollow"/);
  assert.match(html, /بازگشت به صفحه اصلی/);
});

test('canonical host redirects aliases to the configured site origin', async () => {
  const response = await new Promise((resolve, reject) => {
    const request = httpRequest({
      hostname: '127.0.0.1',
      port: new URL(base).port,
      path: '/products/cordless-hammer-drill-1?source=share',
      headers: {host: 'www.bs-shop.ir'},
    }, resolve);
    request.on('error', reject);
    request.end();
  });
  response.resume();
  assert.equal(response.statusCode, 308);
  assert.equal(response.headers.location, 'https://bs-shop.ir/products/cordless-hammer-drill-1?source=share');
});

test('internal search pages are noindex and are excluded from the sitemap', async () => {
  const search = await (await fetch(base + '/search')).text();
  assert.match(search, /name="robots" content="noindex, follow"/);
  const sitemap = await (await fetch(base + '/sitemap.xml')).text();
  assert.doesNotMatch(sitemap, /<loc>[^<]*\/search<\/loc>/);
});

test('sitemap contains every stable canonical product URL exactly once', async () => {
  const response = await fetch(base + '/sitemap.xml');
  assert.equal(response.status, 200);
  const xml = await response.text();
  for (const p of source().products) assert.equal(xml.split(`/products/${p.slug}-${p.id}</loc>`).length - 1, 1);
});

test('every sitemap URL serves indexable HTML with a unique canonical and one H1', async () => {
  const xml = await (await fetch(base + '/sitemap.xml')).text();
  const urls = [...xml.matchAll(/<loc>([^<]+)<\/loc>/g)].map(match => match[1].replaceAll('&amp;', '&'));
  assert.ok(urls.length > 0);
  for (let offset = 0; offset < urls.length; offset += 12) {
    await Promise.all(urls.slice(offset, offset + 12).map(async url => {
      const response = await fetch(url);
      assert.equal(response.status, 200, url);
      const html = await response.text();
      assert.match(html, /<title>[^<]+<\/title>/, url + ': title missing');
      assert.match(html, /<meta name="description" content="[^"]+"\s*\/>/, url + ': description missing');
      assert.match(html, /<meta name="robots" content="index, follow"\s*\/>/, url + ': page is not indexable');
      assert.equal([...html.matchAll(/<link rel="canonical" href="([^"]+)"\s*\/>/g)].length, 1, url + ': canonical count');
      assert.equal(html.match(/<link rel="canonical" href="([^"]+)"\s*\/>/)?.[1], url, url + ': canonical mismatch');
      assert.equal([...html.matchAll(/<h1\b/g)].length, 1, url + ': expected one H1');
    }));
  }
});

test('referenced scripts, styles, product media, logo and flags are served locally', async () => {
  const html = await (await fetch(base)).text();
  const urls = new Set([...html.matchAll(/(?:src|href)="([^"?#]+)(?:\?[^"#]*)?"/g)].map(m => m[1])
    .filter(url => /(?:\.js|\.css)$/.test(url) && !url.startsWith('http')));
  const d = source(), m = d.media['product-1'];
  for (const url of [m.main, m.card, m.thumbnail, ...m.videos, d.brands.bosch.logo, d.countries.DE.flag]) urls.add('/' + url);
  for (const url of urls) {
    const response = await fetch(new URL(url, base));
    assert.equal(response.status, 200, url);
    assert.ok(!response.headers.get('content-type')?.includes('text/html'), url);
    await response.body?.cancel();
  }
});

test('images on every sitemap page, account page, catalog and loaded stylesheet resolve', async () => {
  const sitemap = await (await fetch(base + '/sitemap.xml')).text();
  const routes = new Set([...sitemap.matchAll(/<loc>([^<]+)<\/loc>/g)].map(m => m[1].replaceAll('&amp;', '&')));
  for (const route of ['/cart', '/checkout', '/compare', '/favorites', '/profile', '/profile/orders', '/profile/addresses', '/profile/favorites']) routes.add(base + route);
  const images = new Map(), styles = new Set();
  const addImage = (value, parent, owner) => {
    if (!value || /^(?:data:|#)/i.test(value)) return;
    const url = new URL(value.replaceAll('&amp;', '&'), parent);
    if (url.origin !== base || !/\.(?:png|jpe?g|webp|svg|gif|ico|avif)$/i.test(url.pathname)) return;
    if (!images.has(url.href)) images.set(url.href, new Set());
    images.get(url.href).add(owner);
  };
  for (const route of routes) {
    const response = await fetch(route);
    assert.equal(response.status, 200, route);
    const html = await response.text();
    const documentBase = new URL(html.match(/<base\b[^>]*href="([^"]+)"/i)?.[1] || route, route);
    for (const tag of html.matchAll(/<(?:img|source|video|meta)\b[^>]*>/gi)) {
      for (const attr of tag[0].matchAll(/\b(?:src|poster|content)="([^"]+)"/gi)) addImage(attr[1], documentBase, route);
      for (const attr of tag[0].matchAll(/\bsrcset="([^"]+)"/gi)) {
        for (const candidate of attr[1].split(',')) addImage(candidate.trim().split(/\s+/)[0], documentBase, route);
      }
    }
    for (const tag of html.matchAll(/<link\b[^>]*>/gi)) {
      if (!/rel="stylesheet"/i.test(tag[0])) continue;
      const href = tag[0].match(/href="([^"]+)"/i)?.[1];
      if (href) styles.add(new URL(href.replaceAll('&amp;', '&'), documentBase).href);
    }
  }
  for (const url of styles) {
    const response = await fetch(url);
    assert.equal(response.status, 200, url);
    for (const match of (await response.text()).matchAll(/url\(\s*["']?([^"'()]+)["']?\s*\)/g)) addImage(match[1].trim(), url, url);
  }
  const catalogImages = value => {
    if (typeof value === 'string') addImage('/' + value, base, 'shared catalog');
    else if (value && typeof value === 'object') Object.values(value).forEach(catalogImages);
  };
  catalogImages(source());
  const failures = [];
  const pending = [...images];
  await Promise.all(Array.from({length: 8}, async () => {
    while (pending.length) {
      const [url, owners] = pending.pop();
      const response = await fetch(url, {method: 'HEAD'});
      if (response.status !== 200 || !response.headers.get('content-type')?.startsWith('image/')) failures.push(`${response.status}: ${url} (${[...owners].join(', ')})`);
    }
  }));
  assert.deepEqual(failures, [], 'Broken images:\n' + failures.join('\n'));
  console.log(`Media audit: ${routes.size} pages, ${styles.size} stylesheets, ${images.size} unique images.`);
});
