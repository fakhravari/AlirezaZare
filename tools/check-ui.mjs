import fs from 'node:fs/promises';
import path from 'node:path';
import vm from 'node:vm';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
import postcss from 'postcss';

const root = fileURLToPath(new URL('../', import.meta.url));
const read = file => fs.readFile(path.join(root, file), 'utf8');
const ownerMap = JSON.parse(await read('tools/ui-owners.json'));
const errors = [];
let owners = ownerMap.components;
if (ownerMap.schemaVersion >= 2) {
  owners = {};
  for (const [groupName, group] of Object.entries(ownerMap.groups || {})) {
    if (!group.label || !group.components || typeof group.components !== 'object') {
      errors.push(`Invalid UI owner group: ${groupName}`);
      continue;
    }
    for (const [component, owner] of Object.entries(group.components)) {
      if (owners[component]) errors.push(`Duplicate UI owner id across groups: ${component}`);
      else owners[component] = owner;
    }
  }
}
const baseline = JSON.parse(await read('tools/ui-legacy-budget.json'));
const themeTokensFile = 'Shop/wwwroot/shop/css/core/design-tokens.css';
const publicThemeToken = /^--(?:color-|font-size-|text-(?:xs|sm|md|base|lg|xl)$|space-|radius-|shadow-(?:card|card-hover)|ui-(?:navy|gold|surface|text|muted|border|radius-|shadow-|ease)|md-|mega-)/;
const shorthandForLonghand = {
  'border-top-left-radius': ['border-radius'],
  'border-top-right-radius': ['border-radius'],
  'border-bottom-left-radius': ['border-radius'],
  'border-bottom-right-radius': ['border-radius'],
  'border-top-color': ['border-top', 'border'], 'border-top-style': ['border-top', 'border'], 'border-top-width': ['border-top', 'border'],
  'border-right-color': ['border-right', 'border'], 'border-right-style': ['border-right', 'border'], 'border-right-width': ['border-right', 'border'],
  'border-bottom-color': ['border-bottom', 'border'], 'border-bottom-style': ['border-bottom', 'border'], 'border-bottom-width': ['border-bottom', 'border'],
  'border-left-color': ['border-left', 'border'], 'border-left-style': ['border-left', 'border'], 'border-left-width': ['border-left', 'border'],
  'border-color': ['border'], 'border-style': ['border'], 'border-width': ['border'],
  'padding-top': ['padding'], 'padding-right': ['padding'], 'padding-bottom': ['padding'], 'padding-left': ['padding'],
  'margin-top': ['margin'], 'margin-right': ['margin'], 'margin-bottom': ['margin'], 'margin-left': ['margin'],
  top: ['inset'], right: ['inset'], bottom: ['inset'], left: ['inset'],
  'overflow-x': ['overflow'], 'overflow-y': ['overflow'],
  'row-gap': ['gap'], 'column-gap': ['gap'],
  'flex-grow': ['flex'], 'flex-shrink': ['flex'], 'flex-basis': ['flex'],
  'outline-color': ['outline'], 'outline-style': ['outline'], 'outline-width': ['outline'],
  'background-color': ['background'], 'background-image': ['background'],
  'background-position': ['background'], 'background-position-x': ['background'], 'background-position-y': ['background'],
  'background-repeat': ['background'], 'background-size': ['background'], 'background-attachment': ['background'],
  'background-origin': ['background'], 'background-clip': ['background'],
  'font-family': ['font'], 'font-size': ['font'], 'font-style': ['font'], 'font-weight': ['font'],
  'font-variant': ['font'], 'font-stretch': ['font'], 'line-height': ['font'],
  'list-style-type': ['list-style'], 'list-style-position': ['list-style'], 'list-style-image': ['list-style'],
  'column-width': ['columns'], 'column-count': ['columns'],
  'column-rule-width': ['column-rule'], 'column-rule-style': ['column-rule'], 'column-rule-color': ['column-rule'],
  'transition-property': ['transition'], 'transition-duration': ['transition'],
  'transition-timing-function': ['transition'], 'transition-delay': ['transition'],
  'animation-name': ['animation'], 'animation-duration': ['animation'],
  'animation-timing-function': ['animation'], 'animation-delay': ['animation'],
  'animation-iteration-count': ['animation'], 'animation-direction': ['animation'],
  'animation-fill-mode': ['animation'], 'animation-play-state': ['animation']
};
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
  for (const kind of ['markup', 'data', 'server', 'js', 'css', 'assets', 'pageOverrides']) {
    for (const file of owner[kind] || []) if (!await exists(file)) errors.push(`${component}: missing ${kind} owner ${file}`);
  }
}
const scripts = await files('Shop/wwwroot/shop/js', '.js');
const styles = await files('Shop/wwwroot/shop/css', '.css');
const ownedAssets = new Set(Object.values(owners).flatMap(owner => [...(owner.css || []), ...(owner.js || [])]));
for (const file of [...scripts, ...styles]) {
  if (!ownedAssets.has(file)) errors.push(`${file}: missing CSS/JS owner in tools/ui-owners.json`);
}
for (const file of scripts) {
  try { new vm.Script(await read(file), {filename: file}); }
  catch (error) { errors.push(file + ': ' + error.message); }
}
for (const file of styles) {
  try {
    const css = postcss.parse(await read(file), {from: file});
    let important = 0;
    const declarationsByCascadeKey = new Map();
    css.walkDecls(decl => { if (decl.important) important++; });
    if (important > (baseline.important[file] || 0)) errors.push(`${file}: !important budget increased (${important})`);
    css.walkRules(rule => {
      if (rule.selector.trim() === ':root' && file !== themeTokensFile) {
        rule.walkDecls(declaration => {
          if (publicThemeToken.test(declaration.prop))
            errors.push(`${file}:${declaration.source.start.line}: public theme token ${declaration.prop} belongs in ${themeTokensFile}`);
        });
      }
      const conditions = [];
      const selectorPath = [];
      for (let parent = rule.parent; parent.type !== 'root'; parent = parent.parent) {
        if (parent.type === 'atrule') conditions.unshift(`${parent.name} ${parent.params}`);
        if (parent.type === 'rule') selectorPath.unshift(parent.selector);
      }
      selectorPath.push(rule.selector);
      const declarations = (rule.nodes || []).filter(node => node.type === 'decl');
      for (let index = 0; index < declarations.length; index++) {
        const declaration = declarations[index];
        if (declaration.type !== 'decl') continue;
        const coveringShorthands = shorthandForLonghand[declaration.prop] || [];
        const coveringShorthand = coveringShorthands.find(shorthand => declarations.slice(index + 1).some(
          later => later.important && later.prop === shorthand
        ));
        if (declaration.important && coveringShorthand) {
          errors.push(`${file}:${declaration.source.start.line}: shadowed !important ${declaration.prop}; a later ${coveringShorthand} shorthand has equal priority`);
        }
        const key = `${conditions.join('\n')}\0${selectorPath.join('\n')}\0${declaration.prop}`;
        const earlier = declarationsByCascadeKey.get(key);
        if (declaration.important && earlier) {
          const kind = earlier.important ? 'shadowed !important' : 'shadowed declaration';
          errors.push(`${file}:${declaration.source.start.line}: ${kind} ${declaration.prop}; same selector and conditions already declare it at line ${earlier.line}`);
        }
        declarationsByCascadeKey.set(key, {line: declaration.source.start.line, important: declaration.important});
      }
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
const sharedStyles = [...layout.matchAll(/<link\b[^>]*href="~\/(shop\/css\/[^\"]+\.css)"[^>]*>/gs)].map(match => `Shop/wwwroot/${match[1]}`);
const sharedImportant = new Map();
const industrialCascade = new Map();
const reportedIndustrialShadows = new Set();
for (const file of sharedStyles) {
  try {
    const css = postcss.parse(await read(file), {from: file});
    css.walkRules(rule => {
      const conditions = [];
      const selectorPath = [];
      for (let parent = rule.parent; parent.type !== 'root'; parent = parent.parent) {
        if (parent.type === 'atrule') conditions.unshift(`${parent.name} ${parent.params}`);
        if (parent.type === 'rule') selectorPath.unshift(parent.selector);
      }
      selectorPath.push(rule.selector);
      rule.walkDecls(declaration => {
        const cascadeKey = `${conditions.join('\n')}\0${selectorPath.join('\n')}\0${declaration.prop}`;
        if (file.endsWith('/layout/industrial-system.css')) {
          industrialCascade.set(cascadeKey, {file, line: declaration.source.start.line, important: declaration.important});
        } else {
          const earlierIndustrial = industrialCascade.get(cascadeKey);
          if (earlierIndustrial && (declaration.important || !earlierIndustrial.important) && !reportedIndustrialShadows.has(cascadeKey)) {
            errors.push(`${earlierIndustrial.file}:${earlierIndustrial.line}: shadowed declaration ${declaration.prop}; same selector and conditions later win in ${file}:${declaration.source.start.line}`);
            reportedIndustrialShadows.add(cascadeKey);
          }
        }
        if (!declaration.important) return;
        const key = `${conditions.join('\n')}\0${selectorPath.join('\n')}\0${declaration.prop}\0${declaration.value.trim()}`;
        const earlier = sharedImportant.get(key);
        if (earlier && earlier.file !== file)
          errors.push(`${file}:${declaration.source.start.line}: duplicate shared !important ${declaration.prop}; same selector, conditions and value already win in ${earlier.file}:${earlier.line}`);
        sharedImportant.set(key, {file, line: declaration.source.start.line});
      });
    });
  } catch (error) { errors.push(`${file}: ${error.message}`); }
}
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
