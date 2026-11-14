import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import path from "node:path";
import test from "node:test";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

async function source(relativePath) {
    return readFile(path.join(root, relativePath), "utf8");
}

function methodBody(text, signature) {
    const signatureIndex = text.indexOf(signature);
    assert.notEqual(signatureIndex, -1, `Missing method: ${signature}`);

    const openingBrace = text.indexOf("{", signatureIndex);
    assert.notEqual(openingBrace, -1, `Missing method body: ${signature}`);

    let depth = 0;
    for (let index = openingBrace; index < text.length; index++) {
        if (text[index] === "{") depth++;
        if (text[index] === "}") depth--;
        if (depth === 0) return text.slice(openingBrace, index + 1);
    }

    assert.fail(`Unclosed method body: ${signature}`);
}

function assertDatabasePaging(body, label, responsePattern = /items\s*,\s*total\s*,\s*page\s*,\s*pageSize/) {
    const countIndex = body.indexOf("CountAsync(");
    const skipIndex = body.indexOf(".Skip(");
    const takeIndex = body.indexOf(".Take(pageSize)");
    const materializeIndex = body.indexOf("ToListAsync(");

    assert.ok(countIndex >= 0, `${label} should calculate the filtered total in the database`);
    assert.ok(skipIndex > countIndex, `${label} should apply Skip after counting`);
    assert.ok(takeIndex > skipIndex, `${label} should apply Take after Skip`);
    assert.ok(materializeIndex > takeIndex, `${label} should materialize only the selected page`);
    assert.match(body, responsePattern,
        `${label} should preserve the paged response contract`);
}

test("catalog product and draft lists page in the database", async () => {
    const controller = await source("Admin/Controllers/Api/StoreCatalogAdminApiController.cs");
    const draftMethod = methodBody(controller, "Task<IActionResult> ProductDrafts(");
    const sectionMethod = methodBody(controller, "Task<IActionResult> GetSection(");

    assert.match(draftMethod, /pageSize is < 1 or > 100/);
    assert.match(draftMethod, /q\?\.Length \?\? 0\) > 100/);
    assertDatabasePaging(draftMethod, "Product drafts");

    assert.match(sectionMethod, /pageSize is < 1 or > 200/);
    assert.match(sectionMethod, /productSource\.CountAsync\(/);
    assert.match(sectionMethod, /productSource[\s\S]*?\.Skip\([\s\S]*?\.Take\(pageSize\)[\s\S]*?ToListAsync\(/);
    assert.match(sectionMethod, /items = productItems, total = productTotal, page, pageSize/);
});

test("variant prices and product offers page before materializing rows", async () => {
    const controller = await source("Admin/Controllers/Api/StoreVariantPricesAdminApiController.cs");
    const prices = methodBody(controller, "Task<IActionResult> List(");
    const offers = methodBody(controller, "Task<IActionResult> ProductOffers(");

    for (const [label, body] of [["Variant prices", prices], ["Product offers", offers]]) {
        assert.match(body, /ValidPage\(page, pageSize, q\)/, `${label} should validate paging input`);
        assertDatabasePaging(body, label);
    }
});

test("paid-order dispatch uses bounded pages and stable page ordering", async () => {
    const controller = await source("Admin/Controllers/Api/StorePaidOrdersDispatchApiController.cs");
    const list = methodBody(controller, "Task<IActionResult> List(");

    assert.match(list, /pageSize is < 1 or > 100/);
    assert.match(list, /\.OrderBy\(x => x\.PaidAtUtc\)\.ThenBy\(x => x\.order\.Id\)/);
    assertDatabasePaging(list, "Paid-order dispatch", /items\s*=\s*rows\s*,\s*total\s*,\s*page\s*,\s*pageSize/);
});

test("admin list clients send page parameters and consume paged response items", async () => {
    const adminData = await source("Admin/wwwroot/js/admin-data.js");
    const priceClient = await source("Admin/wwwroot/js/admin-variant-prices.js");
    const dispatchClient = await source("Admin/wwwroot/js/admin-paid-orders-dispatch.js");

    const draftLoader = methodBody(adminData, "async function loadProductDrafts(");
    assert.match(draftLoader, /searchParams\.set\("page"/);
    assert.match(draftLoader, /searchParams\.set\("pageSize"/);
    assert.match(draftLoader, /result\.items/);

    assert.match(priceClient, /searchParams\.set\("page"/);
    assert.match(priceClient, /searchParams\.set\("pageSize"/);
    assert.match(priceClient, /page\._variantPriceRows = result\.items/);
    assert.match(priceClient, /page\._offers = result\.items/);

    assert.match(dispatchClient, /searchParams\.set\("page"/);
    assert.match(dispatchClient, /searchParams\.set\("pageSize"/);
    assert.match(dispatchClient, /result\.items\.length/);
});

test("dispatch selection survives page loads and stays within the 200-order limit", async () => {
    const client = await source("Admin/wwwroot/js/admin-paid-orders-dispatch.js");

    assert.match(client, /const pageSize = 50, selectedIds = new Set\(\)/);
    assert.match(client, /checkbox\.checked = selectedIds\.has\(item\.id\)/);
    assert.match(client, /selectedIds\.size > 200/);
    assert.match(client, /selectedIds\.clear\(\); await load\(1\)/);
});
