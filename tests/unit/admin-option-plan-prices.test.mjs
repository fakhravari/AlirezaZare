import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';

test('management prices variants by plan without legacy purchase options', async () => {
  const controller = await readFile('Admin/Areas/Store/Controllers/Api/StoreVariantPricesAdminApiController.cs', 'utf8');
  const script = await readFile('Admin/wwwroot/js/admin-variant-prices.js', 'utf8');
  const view = await readFile('Admin/Areas/Store/Views/Features/VariantPrices.cshtml', 'utf8');
  assert.match(controller, /IdProductVariant == x\.variant\.Id && v\.IdSalesPlan == x\.plan\.Id/);
  assert.match(controller, /VariantPlanPriceInput\(decimal PriceAmount, decimal DiscountPercent, bool IsEnabled\)/);
  assert.doesNotMatch(controller, /IdOptionValue|option-prices/);
  assert.doesNotMatch(script, /data-option-prices|\/option-prices/);
  assert.doesNotMatch(view, /data-option-prices|option-prices-modal/);
});
