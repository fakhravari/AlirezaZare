import {test, expect} from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
test('uploaded image and video render from SQL catalog', async ({page}, info) => {
 await page.route('**/content/products/**', async route => {
  const pathname=new URL(route.request().url()).pathname;
  const local=path.resolve('artifacts/media-ftp', '.'+pathname);
  if(fs.existsSync(local) && fs.statSync(local).isFile()) return route.fulfill({path:local});
  return route.continue();
 });
 await page.goto('/products/adjustable-soldering-iron-12');
 const main=page.locator('#productDetailMedia img');
 await expect.poll(() => main.evaluate(el => el.naturalWidth)).toBeGreaterThan(0);
 const videoButton=page.locator('[data-detail-media-index]').last();
 await expect(videoButton).toHaveAttribute('aria-label', /ویدئو/);
 await videoButton.click();
 const video=page.locator('#productDetailMedia video');
 await expect.poll(() => video.evaluate(el => el.readyState),{timeout:15000}).toBeGreaterThanOrEqual(1);
 await video.evaluate(el=>el.play());
 await expect.poll(() => video.evaluate(el=>el.currentTime)).toBeGreaterThan(0);
 await page.locator('#openProductGallery').click();
 await expect(page.locator('.product-gallery-dialog video')).toBeVisible();
 await page.screenshot({path:`artifacts/media-contract/gallery-${info.project.name}.png`});
});
