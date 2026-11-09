import {test, expect} from '@playwright/test';
import {readFile} from 'node:fs/promises';

test('geography recovers from load failures without repeating a successful save', async ({page}) => {
  let html = await readFile('Shop.Admin/Areas/Store/Views/Features/Geography.cshtml', 'utf8');
  html = html.replace('@model Shop.Admin.Models.FeaturePageViewModel', '').replace('@await Html.PartialAsync("_AdminPageIntro", (Model.Title, Model.Description))', '').replace('@Html.AntiForgeryToken()', '<input name="__RequestVerificationToken" value="test">');
  await page.setContent(html);
  await page.evaluate(() => {
    window.geoTest = {getCount: 0, saves: 0, notifications: []};
    window.AdminUI = {notify: (message, kind) => window.geoTest.notifications.push({message, kind}), getJson: async (url, options) => {
      if (options.method === 'GET') {
        window.geoTest.getCount++;
        if (window.geoTest.getCount === 1 || window.geoTest.getCount === 3) throw new Error('شبکه قطع است');
        return {countries: [{id: 1, nameFa: 'ایران', nameEn: 'Iran', code: 'IR'}], provinces: [], cities: []};
      }
      window.geoTest.saves++;
      return {message: 'ذخیره شد'};
    }};
  });
  await page.addScriptTag({content: await readFile('Shop.Admin/wwwroot/js/admin-geography.js', 'utf8')});
  await page.evaluate(() => window.AdminGeography.initialize(document));
  const retry = page.locator('[data-geo-retry]'), submit = page.locator('[data-geo-form] [type=submit]');
  await expect(retry).toBeVisible(); await expect(submit).toBeDisabled();
  await retry.click(); await expect(retry).toBeHidden(); await expect(submit).toBeEnabled();
  await page.locator('#geo-name').fill('کشور جدید'); await page.locator('#geo-name-en').fill('New country'); await page.locator('#geo-code').fill('XX');
  await submit.click(); await expect(retry).toBeVisible(); await expect(submit).toBeDisabled();
  expect(await page.evaluate(() => window.geoTest.saves)).toBe(1);
  await retry.click(); await expect(submit).toBeEnabled();
  expect(await page.evaluate(() => window.geoTest.saves)).toBe(1);
  await page.locator('[data-geo-tab=countries]').press('ArrowLeft');
  await expect(page.locator('[data-geo-tab=provinces]')).toHaveAttribute('aria-selected', 'true');
  await expect(page.locator('#geo-name')).toHaveAttribute('maxlength', '150');
  await page.locator('[data-geo-tab=cities]').click();
  await expect(page.locator('#geo-name')).toHaveAttribute('maxlength', '200');
});
