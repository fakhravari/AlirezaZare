import {test, expect} from '@playwright/test';
import {readFile} from 'node:fs/promises';

test('lists show all stored geography fields', async ({page}) => {
  let html = await readFile('Admin/Areas/Store/Views/Features/Geography.cshtml', 'utf8');
  html = html.replace('@model Shop.Admin.Models.FeaturePageViewModel', '').replace('@await Html.PartialAsync("_AdminPageIntro", (Model.Title, Model.Description))', '').replace('@Html.AntiForgeryToken()', '<input name="__RequestVerificationToken" value="test">');
  await page.setContent(html);
  await page.evaluate(() => { window.AdminUI = {notify: () => {}, destroyDataTable: () => {}, refreshDataTable: () => {}, getJson: async () => ({
    countries: [{id: 1, nameFa: 'ایران', nameEn: 'Iran', code: 'IR', flagPath: 'content/flags/iran.webp'}],
    provinces: [{id: 2, countryId: 1, nameFa: 'فارس', nameEn: 'Fars', parentName: 'ایران', latitude: 29.5, longitude: 52.5}],
    cities: [{id: 3, countryId: 1, provinceId: 2, nameFa: 'شیراز', nameEn: 'Shiraz', parentName: 'ایران / فارس', sourceCountryName: 'Iran source', latitude: 29.6, longitude: 52.6, isProvinceCenter: true}]
  })}; });
  await page.addScriptTag({content: await readFile('Admin/wwwroot/js/admin-geography.js', 'utf8')});
  await page.evaluate(() => window.AdminGeography.initialize(document));
  await expect(page.locator('[data-geo-rows]')).toContainText('content/flags/iran.webp');
  await page.locator('[data-geo-tab=provinces]').click();
  await expect(page.locator('[data-geo-rows]')).toContainText('29.5');
  await page.locator('[data-geo-tab=cities]').click();
  await expect(page.locator('[data-geo-rows]')).toContainText('Iran source');
  await expect(page.locator('[data-geo-rows]')).toContainText('بله');
  await expect(page.locator('thead')).toContainText('شناسه استان');
  await expect(page.locator('thead')).toContainText('CountryName خام منبع');
});

