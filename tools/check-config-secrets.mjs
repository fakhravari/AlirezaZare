import {execFileSync} from 'node:child_process';
import {readFileSync} from 'node:fs';
import {findSecrets} from './config-secrets.mjs';

const trackedFiles = execFileSync('git', ['ls-files', '-z', '--', ':(glob,icase)**/appsettings*.json'], {encoding: 'utf8'})
  .split('\0')
  .filter(Boolean)
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
