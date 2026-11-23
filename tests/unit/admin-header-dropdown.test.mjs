import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { readFile } from 'node:fs/promises';
import { chromium } from '@playwright/test';

test('admin account dropdown opens and closes with the shipped vendor runtime', { skip: !existsSync(chromium.executablePath()) }, async t => {
  const layout = await readFile('Admin/Views/Shared/_Layout.cshtml', 'utf8');
  const header = await readFile('Admin/Views/Shared/_AdminHeader.cshtml', 'utf8');
  assert.match(layout, /color_admin\/assets\/js\/vendor\.min\.js/);
  assert.doesNotMatch(layout, /lib\/bootstrap\/bootstrap\.bundle\.min\.js/);
  assert.match(header, /asp-area="" asp-controller="Auth" asp-action="Logout" method="post"/);
  assert.match(header, /@Html\.AntiForgeryToken\(\)/);

  const browser = await chromium.launch({ headless: true });
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent('<div class="dropdown"><button data-bs-toggle="dropdown" aria-expanded="false">حساب</button><div class="dropdown-menu"><button>خروج</button></div></div>');
  await page.addScriptTag({ path: 'Admin/wwwroot/color_admin/assets/js/vendor.min.js' });
  const toggle = page.locator('[data-bs-toggle="dropdown"]');
  await toggle.click();
  assert.equal(await toggle.getAttribute('aria-expanded'), 'true');
  assert.equal(await page.locator('.dropdown-menu').evaluate(element => element.classList.contains('show')), true);
  await page.keyboard.press('Escape');
  assert.equal(await toggle.getAttribute('aria-expanded'), 'false');
});
