import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

const source = await readFile(new URL('../../Shop/wwwroot/shop/js/layout/main.js', import.meta.url), 'utf8');
const start = source.indexOf('  const heroSwiperElement =');
const end = source.indexOf('  const storage = window.HNUI.storage;', start);
assert.ok(start >= 0 && end > start, 'hero initialization is present');
const heroCode = source.slice(start, end);

function mount(slides, mobile = false) {
  const wrapper = { children: slides, replaceChildren(...items) { this.children = items; } };
  const hero = { style: {}, querySelector: () => wrapper, querySelectorAll: () => [] };
  const media = { matches: mobile, addEventListener(type, listener) { assert.equal(type, 'change'); this.change = listener; } };
  const instances = [];
  class Swiper {
    constructor(element, options) { this.element = element; this.options = options; instances.push(this); }
    destroy(clean, styles) { this.destroyed = [clean, styles]; }
  }
  vm.runInNewContext(heroCode, { document: { querySelector: () => hero }, window: { Swiper, matchMedia: () => media }, Swiper, prefersReducedMotion: false });
  return { wrapper, hero, media, instances };
}
const slide = (id, mobile, desktop) => ({ id, dataset: { showMobile: String(mobile), showDesktop: String(desktop) } });

test('hero switches device-specific slides and recreates the carousel', () => {
  const h = mount([slide('both', true, true), slide('desktop', false, true), slide('mobile', true, false)]);
  assert.deepEqual(Array.from(h.wrapper.children, x => x.id), ['both', 'desktop']);
  assert.equal(h.instances[0].options.loop, true);
  h.media.matches = true; h.media.change();
  assert.deepEqual(Array.from(h.wrapper.children, x => x.id), ['both', 'mobile']);
  assert.deepEqual(Array.from(h.instances[0].destroyed), [true, true]);
  h.media.matches = false; h.media.change();
  assert.deepEqual(Array.from(h.wrapper.children, x => x.id), ['both', 'desktop']);
});

test('hero hides without matching slides and does not loop a single slide', () => {
  const h = mount([slide('mobile', true, false)]);
  assert.equal(h.hero.style.display, 'none');
  assert.equal(h.instances.length, 0);
  h.media.matches = true; h.media.change();
  assert.equal(h.hero.style.display, '');
  assert.equal(h.wrapper.children.length, 1);
  assert.equal(h.instances[0].options.loop, false);
  h.media.matches = false; h.media.change();
  assert.equal(h.hero.style.display, 'none');
  assert.equal(h.wrapper.children.length, 0);
});
