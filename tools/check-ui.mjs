import fs from 'node:fs/promises';
import path from 'node:path';
import vm from 'node:vm';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
import postcss from 'postcss';

const root = fileURLToPath(new URL('../', import.meta.url));
const read = file => fs.readFile(path.join(root, file), 'utf8');
const owners = JSON.parse(await read('tools/ui-owners.json')).components;
const baseline = JSON.parse(await read('tools/ui-legacy-budget.json'));
const errors = [];
const exists = async file => { try { await fs.access(path.join(root, file)); return true; } catch { return false; } };
async function files(directory, suffix) {
  const list = [];
  for (const entry of await fs.readdir(path.join(root, directory), {withFileTypes: true})) {
    const file = directory + '/' + entry.name;
    if (entry.isDirectory()) list.push(...await files(file, suffix));
    else if (file.endsWith(suffix)) list.push(file);
  }
  return list;
}
for (const [component, owner] of Object.entries(owners)) {
  for (const kind of ['markup', 'data', 'server', 'js', 'css', 'pageOverrides']) {
    for (const file of owner[kind] || []) if (!await exists(file)) errors.push(`${component}: missing ${kind} owner ${file}`);
  }
}
const scripts = await files('Shop/wwwroot/shop/js', '.js');
for (const file of scripts) {
  try { new vm.Script(await read(file), {filename: file}); }
  catch (error) { errors.push(file + ': ' + error.message); }
}
const styles = await files('Shop/wwwroot/shop/css', '.css');
for (const file of styles) {
  try {
    const css = postcss.parse(await read(file), {from: file});
    let important = 0;
    css.walkDecls(decl => { if (decl.important) important++; });
    if (important > (baseline.important[file] || 0)) errors.push(`${file}: !important budget increased (${important})`);
    css.walkRules(rule => {
      for (const [name, owner] of Object.entries(owners)) {
        if (!owner.selector || !new RegExp(owner.selector).test(rule.selector)) continue;
        if ([...(owner.css || []), ...(owner.pageOverrides || [])].includes(file)) continue;
        // Existing debt is allowed unchanged or removed. A new override belongs in its owner.
        const parents = []; for (let p = rule.parent; p.type !== 'root'; p = p.parent) parents.unshift(p.name + ' ' + p.params);
        const signature = createHash('sha256').update((file + '\n' + parents.join('\n') + '\n' + rule.toString()).replace(/\r\n/g, '\n')).digest('hex');
        if (!baseline.componentRules[name]?.includes(signature)) errors.push(`${file}:${rule.source.start.line}: ${name} rule belongs in its owner`);
      }
      if (/\.(?:brand-dropdown|category-nav-dropdown|quick-links-dropdown):is\([^)]*(?::hover|:focus-within)/.test(rule.selector))
        errors.push(`${file}:${rule.source.start.line}: dropdown opening must use explicit JS state`);
    });
  } catch (error) { errors.push(String(error)); }
}
const layout = await read('Shop/Views/Shared/_Shop.cshtml');
const dependencies = ['core/storage.js', 'store/cart.js', 'core/ui-core.js', 'store/catalog.js', 'store/product-card.js', 'layout/header-navigation.js', 'layout/main.js', 'store/commerce.js'];
let previous = -1;
for (const file of dependencies) {
  const marker = `~/shop/js/${file}`;
  const index = layout.indexOf(marker);
  if (index < previous || index === -1 || layout.split(marker).length !== 2) errors.push(`Invalid shared script order/duplicate: ${file}`);
  previous = index;
}
if (errors.length) { console.error(errors.join('\n')); process.exitCode = 1; }
else console.log(`UI contracts OK: ${scripts.length} scripts, ${styles.length} styles; owners, legacy budgets and load order verified.`);
