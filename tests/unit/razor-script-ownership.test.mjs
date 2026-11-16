import assert from 'node:assert/strict';
import { readdir, readFile } from 'node:fs/promises';
import path from 'node:path';
import test from 'node:test';

async function razorFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const nested = await Promise.all(entries.map(entry => {
    const file = path.join(directory, entry.name);
    return entry.isDirectory() ? razorFiles(file) : entry.name.endsWith('.cshtml') ? [file] : [];
  }));
  return nested.flat();
}

test('Razor views load executable JavaScript from files', async () => {
  const files = (await Promise.all(['Shop/Views', 'Admin/Views', 'Admin/Areas'].map(razorFiles))).flat();
  const violations = [];
  for (const file of files) {
    const content = await readFile(file, 'utf8');
    for (const match of content.matchAll(/<script\b([^>]*)>([\s\S]*?)<\/script\s*>/gi)) {
      const [, attributes, body] = match;
      if (body.trim() && !/\btype\s*=\s*["']application\/(?:ld\+)?json["']/i.test(attributes)) violations.push(file);
    }
  }
  assert.deepEqual(violations, []);
  const profile = await readFile('Shop/Views/Profile/Index.cshtml', 'utf8');
  assert.match(profile, /<script src="~\/shop\/js\/pages\/profile-edit\.js" asp-append-version="true"><\/script>/);
  assert.ok(profile.indexOf('orders-pagination.js') < profile.indexOf('profile-edit.js'));
});
