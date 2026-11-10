import {test, expect} from '@playwright/test';

test('product gallery switches between an image and video from catalog data', async ({page}, testInfo) => {
  let video;
  await page.route('**/products/cordless-hammer-drill-1', async route => {
    const response = await route.fetch();
    const html = await response.text();
    const cdnBase = html.match(/<meta name="media-cdn-base-url" content="([^"]+)"/)?.[1];
    if (!cdnBase) throw new Error('Media CDN base URL not found');
    video = new URL('content/products/videos/69b9f43c20c44fbb9c982cb170a89573.mp4', cdnBase).href;
    const updated = html.replace(/(<script type="application\/json" id="storeCatalog">)([\s\S]*?)(<\/script>)/, (_, start, json, end) => {
      const catalog = JSON.parse(json);
      const product = catalog.products.find(item => item.slug === 'cordless-hammer-drill');
      if (!product) throw new Error('Test product not found');
      catalog.media[product.mediaId].videos = [video];
      return start + JSON.stringify(catalog).replace(/</g, '\\u003c') + end;
    });
    if (updated === html) throw new Error('Catalog bootstrap not found');
    await route.fulfill({response, body: updated});
  });
  await page.goto('/products/cordless-hammer-drill-1');
  const videoButton = page.locator('[data-detail-media-index]').last();
  await expect(videoButton).toHaveAttribute('aria-label', /ویدئو/);
  await videoButton.click();
  await expect(page.locator('#productDetailMedia video source')).toHaveAttribute('src', video);
  const player = page.locator('#productDetailMedia video');
  await expect.poll(() => player.evaluate(element => element.readyState), {timeout: 15000}).toBeGreaterThanOrEqual(1);
  await expect.poll(() => player.evaluate(element => element.videoWidth)).toBeGreaterThan(0);
  await player.evaluate(element => element.play());
  await expect.poll(() => player.evaluate(element => element.currentTime)).toBeGreaterThan(0);
  await expect(videoButton).toHaveAttribute('aria-pressed', 'true');
  await page.locator('#openProductGallery').click();
  const dialog = page.locator('.product-gallery-dialog');
  await expect(dialog).toBeVisible();
  await expect(dialog.locator('video source')).toHaveAttribute('src', video);
  await page.screenshot({path: 'artifacts/ftp-check/gallery-' + testInfo.project.name + '.png'});
  await dialog.locator('[data-gallery-media-index="0"]').click();
  await expect(dialog.locator('#productGalleryFullStage img')).toBeVisible();
  await expect.poll(() => dialog.locator('#productGalleryFullStage img').evaluate(image => image.naturalWidth)).toBeGreaterThan(0);
  await expect(page.locator('#productDetailMedia img')).toBeVisible();
  await dialog.locator('[data-gallery-close]').click();
});



