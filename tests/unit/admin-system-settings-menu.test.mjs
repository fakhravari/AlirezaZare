import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

test('general settings appears in the system menu only for full-access users', async () => {
  const sidebar = await readFile('Admin/Views/Shared/_AdminSidebar.cshtml', 'utf8');
  const systemHeader = sidebar.indexOf('<summary class="menu-header">مدیریت سامانه</summary>');
  const storeHeader = sidebar.indexOf('<summary class="menu-header">مدیریت فروشگاه</summary>');
  const generalSettings = sidebar.indexOf('<span class="menu-text">تنظیمات عمومی</span>');
  assert.ok(systemHeader >= 0 && systemHeader < generalSettings && generalSettings < storeHeader);
  const systemMenu = sidebar.slice(systemHeader, storeHeader);
  assert.match(systemMenu, /@if \(User\.IsInRole\("system\.full-access"\)\)[\s\S]*?asp-controller="SiteSettings" asp-action="Index"/);
  assert.equal(sidebar.split('<span class="menu-text">تنظیمات عمومی</span>').length - 1, 1);
  assert.ok(sidebar.indexOf('asp-action="Slider"') > storeHeader);
});

test('geography appears in the system menu only for full-access users', async () => {
  const sidebar = await readFile('Admin/Views/Shared/_AdminSidebar.cshtml', 'utf8');
  const systemHeader = sidebar.indexOf('<summary class="menu-header">مدیریت سامانه</summary>');
  const storeHeader = sidebar.indexOf('<summary class="menu-header">مدیریت فروشگاه</summary>');
  const geography = sidebar.indexOf('<span class="menu-text">مناطق جغرافیایی</span>');
  assert.ok(systemHeader >= 0 && systemHeader < geography && geography < storeHeader);
  const systemMenu = sidebar.slice(systemHeader, storeHeader);
  assert.match(systemMenu, /@if \(User\.IsInRole\("system\.full-access"\)\)[\s\S]*?asp-route-feature="geography"/);
  assert.equal(sidebar.split('asp-route-feature="geography"').length - 1, 1);
});

test('slider appears in the content submenu and opens it on its page', async () => {
  const sidebar = await readFile('Admin/Views/Shared/_AdminSidebar.cshtml', 'utf8');
  const contentHeader = sidebar.indexOf('<summary class="menu-header">محتوا</summary>');
  const contentMenu = sidebar.indexOf('<div id="content-menu" class="menu-submenu">');
  const slider = sidebar.indexOf('asp-controller="SiteSettings" asp-action="Slider"');
  const warehouseHeader = sidebar.indexOf('<summary class="menu-header">مدیریت انبار</summary>');
  assert.ok(contentHeader >= 0 && contentHeader < contentMenu && contentMenu < slider && slider < warehouseHeader);
  assert.match(sidebar, /bool contentMenuOpen = IsFeature\("content"\) \|\| sliderOpen;/);
  assert.match(sidebar, /aria-expanded="@\(contentMenuOpen \? "true" : "false"\)"/);
  assert.equal(sidebar.split('asp-action="Slider"').length - 1, 1);
});

test('top-level sidebar groups are collapsible and initially closed', async () => {
  const sidebar = await readFile('Admin/Views/Shared/_AdminSidebar.cshtml', 'utf8');
  const groups = [...sidebar.matchAll(/<details class="admin-sidebar-group"([^>]*)>\s*<summary class="menu-header">([^<]+)<\/summary>/g)];
  assert.deepEqual(groups.map(group => group[2]), ['مدیریت سامانه', 'مدیریت فروشگاه', 'محتوا', 'مدیریت انبار']);
  for (const group of groups) assert.doesNotMatch(group[1], /\sopen(?:\s|=|$)/);
});
