using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Controllers.Api;
using Shop.Data.Infrastructure.Persistence;

internal static class UnitControllerTest
{
    public static async Task RunAsync(DbContextOptions<ShopDbContext> options)
    {
        for (int iteration = 1; iteration <= 3; iteration++)
        {
            await using var db = new ShopDbContext(options);
            long warehouse = await db.Database.SqlQueryRaw<long>("SELECT IdWarehouse AS [Value] FROM Store.SiteSetting WHERE Id=1").SingleAsync();
            long plan = await (from owner in db.WarehouseStockOwners.AsNoTracking()
                join p in db.SalesPlanReferences.AsNoTracking() on owner.IdStockOwner equals p.Id
                where owner.IdWarehouse == warehouse && p.IsActive select p.Id).FirstAsync();
            long[] units = await db.WarehouseUnits.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).Take(3).ToArrayAsync();
            if (units.Length != 3) throw new InvalidOperationException("Three active units are required.");
            long receiptType = await db.WarehouseDocumentTypes.AsNoTracking().Where(x => x.IsActive && x.AffectsStock && x.Direction == "R" && !x.Code.Contains("return") && !x.Name.Contains("مرجوع")).Select(x => x.Id).FirstAsync();
            long issueType = await db.WarehouseDocumentTypes.AsNoTracking().Where(x => x.IsActive && x.AffectsStock && x.Direction == "I" && !x.Code.Contains("return") && !x.Name.Contains("مرجوع")).Select(x => x.Id).FirstAsync();
            long party = await db.WarehouseParties.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).FirstAsync();
            string key = $"concurrency-stock-{Guid.NewGuid():N}";
            long product = long.MaxValue - Random.Shared.NextInt64(1, 1000000000), item = 0, receipt = 0, issue = 0;
            try
            {
                await using (var tx = await db.Database.BeginTransactionAsync())
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Warehouse.Product (Id,IdBaseUnit,Name,ProductKey,Code,IsWarehouseEnabled,IsStoreEnabled,IsCatalogActive) VALUES ({product},{units[0]},{key},{key},{key},1,0,0)");
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Warehouse.ProductUnit (IdProduct,IdUnit,ConversionFactor,IsBaseUnit,IsActive) VALUES ({product},{units[0]},1,1,1),({product},{units[1]},6,0,1),({product},{units[2]},2,0,1)");
                    item = (await db.Database.SqlQuery<long>($"INSERT Warehouse.InventoryItem (IdProduct,IsActive) OUTPUT INSERTED.Id AS [Value] VALUES ({product},1)").ToListAsync()).Single();
                    await tx.CommitAsync();
                }
                var input = new SaveWarehouseDocumentInput(warehouse, receiptType, party, plan, DateOnly.FromDateTime(DateTime.Today), "D", key, null,
                    [new WarehouseDocumentLineInput(item, units[1], 2.1234m, plan)]);
                await using (var context = new ShopDbContext(options))
                {
                    var result = await Controller(context).Create(input, CancellationToken.None) as OkObjectResult ?? throw new InvalidOperationException("Draft receipt failed.");
                    receipt = JsonSerializer.SerializeToElement(result.Value).GetProperty("Id").GetInt64();
                }
                await CheckAsync(options, warehouse, plan, item, receipt, 0, 2.123m, 6m, 12.738m);
                await using (var context = new ShopDbContext(options))
                    if (await Controller(context).Update(receipt, input with { Status = "F" }, CancellationToken.None) is not OkObjectResult) throw new InvalidOperationException("Final receipt failed.");
                await CheckAsync(options, warehouse, plan, item, receipt, 12.738m, 2.123m, 6m, 12.738m);
                var issueInput = input with { DocumentTypeId = issueType, Status = "F", Lines = [new WarehouseDocumentLineInput(item, units[2], 1.2344m, plan)] };
                await using (var context = new ShopDbContext(options))
                {
                    var result = await Controller(context).Create(issueInput, CancellationToken.None) as OkObjectResult ?? throw new InvalidOperationException("Issue failed.");
                    issue = JsonSerializer.SerializeToElement(result.Value).GetProperty("Id").GetInt64();
                }
                await CheckAsync(options, warehouse, plan, item, issue, 10.270m, 1.234m, 2m, 2.468m);
                await using (var context = new ShopDbContext(options))
                    if (await Controller(context).Create(issueInput with { Lines = [new WarehouseDocumentLineInput(item, units[1], 3m, plan)] }, CancellationToken.None) is not ConflictObjectResult) throw new InvalidOperationException("Converted shortage was not rejected.");
                await CheckAsync(options, warehouse, plan, item, issue, 10.270m, 1.234m, 2m, 2.468m);
                await using (var context = new ShopDbContext(options))
                    if (await Controller(context).Update(issue, issueInput with { Status = "D" }, CancellationToken.None) is not OkObjectResult) throw new InvalidOperationException("Returning issue to draft failed.");
                await CheckAsync(options, warehouse, plan, item, issue, 12.738m, 1.234m, 2m, 2.468m);
                Console.WriteLine($"Unit controller iteration {iteration}/3 OK: conversions, draft, final, shortage and rollback.");
            }
            finally
            {
                foreach (long document in new[] { issue, receipt }.Where(x => x != 0))
                {
                    await using var context = new ShopDbContext(options);
                    if (await Controller(context).Delete(document, CancellationToken.None) is not OkObjectResult) throw new InvalidOperationException($"Could not delete test document {document}.");
                }
                await using var tx = await db.Database.BeginTransactionAsync();
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.StockBalance WHERE IdInventoryItem={item} AND Quantity=0");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.InventoryItem WHERE Id={item} AND IdProduct={product}");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.ProductUnit WHERE IdProduct={product}");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.Product WHERE Id={product} AND ProductKey={key}");
                await tx.CommitAsync();
            }
        }
    }

    private static async Task CheckAsync(DbContextOptions<ShopDbContext> options, long warehouse, long plan, long item, long document, decimal balance, decimal quantity, decimal factor, decimal baseQuantity)
    {
        await using var db = new ShopDbContext(options);
        decimal actual = await db.StockBalances.AsNoTracking().Where(x => x.IdWarehouse == warehouse && x.IdInventoryItem == item && x.IdSalesPlanReference == plan).Select(x => (decimal?)x.Quantity).SingleOrDefaultAsync() ?? 0;
        var line = await db.WarehouseDocumentLines.AsNoTracking().SingleAsync(x => x.IdDocument == document);
        if (actual != balance || line.Quantity != quantity || line.ConversionFactor != factor || line.BaseQuantity != baseQuantity)
            throw new InvalidOperationException($"Unexpected balance or conversion: {actual}, {line.Quantity}, {line.ConversionFactor}, {line.BaseQuantity}.");
    }

    private static WarehouseDocumentsApiController Controller(ShopDbContext db) => new(db) { ControllerContext = new ControllerContext
    { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test")) } } };
}
