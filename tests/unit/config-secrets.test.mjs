import test from 'node:test';
import assert from 'node:assert/strict';
import {findSecrets, isAllowedSecret} from '../../tools/config-secrets.mjs';

test('configuration secret detection catches nested credentials without flagging blanks or env placeholders', () => {
  const config = {
    ConnectionStrings: {Primary: 'Server=db;Password=sample'},
    Email: {Smtp: {Password: 'sample-secret', Username: 'shop@example.test'}},
    Jwt: {SigningKey: 'sample-signing-key', Issuer: 'shop'},
    Storage: {AccessKey: '${STORAGE_ACCESS_KEY}'},
    Legacy: {Password: '   '},
  };

  assert.deepEqual(findSecrets(config), [
    'ConnectionStrings:Primary',
    'Email:Smtp:Password',
    'Jwt:SigningKey',
  ]);
});

test('only the explicitly approved default connection fields are allowed', () => {
  assert.equal(isAllowedSecret('Shop/appsettings.json', 'ConnectionStrings:DefaultConnection'), true);
  assert.equal(isAllowedSecret('Admin/appsettings.json', 'ConnectionStrings:DefaultConnection'), true);
  assert.equal(isAllowedSecret('Shop/appsettings.json', 'ConnectionStrings:Other'), false);
  assert.equal(isAllowedSecret('Shop/appsettings.Development.json', 'ConnectionStrings:DefaultConnection'), false);
  assert.equal(isAllowedSecret('Admin/appsettings.json', 'Email:Smtp:Password'), false);
});
