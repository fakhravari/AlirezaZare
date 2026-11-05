import {execFileSync} from 'node:child_process';
import {readFileSync} from 'node:fs';

const secretKey = /(?:password|secret|token|api[-_]?key|connectionstrings?|private[-_]?key|client[-_]?secret)/i;
const placeholder = /^(?:\$\{[^}]+\}|CHANGE_ME|REPLACE_ME|YOUR_[A-Z0-9_]+)$/i;

function findSecrets(value, prefix = '') {
  const findings = [];
  if (!value || typeof value !== 'object') return findings;

  for (const [key, child] of Object.entries(value)) {
    const field = prefix ? `${prefix}:${key}` : key;
    if (secretKey.test(key) && typeof child === 'string' && child.trim() && !placeholder.test(child.trim())) {
      findings.push(field);
    } else if (child && typeof child === 'object') {
      findings.push(...findSecrets(child, field));
    }
  }
  return findings;
}

const trackedFiles = execFileSync('git', ['ls-files', '-z'], {encoding: 'utf8'})
  .split('\0')
  .filter(file => /(^|\/)appsettings(?:\.[^/]+)?\.json$/i.test(file));
const problems = [];

for (const file of trackedFiles) {
  let config;
  try {
    config = JSON.parse(readFileSync(file, 'utf8'));
  } catch {
    problems.push(`${file}: invalid JSON`);
    continue;
  }
  for (const key of findSecrets(config)) problems.push(`${file}: non-empty secret field ${key}`);
}

if (problems.length) {
  process.stderr.write(`Tracked application settings contain credentials or invalid JSON:\n${problems.join('\n')}\n`);
  process.exitCode = 1;
} else {
  process.stdout.write(`Configuration secret check OK (${trackedFiles.length} tracked appsettings file(s)).\n`);
}

export {findSecrets};
