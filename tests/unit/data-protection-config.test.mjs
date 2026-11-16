import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

test('Shop and Admin keep separate data protection keys under App_Data', async () => {
  for (const app of ['Shop', 'Admin']) {
    const program = await readFile(`${app}/Program.cs`, 'utf8');
    const project = await readFile(`${app}/${app}.csproj`, 'utf8');
    const profile = await readFile(`${app}/Properties/PublishProfiles/FolderProfile.pubxml`, 'utf8');
    assert.ok(program.includes('AddPersistentDataProtection(builder.Configuration, builder.Environment.ContentRootPath, builder.Environment.EnvironmentName)'));
    assert.match(project, /<Content Remove="keys\/\*\*\/\*"\s*\/>/);
    assert.match(project, /<None Remove="keys\/\*\*\/\*"\s*\/>/);
    assert.match(project, /<Content Remove="App_Data\/keys\/\*\*\/\*"\s*\/>/);
    assert.match(project, /<None Remove="App_Data\/keys\/\*\*\/\*"\s*\/>/);
    assert.match(profile, /<DeleteExistingFiles>false<\/DeleteExistingFiles>/);
  }
  const protection = await readFile('Data/Infrastructure/DataProtectionConfiguration.cs', 'utf8');
  assert.match(protection, /Path\.Combine\(contentRoot, "App_Data", "keys"\)/);
  assert.match(protection, /SetApplicationName\(\$"my-app-\{environment\}"\)/);
  const gitignore = await readFile('.gitignore', 'utf8');
  assert.match(gitignore, /^\/Admin\/keys\/$/m);
  assert.match(gitignore, /^\/Shop\/keys\/$/m);
  assert.doesNotMatch(gitignore, /^\/Admin\/App_Data\/keys\/$/m);
  assert.doesNotMatch(gitignore, /^\/Shop\/App_Data\/keys\/$/m);
});
