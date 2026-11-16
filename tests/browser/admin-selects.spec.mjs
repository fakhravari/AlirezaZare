import { test, expect } from '@playwright/test';
import path from 'node:path';

test('admin Select2 keeps native events and modal controls working', async ({ page }) => {
  await page.setContent(`
    <select id="country"><option value="1">Iran</option><option value="2">Turkey</option><option value="3" hidden>Hidden</option></select>
    <select id="province"><option value="">Choose</option></select>
    <div class="modal" id="editor" tabindex="-1"><div class="modal-dialog"><div class="modal-content">
      <form id="editor-form"><select id="status"><option value="1">First</option><option value="2">Second</option></select></form>
    </div></div></div>
  `);
  for (const file of [
    'Admin/wwwroot/color_admin/assets/js/vendor.min.js',
    'Admin/wwwroot/lib/bootstrap/bootstrap.bundle.min.js',
    'Admin/wwwroot/color_admin/assets/plugins/select2/dist/js/select2.min.js',
    'Admin/wwwroot/color_admin/assets/plugins/select2/dist/js/i18n/fa.js',
    'Admin/wwwroot/js/admin-selects.js'
  ]) await page.addScriptTag({ path: path.resolve(file) });

  await page.evaluate(() => {
    window.changeCounts = { native: 0, jquery: 0 };
    const country = document.querySelector('#country');
    country.addEventListener('change', () => {
      window.changeCounts.native++;
      document.querySelector('#province').replaceChildren(new Option(country.value === '2' ? 'Ankara' : 'Tehran', country.value));
    });
    window.jQuery(country).on('change', () => window.changeCounts.jquery++);
  });
  await page.locator('#country + .select2').click();
  await expect(page.locator('.select2-results__option', { hasText: 'Hidden' })).toHaveCount(0);
  await page.locator('.select2-results__option', { hasText: 'Turkey' }).click();
  await expect(page.locator('#province + .select2 .select2-selection__rendered')).toHaveText('Ankara');
  expect(await page.evaluate(() => window.changeCounts)).toEqual({ native: 1, jquery: 1 });
  await page.evaluate(() => {
    const dynamic = document.createElement('select');
    dynamic.id = 'dynamic';
    dynamic.append(new Option('Dynamic option', '1'));
    document.body.append(dynamic);
  });
  await expect(page.locator('#dynamic + .select2 .select2-selection__rendered')).toHaveText('Dynamic option');
  await page.evaluate(() => { document.querySelector('#dynamic').disabled = true; });
  await expect(page.locator('#dynamic + .select2')).toHaveClass(/select2-container--disabled/);

  await page.evaluate(() => {
    document.querySelector('#status').value = '2';
    new window.bootstrap.Modal(document.querySelector('#editor')).show();
  });
  await expect(page.locator('#status + .select2 .select2-selection__rendered')).toHaveText('Second');
  await page.locator('#status + .select2').click();
  await expect(page.locator('#editor .select2-search__field')).toBeVisible();
  await page.evaluate(() => { window.jQuery('#status').select2('close'); document.querySelector('#editor-form').reset(); });
  await expect(page.locator('#status + .select2 .select2-selection__rendered')).toHaveText('First');
  await page.evaluate(() => { window.bootstrap.Modal.getInstance(document.querySelector('#editor')).hide(); });
  await expect(page.locator('#editor')).not.toHaveClass(/show/);
  await page.evaluate(() => {
    document.querySelector('#status').value = '2';
    window.bootstrap.Modal.getInstance(document.querySelector('#editor')).show();
  });
  await expect(page.locator('#status + .select2 .select2-selection__rendered')).toHaveText('Second');
});