test('geography recovers from load failures without repeating a successful save', async ({page}) => {
  let html = await readFile('Admin/Areas/Store/Views/Features/Geography.cshtml', 'utf8');
  html = html.replace('@model Shop.Admin.Models.FeaturePageViewModel', '').replace('@await Html.PartialAsync("_AdminPageIntro", (Model.Title, Model.Description))', '').replace('@Html.AntiForgeryToken()', '<input name="__RequestVerificationToken" value="test">');
  await page.setContent(html);
  await page.evaluate(() => {
    window.geoTest = {getCount: 0, saves: 0, notifications: []};
    window.AdminUI = {notify: (message, kind) => window.geoTest.notifications.push({message, kind}), destroyDataTable: () => {}, refreshDataTable: () => {}, getJson: async (url, options) => {
      if (options.method === 'GET') {
        window.geoTest.getCount++;
        if (window.geoTest.getCount === 1 || window.geoTest.getCount === 3) throw new Error('شبکه قطع است');
        return {countries: [{id: 1, nameFa: 'ایران', nameEn: 'Iran', code: 'IR'}], provinces: [], cities: []};
      }
      window.geoTest.saves++;
      return {message: 'ذخیره شد'};
    }};
  });
  await page.addScriptTag({content: await readFile('Admin/wwwroot/js/admin-geography.js', 'utf8')});
  await page.evaluate(() => window.AdminGeography.initialize(document));
  const retry = page.locator('[data-geo-retry]'), submit = page.locator('[data-geo-form] [type=submit]');
  await expect(retry).toBeVisible(); await expect(submit).toBeDisabled();
  const failedStatus = await page.locator("[data-geo-status]").innerText();
  await page.locator("[data-geo-search]").fill("ایران");
  await expect(page.locator("[data-geo-status]")).toHaveText(failedStatus);
  await expect(page.locator("[data-geo-rows] tr")).toHaveCount(0);
  await page.locator("[data-geo-search]").clear();
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


test('uncertain network result refreshes the list before another save', async ({page}) => {
  let html = await readFile('Admin/Areas/Store/Views/Features/Geography.cshtml', 'utf8');
  html = html.replace('@model Shop.Admin.Models.FeaturePageViewModel', '').replace('@await Html.PartialAsync("_AdminPageIntro", (Model.Title, Model.Description))', '').replace('@Html.AntiForgeryToken()', '<input name="__RequestVerificationToken" value="test">');
  await page.setContent(html);
  await page.evaluate(() => {
    window.geoTest = {saves: 0, notifications: [], countries: []};
    window.AdminUI = {notify: message => window.geoTest.notifications.push(message), destroyDataTable: () => {}, refreshDataTable: () => {}, getJson: async (url, options) => {
      if (options.method === 'GET') return {countries: [...window.geoTest.countries], provinces: [], cities: []};
      window.geoTest.saves++;
      window.geoTest.countries.push({id: 8, nameFa: 'کشور آزمایشی', nameEn: 'Test', code: 'XX'});
      throw new TypeError('Network connection lost');
    }};
  });
  await page.addScriptTag({content: await readFile('Admin/wwwroot/js/admin-geography.js', 'utf8')});
  await page.evaluate(() => window.AdminGeography.initialize(document));
  const submit = page.locator('[data-geo-form] [type=submit]'); await expect(submit).toBeEnabled();
  await page.locator('#geo-name').fill('کشور آزمایشی'); await page.locator('#geo-name-en').fill('Test'); await page.locator('#geo-code').fill('XX');
  await submit.click();
  await expect(page.locator('[data-geo-rows]')).toContainText('کشور آزمایشی');
  await expect(page.locator('#geo-name')).toHaveValue('');
  expect(await page.evaluate(() => window.geoTest.saves)).toBe(1);
  expect(await page.evaluate(() => window.geoTest.notifications.some(text => text.includes('نامشخص')))).toBe(true);
});

test('province list follows the selected country and combines with search', async ({page}) => {
  let html = await readFile('Admin/Areas/Store/Views/Features/Geography.cshtml', 'utf8');
  html = html.replace('@model Shop.Admin.Models.FeaturePageViewModel', '').replace('@await Html.PartialAsync("_AdminPageIntro", (Model.Title, Model.Description))', '').replace('@Html.AntiForgeryToken()', '<input name="__RequestVerificationToken" value="test">');
  await page.setContent(html);
  await page.evaluate(() => {
    window.AdminUI = {notify: () => {}, destroyDataTable: () => {}, refreshDataTable: () => {}, getJson: async () => ({
      countries: [{id: 1, nameFa: 'ایران', nameEn: 'Iran', code: 'IR'}, {id: 2, nameFa: 'ترکیه', nameEn: 'Turkey', code: 'TR'}],
      provinces: [{id: 11, countryId: 1, nameFa: 'تهران', nameEn: 'Tehran', parentName: 'ایران'}, {id: 12, countryId: 1, nameFa: 'فارس', nameEn: 'Fars', parentName: 'ایران'}, {id: 21, countryId: 2, nameFa: 'استانبول', nameEn: 'Istanbul', parentName: 'ترکیه'}], cities: []
    })};
  });
  await page.addScriptTag({content: await readFile('Admin/wwwroot/js/admin-geography.js', 'utf8')});
  await page.evaluate(() => window.AdminGeography.initialize(document));
  await expect(page.locator('[data-geo-form] [type=submit]')).toBeEnabled();
  await page.locator('[data-geo-tab=provinces]').click();
  const rows = page.locator('[data-geo-rows] tr'), country = page.locator('#geo-country');
  await expect(rows).toHaveCount(3);
  await country.selectOption('1'); await expect(rows).toHaveCount(2); await expect(page.locator('[data-geo-rows]')).not.toContainText('استانبول');
  await page.locator('#geo-search').fill('تهران'); await expect(rows).toHaveCount(1);
  await country.selectOption('2'); await expect(rows).toHaveCount(0);
  await page.locator('#geo-search').clear(); await expect(rows).toHaveCount(1); await expect(rows.first()).toContainText('استانبول');
  await page.locator('[data-geo-cancel]').click(); await expect(country).toHaveValue('2'); await expect(rows).toHaveCount(1);
  await country.selectOption(''); await expect(rows).toHaveCount(3);
});

test('city list follows country and province selections', async ({page}) => {
  let html = await readFile('Admin/Areas/Store/Views/Features/Geography.cshtml', 'utf8');
  html = html.replace('@model Shop.Admin.Models.FeaturePageViewModel', '').replace('@await Html.PartialAsync("_AdminPageIntro", (Model.Title, Model.Description))', '').replace('@Html.AntiForgeryToken()', '<input name="__RequestVerificationToken" value="test">');
  await page.setContent(html);
  await page.evaluate(() => {
    window.AdminUI = {notify: () => {}, destroyDataTable: () => {}, refreshDataTable: () => {}, getJson: async () => ({
      countries: [{id: 1, nameFa: 'ایران', nameEn: 'Iran'}, {id: 2, nameFa: 'ترکیه', nameEn: 'Turkey'}],
      provinces: [{id: 11, countryId: 1, nameFa: 'تهران'}, {id: 12, countryId: 1, nameFa: 'فارس'}, {id: 21, countryId: 2, nameFa: 'استانبول'}],
      cities: [{id: 101, countryId: 1, provinceId: 11, nameFa: 'تهران', nameEn: 'Tehran'}, {id: 102, countryId: 1, provinceId: 12, nameFa: 'شیراز', nameEn: 'Shiraz'}, {id: 103, countryId: 2, provinceId: 21, nameFa: 'استانبول', nameEn: 'Istanbul'}]
    })};
  });
  await page.addScriptTag({content: await readFile('Admin/wwwroot/js/admin-geography.js', 'utf8')});
  await page.evaluate(() => window.AdminGeography.initialize(document));
  await expect(page.locator('[data-geo-form] [type=submit]')).toBeEnabled();
  await page.locator('[data-geo-tab=cities]').click();
  const rows = page.locator('[data-geo-rows] tr'), country = page.locator('#geo-country'), province = page.locator('#geo-province');
  await expect(rows).toHaveCount(3);
  await page.locator('#geo-country-search').fill('ترک');
  await expect(country.locator('option[value="1"]')).toHaveJSProperty('hidden', true);
  await expect(country.locator('option[value="2"]')).toHaveJSProperty('hidden', false);
  await page.locator('#geo-country-search').clear();
  await country.selectOption('1'); await expect(rows).toHaveCount(2);
  await page.locator('#geo-province-search').fill('فارس');
  await expect(province.locator('option[value="11"]')).toHaveJSProperty('hidden', true);
  await expect(province.locator('option[value="12"]')).toHaveJSProperty('hidden', false);
  await province.selectOption('12'); await expect(rows).toHaveCount(1); await expect(rows.first()).toContainText('شیراز');
  await page.locator('#geo-search').fill('تهران'); await expect(rows).toHaveCount(0);
  await page.locator('#geo-search').clear();
  await page.locator('[data-geo-cancel]').click(); await expect(country).toHaveValue('1'); await expect(province).toHaveValue('12'); await expect(rows).toHaveCount(1);
  await country.selectOption('2'); await expect(province).toHaveValue(''); await expect(rows).toHaveCount(1); await expect(rows.first()).toContainText('استانبول');
  await country.selectOption(''); await expect(province).toHaveValue(''); await expect(rows).toHaveCount(3);
});
