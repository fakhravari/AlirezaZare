using System.Text.Json;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;
using Shop.Database.Infrastructure.Persistence.Repositories;

internal static class StockOverflowTest
{
    public static async Task RunAsync(DbContextOptions<ShopDbContext> options)
    {
        await using var db = new ShopDbContext(options);
        var candidate = (await db.Database.SqlQueryRaw<Candidate>("""
            SELECT TOP (1) b.Id AS BalanceId, CONVERT(int,p.Id) AS ProductId, sp.SalesPlanKey AS PlanKey
            FROM Warehouse.StockBalance b
            JOIN Store.SiteSetting setting ON setting.IdWarehouse=b.IdWarehouse
            JOIN Warehouse.InventoryItem item ON item.Id=b.IdInventoryItem AND item.IsActive=1 AND item.IdProductVariant IS NULL
            JOIN Warehouse.Product p ON p.Id=item.IdProduct AND p.IsCatalogActive=1 AND p.IsStoreEnabled=1
            JOIN Warehouse.SalesPlanReference sp ON sp.Id=b.IdSalesPlanReference AND sp.IsDefault=1
            WHERE NOT EXISTS (SELECT 1 FROM Warehouse.ProductVariant v WHERE v.IdProduct=p.Id AND v.IsActive=1)
            ORDER BY b.Id
            """).ToListAsync()).First();
        for (int iteration = 1; iteration <= 3; iteration++)
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                var plans = new StoreSalesPlanRepository(db);
                var catalog = new StoreRuntimeDatasetRepository(db);
                decimal? expectedPrice = null;
                foreach (var (quantity, expectedStock) in new[] { (0.999m, 0), (1.999m, 1), (2147483648m, int.MaxValue) })
                {
                    var quantityParameter = new SqlParameter("@quantity", SqlDbType.Decimal) { Precision = 18, Scale = 3, Value = quantity };
                    await db.Database.ExecuteSqlRawAsync("UPDATE Warehouse.StockBalance SET Quantity=@quantity WHERE Id=@id",
                        quantityParameter, new SqlParameter("@id", candidate.BalanceId));
                    var snapshot = await plans.ReadSalesPlanSnapshotAsync(CancellationToken.None);
                    var offer = snapshot.Offers.Single(x => x.ProductId == candidate.ProductId && x.SalesPlanId == candidate.PlanKey);
                    if (offer.StockQuantity != expectedStock) throw new InvalidOperationException($"Offer stock {offer.StockQuantity}, expected {expectedStock}.");
                    expectedPrice ??= offer.PriceAmount;
                    if (offer.PriceAmount != expectedPrice) throw new InvalidOperationException("Changing stock changed the offer price.");
                    using var json = JsonDocument.Parse(await catalog.ReadCatalogJsonAsync(CancellationToken.None) ?? throw new InvalidOperationException("Catalog is empty."));
                    var product = json.RootElement.GetProperty("products").EnumerateArray().Single(x => x.GetProperty("id").GetInt32() == candidate.ProductId);
                    if (quantity > int.MaxValue && product.GetProperty("stockQuantity").GetInt32() != int.MaxValue)
                        throw new InvalidOperationException("Catalog stock did not saturate at Int32 maximum.");
                }
            }
            finally { await tx.RollbackAsync(); }
            Console.WriteLine($"Stock overflow iteration {iteration}/3 OK: fractional floor, capped stock, unchanged offer price, rollback.");
        }
    }

    public sealed class Candidate
    {
        public long BalanceId { get; set; }
        public int ProductId { get; set; }
        public string PlanKey { get; set; } = "";
    }
}
