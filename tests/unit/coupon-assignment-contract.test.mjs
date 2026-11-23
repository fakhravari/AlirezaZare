import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

test("personal coupons are restricted during quote, checkout and account projection", async () => {
  const account = await readFile("Shop/Controllers/AccountController.cs", "utf8");
  const quote = account.slice(account.indexOf('    [HttpPost("coupon/quote")]'), account.indexOf('    [HttpPost("checkout")]'));
  const checkout = account.slice(account.indexOf('    [HttpPost("checkout")]'));
  assert.match(quote, /coupon\.IdProfile\.HasValue\s*&&\s*coupon\.IdProfile\s*!=\s*ProfileId/);
  assert.match(checkout, /appliedCoupon\.IdProfile\.HasValue\s*&&\s*appliedCoupon\.IdProfile\s*!=\s*ProfileId/);
  const repository = await readFile("Data/Infrastructure/Persistence/Repositories/StoreRuntimeDatasetRepository.cs", "utf8");
  assert.match(repository, /AccountCoupons\.AsNoTracking\(\)\.Where\(x => x\.IdProfile == null\)/);
  const profile = await readFile("Shop/Controllers/ProfileController.cs", "utf8");
  assert.match(profile, /coupon\.IdProfile == profileId/);
  assert.match(profile, /User\.FindFirstValue\(ClaimTypes\.NameIdentifier\)/);
});

test("coupon assignment uses a verified profile and preserves existing public coupons", async () => {
  const controller = await readFile("Admin/Areas/Store/Controllers/CouponsController.cs", "utf8");
  assert.match(controller, /audience is not \("public" or "personal"\)/);
  assert.match(controller, /db\.AccountProfiles\.Where\(profile => profile\.Email == email\)/);
  assert.match(controller, /if \(profileId is null\)/);
  const migration = await readFile("Shop/Features/Store/Database/Migrations/167-coupon-profile-assignment.sql", "utf8");
  assert.match(migration, /ADD IdProfile bigint NULL/);
  assert.match(migration, /FOREIGN KEY \(IdProfile\) REFERENCES Account\.Profile\(Id\)/);
});
