import { readdir, readFile, stat } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

// Run from any directory: node docs/development/check-docs.mjs
const docsRoot = fileURLToPath(new URL("../", import.meta.url));
const repoRoot = path.dirname(docsRoot.replace(/[\\/]$/, ""));
const rootFiles = new Set(["README.md", "AGENTS.md"]);
const kebabCase = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
const errors = [];
const markdownFiles = [];
const sections = [];
const directoryCache = new Map();
const display = (file) => path.relative(repoRoot, file).split(path.sep).join("/");

async function entries(directory) {
  if (!directoryCache.has(directory)) {
    directoryCache.set(directory, await readdir(directory, { withFileTypes: true }));
  }
  return directoryCache.get(directory);
}

async function scan(directory) {
  const children = await entries(directory);
  if (children.some((item) => item.isFile() && item.name.endsWith(".md")) &&
      !children.some((item) => item.isFile() && item.name === "README.md")) {
    errors.push(display(directory) + ": missing section README.md");
  }
  for (const item of children) {
    const file = path.join(directory, item.name);
    if (item.isDirectory()) {
      if (!kebabCase.test(item.name)) errors.push(display(file) + ": use lowercase-kebab-case");
      if (directory === docsRoot) sections.push(file);
      await scan(file);
    } else if (item.isFile()) {
      if (directory === docsRoot && !rootFiles.has(item.name)) {
        errors.push(display(file) + ": move the guide/support file into its owning section");
      }
      if (item.name.endsWith(".md")) {
        if (!rootFiles.has(item.name) && !kebabCase.test(item.name.slice(0, -3))) {
          errors.push(display(file) + ": use lowercase-kebab-case.md");
        }
        markdownFiles.push(file);
      }
    }
  }
}

// Examples in fenced code blocks are not live navigation links.
function prose(source) {
  let fence = null;
  return source.split(/\r?\n/).map((line) => {
    const match = line.match(/^ {0,3}(\x60{3,}|~{3,})/);
    if (match) {
      if (!fence) fence = match[1];
      else if (match[1][0] === fence[0] && match[1].length >= fence.length) fence = null;
      return "";
    }
    return fence ? "" : line;
  }).join("\n");
}

async function exactPath(target) {
  let current = repoRoot;
  for (const part of path.relative(repoRoot, target).split(path.sep).filter(Boolean)) {
    const children = await entries(current);
    const entry = children.find((item) => item.name === part);
    if (!entry) return false;
    current = path.join(current, entry.name);
  }
  return true;
}

await scan(docsRoot);
for (const section of sections) {
  if (!(await entries(section)).some((item) => item.isFile() && item.name === "README.md")) {
    errors.push(display(section) + ": missing section README.md");
  }
}

const graph = new Map();
let linkCount = 0;
for (const file of markdownFiles) {
  const source = prose(await readFile(file, "utf8"));
  const edges = new Set();
  graph.set(file, edges);
  for (const match of source.matchAll(/!?\[[^\]]*\]\((<[^>]+>|[^\s)]+)(?:\s+"[^"]*")?\)/g)) {
    const href = match[1].replace(/^<|>$/g, "");
    if (/^(?:[a-z][a-z0-9+.-]*:|\/\/|#)/i.test(href)) continue;
    const rawPath = href.split(/[?#]/)[0];
    if (!rawPath) continue;
    linkCount++;
    try {
      const target = path.resolve(path.dirname(file), decodeURIComponent(rawPath));
      const relative = path.relative(repoRoot, target);
      if (relative === ".." || relative.startsWith(".." + path.sep) || path.isAbsolute(relative)) {
        throw new Error("link leaves the repository");
      }
      await stat(target);
      if (!(await exactPath(target))) throw new Error("path casing differs from the file on disk");
      if (target.endsWith(".md")) edges.add(target);
    } catch (error) {
      errors.push(display(file) + ": " + href + " — " + error.message);
    }
  }
}

const reachable = new Set();
const pending = [path.join(docsRoot, "README.md")];
while (pending.length) {
  const file = pending.pop();
  if (reachable.has(file)) continue;
  reachable.add(file);
  for (const target of graph.get(file) || []) pending.push(target);
}
for (const file of markdownFiles) {
  if (!reachable.has(file)) errors.push(display(file) + ": not reachable from docs/README.md");
}

if (errors.length) {
  console.error(errors.join("\n"));
  process.exitCode = 1;
} else {
  console.log("Docs OK: " + markdownFiles.length + " Markdown files, " + sections.length +
    " sections, " + linkCount + " local links; placement, casing and navigation verified.");
}
