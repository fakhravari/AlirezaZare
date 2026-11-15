import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import path from "node:path";
import test from "node:test";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

async function source(relativePath) {
    return readFile(path.join(root, relativePath), "utf8");
}

test("store report routes and role permissions share one inherited tree", async () => {
    const migration = await source("Shop/Features/Store/Database/Migrations/091-store-reports-permissions.sql");
    const controller = await source("Admin/Controllers/Api/StoreReportsAdminApiController.cs");

    assert.match(migration, /store\.features\.reports[\s\S]*?store\.manager/);
    assert.match(migration, /store\.reports\.read[\s\S]*?StoreReportsAdminApi[\s\S]*?GetReport/);
    assert.match(migration, /store\.reports\.export[\s\S]*?StoreReportsAdminApi[\s\S]*?Export/);
    assert.match(migration, /system\.full-access-store[\s\S]*?store\.manager/);
    assert.match(controller, /\[Route\("api\/admin\/store\/reports"\)\]/);
    assert.match(controller, /\[HttpGet\("export"\)\][\s\S]*?Task<IActionResult> Export/);
    assert.match(controller, /const int exportPageSize = 1000/);
    assert.match(controller, /while \(\(long\)\(page - 1\) \* exportPageSize < total\)/);
});

test("store reports cover four views and restrict inventory to allowed warehouses", async () => {
    const controller = await source("Admin/Controllers/Api/StoreReportsAdminApiController.cs");
    const view = await source("Admin/Areas/Store/Views/Features/Reports.cshtml");
    const client = await source("Admin/wwwroot/js/admin-store-reports.js");

    for (const reportType of ["sales", "products", "inventory", "customers"]) {
        assert.match(controller, new RegExp(`"${reportType}"`));
        assert.match(view, new RegExp(`data-report-tab="${reportType}"`));
        assert.match(client, new RegExp(`${reportType}: \\{`));
    }
    assert.match(controller, /AllowedWarehouseIds\(\)/);
    assert.match(controller, /fullAccess \|\| allowed\.Contains\(x\.IdWarehouse\)/);
    assert.match(controller, /fullAccess \|\| allowed\.Contains\(movement\.IdWarehouse\)/);
    assert.match(controller, /SalesStatuses\.Contains\(order\.Status\)/);
    assert.match(controller, /payment\.Status == "verified"/);
});
