import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const read = path => readFile(new URL(`../../${path}`, import.meta.url), "utf8");
const method = (source, name, next) => {
    const start = source.indexOf(`Task<IActionResult> ${name}(`);
    const end = source.indexOf(next, start);
    assert.ok(start >= 0 && end > start, `Missing method ${name}`);
    return source.slice(start, end);
};

test("product drafts stay manageable while publication uses one flag", async () => {
    const controller = await read("Admin/Areas/Store/Controllers/Api/StoreCatalogAdminApiController.cs");
    const drafts = method(controller, "ProductDrafts", '[HttpGet("products/create-options")]');
    const create = method(controller, "CreateProduct", '[HttpPost("products/manage/{id:long}/publish")]');
    const publish = method(controller, "PublishProduct", '[HttpPost("products/manage/{id:long}/unpublish")]');
    const unpublish = method(controller, "UnpublishProduct", "private static bool IsJson");
    assert.match(drafts, /where\s+!product\.IsStoreEnabled/);
    assert.match(create, /IsStoreEnabled\s*=\s*false/);
    assert.match(publish, /x\.Id\s*==\s*id\s*&&\s*!x\.IsStoreEnabled/);
    assert.match(publish, /product\.IsStoreEnabled\s*=\s*true/);
    assert.match(unpublish, /x\.Id\s*==\s*id\s*&&\s*x\.IsStoreEnabled/);
    assert.match(unpublish, /product\.IsStoreEnabled\s*=\s*false/);
    for (const name of ["ProductEditor", "ProductDetails", "UpdateProduct", "ProductBenefits", "CreateProductAccessory"]) {
        const start = controller.indexOf(`Task<IActionResult> ${name}(`);
        const end = controller.indexOf("\n    [Http", start);
        assert.ok(start >= 0 && end > start);
        assert.doesNotMatch(controller.slice(start, end), /x\.Id\s*==\s*id\s*&&\s*x\.IsStoreEnabled/, name);
    }
    for (const path of ["Admin/Areas/Store/Controllers/Api/StoreProductImageApiController.cs", "Admin/Areas/Store/Controllers/Api/StoreMediaUploadApiController.cs"]) {
        assert.doesNotMatch(await read(path), /x\.Id\s*==\s*productId\s*&&\s*x\.IsStoreEnabled/);
    }
});

test("migration preserves old public visibility and is transactional", async () => {
    const sql = await read("Shop/Features/Store/Database/Migrations/100-product-store-publication.sql");
    assert.match(sql, /BEGIN TRANSACTION/i);
    assert.match(sql, /\[IsStoreEnabled\]\s*=\s*CASE\s+WHEN\s+\[IsStoreEnabled\]\s*=\s*1\s+AND\s+\[IsCatalogActive\]\s*=\s*1\s+THEN\s+1\s+ELSE\s+0\s+END/i);
    assert.ok(sql.indexOf("UPDATE [Warehouse].[Product]") < sql.indexOf("DROP COLUMN [IsCatalogActive]"));
    assert.match(sql, /ROLLBACK TRANSACTION/i);
    assert.match(sql, /100-product-store-publication/);
});
