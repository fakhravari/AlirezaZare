using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Controllers.Api;
using Shop.Database.Infrastructure.Persistence;

internal static class SalesPlansMainVerification
{
    public static async Task Run(string connectionString)
    {
        var connection = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(connection.InitialCatalog, "bsshopir_fakhravari", StringComparison.OrdinalIgnoreCase) ||
            !connection.DataSource.Contains("31.25.91.7\\sqlserver2022", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Sales-plan verification is restricted to the configured primary SQL database.");

        var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connectionString).Options;
        await using var db = new ShopDbContext(options);
        var controller = new StoreVariantPricesAdminApiController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "sales-plan-verification"))
                }
            }
        };

        string key = "verify-plan-" + Guid.NewGuid().ToString("N");
        string code = "VERIFY-" + Guid.NewGuid().ToString("N")[..16];
        long id = 0;
        try
        {
            var create = new StoreVariantPricesAdminApiController.PlanInput(key, code, "طرح کنترل", "verification", null, null, null, false);
            EnsureOk(await controller.CreatePlan(create, default), "Create draft");
            id = await db.SalesPlanReferences.AsNoTracking().Where(x => x.StoreSalesPlanKey == key).Select(x => x.Id).SingleAsync();
            long mappedWarehouses = await db.Database.SqlQuery<long>($"SELECT COUNT_BIG(*) AS [Value] FROM [Warehouse].[WarehouseStockOwner] WHERE [IdStockOwner]={id}").SingleAsync();
            if (mappedWarehouses == 0) throw new InvalidOperationException("Created plan was not connected to sales warehouses.");

            EnsureOk(await controller.UpdatePlan(id, create with { Code = code + "-E", DisplayName = "طرح ویرایش‌شده" }, default), "Edit metadata");
            EnsureOk(await controller.SavePlan(id, new(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(3), "آزمایشی", true), default), "Edit schedule");
            EnsureOk(await controller.SetPlanPublication(id, new(true), default), "Publish");
            bool active = await db.Database.SqlQuery<bool>($"SELECT [IsActive] AS [Value] FROM [Warehouse].[SalesPlanReference] WHERE [Id]={id}").SingleAsync();
            if (!active) throw new InvalidOperationException("Published plan remained inactive.");
            EnsureOk(await controller.SetPlanPublication(id, new(false), default), "Unpublish");
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([], "unauthorized"));
            if (await controller.DeletePlan(id, default) is not ForbidResult) throw new InvalidOperationException("Unauthorized deletion was accepted.");
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "sales-plan-verification"));
            EnsureOk(await controller.DeletePlan(id, default), "Delete unused plan");
            id = 0;
            Console.WriteLine("Primary sales-plan create, metadata edit, schedule, publish/unpublish, warehouse mapping, authorization and safe delete checks passed.");
        }
        finally
        {
            if (id != 0)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Warehouse].[WarehouseStockOwner] WHERE [IdStockOwner]={id} AND [IdWarehouse] IN (SELECT [Id] FROM [Warehouse].[Warehouse] WHERE [IsSalesWarehouse]=1)");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Warehouse].[SalesPlanReference] WHERE [Id]={id} AND [IdStockOwnerType]=1 AND [StoreSalesPlanKey]={key}");
            }
        }
    }

    private static void EnsureOk(IActionResult result, string operation)
    {
        if (result is not OkObjectResult) throw new InvalidOperationException($"{operation} failed: {result.GetType().Name}");
    }
}
