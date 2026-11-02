const fs = require("node:fs");
const path = require("node:path");
const postcss = require("postcss");

const root = "Shop/wwwroot/shop/css";
const targets = new Set([
  "cart-quantity",
  "cart-item-controls",
  "cart-item-remove",
  "cart-line-item",
  "cart-empty-benefits",
  "cart-empty",
]);
const stats = { files: 0, rules: 0, declarations: 0, important: 0, touched: [] };

function matchingClose(text, open) {
  let depth = 0;
  let quote = null;
  let escaped = false;
  for (let i = open; i < text.length; i++) {
    const char = text[i];
    if (quote) {
      if (escaped) escaped = false;
      else if (char === "\\") escaped = true;
      else if (char === quote) quote = null;
      continue;
    }
    if (char === "'" || char === '"') quote = char;
    else if (char === "(") depth++;
    else if (char === ")" && --depth === 0) return i;
  }
  return -1;
}

function splitList(text) {
  return postcss.list.comma(text).map((item) => item.trim()).filter(Boolean);
}

function formatList(items, original) {
  return items.join(original.includes("\n") ? ",\n  " : ", ");
}

function hasDirectTarget(text) {
  let bracket = 0;
  let quote = null;
  let escaped = false;
  for (let i = 0; i < text.length; i++) {
    const char = text[i];
    if (quote) {
      if (escaped) escaped = false;
      else if (char === "\\") escaped = true;
      else if (char === quote) quote = null;
      continue;
    }
    if (char === "'" || char === '"') quote = char;
    else if (char === "[") bracket++;
    else if (char === "]") bracket--;
    else if (char === ":" && !bracket && text[i + 1] !== ":") {
      const name = text.slice(i + 1).match(/^[-_a-zA-Z][-_a-zA-Z0-9]*/)?.[0];
      if (name && text[i + 1 + name.length] === "(") {
        const end = matchingClose(text, i + 1 + name.length);
        if (end >= 0) i = end;
      }
    } else if (char === "." && !bracket) {
      const name = text.slice(i + 1).match(/^[-_a-zA-Z][-_a-zA-Z0-9]*/)?.[0];
      if (name && targets.has(name) && (i === 0 || !/[\w-]/.test(text[i - 1]))) return true;
    }
  }
  return false;
}

function cleanBranch(text) {
  let output = "";
  let changed = false;
  for (let i = 0; i < text.length;) {
    if (text[i] === ":" && text[i + 1] !== ":") {
      const name = text.slice(i + 1).match(/^[-_a-zA-Z][-_a-zA-Z0-9]*/)?.[0];
      const open = name ? i + 1 + name.length : -1;
      const end = open >= 0 && text[open] === "(" ? matchingClose(text, open) : -1;
      if (end >= 0) {
        if (name.toLowerCase() === "is" || name.toLowerCase() === "where") {
          const inner = text.slice(open + 1, end);
          const original = splitList(inner);
          const cleaned = original.map(cleanBranch).filter((item) => !item.removed);
          const nestedChange = cleaned.length !== original.length || cleaned.some((item) => item.changed);
          if (!cleaned.length) return { text: "", removed: true, changed: true };
          output += nestedChange
            ? `${text.slice(i, open + 1)}${formatList(cleaned.map((item) => item.text), inner)})`
            : text.slice(i, end + 1);
          changed ||= nestedChange;
        } else {
          output += text.slice(i, end + 1);
        }
        i = end + 1;
        continue;
      }
    }
    output += text[i++];
  }
  if (hasDirectTarget(output)) return { text: "", removed: true, changed: true };
  return { text: output.trim(), removed: false, changed };
}

function cssFiles(dir) {
  return fs.readdirSync(dir, { withFileTypes: true })
    .flatMap((entry) => entry.isDirectory() ? cssFiles(path.join(dir, entry.name)) : [path.join(dir, entry.name)])
    .filter((file) => file.endsWith(".css"));
}

for (const file of cssFiles(root)) {
  const input = fs.readFileSync(file, "utf8");
  const ast = postcss.parse(input, { from: file });
  let touched = false;
  ast.walkRules((rule) => {
    const original = rule.selector;
    const branches = splitList(original);
    const cleaned = branches.map(cleanBranch).filter((item) => !item.removed);
    if (cleaned.length === branches.length && !cleaned.some((item) => item.changed)) return;
    touched = true;
    if (!cleaned.length) {
      rule.walkDecls((decl) => {
        stats.declarations++;
        if (decl.important) stats.important++;
      });
      stats.rules++;
      rule.remove();
    } else {
      rule.selector = formatList(cleaned.map((item) => item.text), original);
    }
  });
  if (!touched) continue;
  const eol = input.includes("\r\n") ? "\r\n" : "\n";
  fs.writeFileSync(file, ast.toString().replace(/\r?\n/g, eol));
  stats.files++;
  stats.touched.push(file);
}

console.log(JSON.stringify(stats, null, 2));
