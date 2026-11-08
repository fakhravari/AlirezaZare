using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Application.Store.Contracts;
using Shop.Database.Application.Store.Models;

namespace Shop.Database.Infrastructure.Persistence.Repositories;

public sealed class StoreSalesPlanRepository(ShopDbContext dbContext) : IStoreSalesPlanRepository
{
    public async Task<StoreSalesPlanSnapshot> ReadSalesPlanSnapshotAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        bool closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);

        try
        {
            List<StoreSalesPlanRow> plans = [];
            await using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT [SalesPlanKey], [Code], [Name], [PlanType], [IsDefault], [IsEnabled],
                           [StartsAtUtc], [EndsAtUtc], [Label], [ShowCountdown]
                    FROM [Store].[SalesPlan]
                    ORDER BY [IsDefault] DESC, [Code];
                    """;
                await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    plans.Add(new(
                        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                        reader.GetBoolean(4), reader.GetBoolean(5),
                        reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
                        reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
                        reader.IsDBNull(8) ? null : reader.GetString(8), reader.GetBoolean(9)));
            }

            Dictionary<(int ProductId, string PlanId), OfferBuilder> offers = [];
            await using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = """
                    WITH [SiteStock] AS
                    (
                        SELECT wp.[IdStoreProduct] AS [IdProduct], spr.[IdStoreSalesPlan] AS [IdSalesPlan],
                               SUM(sb.[Quantity]) AS [Quantity]
                        FROM [Warehouse].[SiteWarehouseSetting] setting
                        INNER JOIN [Warehouse].[StockBalance] sb ON sb.[IdWarehouse] = setting.[IdWarehouse]
                        INNER JOIN [Warehouse].[InventoryItem] ii ON ii.[Id] = sb.[IdInventoryItem]
                        INNER JOIN [Warehouse].[Product] wp ON wp.[Id] = ii.[IdProduct]
                        INNER JOIN [Warehouse].[SalesPlanReference] spr ON spr.[Id] = sb.[IdSalesPlanReference]
                        GROUP BY wp.[IdStoreProduct], spr.[IdStoreSalesPlan]
                    ), [OfferRows] AS
                    (
                        SELECT p.[Id] AS [IdProduct], sp.[Id] AS [IdSalesPlan], sp.[SalesPlanKey],
                               CONVERT(DECIMAL(19,0), ROUND(p.[PriceAmount] * 100.0 /
                                   (100.0 - ISNULL(p.[DiscountPercent], 0)), 0)) AS [PriceAmount],
                               ISNULL(p.[DiscountPercent], 0) AS [DiscountPercent],
                               p.[MaxPurchaseQuantity], p.[IsActive] AS [IsEnabled]
                        FROM [Store].[Product] p
                        CROSS JOIN [Store].[SalesPlan] sp
                        WHERE sp.[IsDefault] = 1
                        UNION ALL
                        SELECT p.[Id], sp.[Id], sp.[SalesPlanKey], o.[PriceAmount], o.[DiscountPercent],
                               o.[MaxPurchaseQuantity], CONVERT(BIT, CASE WHEN o.[IsEnabled] = 1 AND p.[IsActive] = 1 AND sp.[IsEnabled] = 1 THEN 1 ELSE 0 END)
                        FROM [Store].[ProductSalesPlanOffer] o
                        INNER JOIN [Store].[Product] p ON p.[Id] = o.[IdProduct]
                        INNER JOIN [Store].[SalesPlan] sp ON sp.[Id] = o.[IdSalesPlan]
                    )
                    SELECT o.[IdProduct], o.[SalesPlanKey], o.[PriceAmount], o.[DiscountPercent],
                           CONVERT(INT, ISNULL(s.[Quantity], 0)), o.[MaxPurchaseQuantity],
                           CONVERT(BIT, CASE WHEN o.[IsEnabled] = 1 AND sp.[IsEnabled] = 1 THEN 1 ELSE 0 END)
                    FROM [OfferRows] o
                    INNER JOIN [Store].[SalesPlan] sp ON sp.[Id] = o.[IdSalesPlan]
                    LEFT JOIN [SiteStock] s ON s.[IdProduct] = o.[IdProduct] AND s.[IdSalesPlan] = o.[IdSalesPlan]
                    ORDER BY o.[IdProduct], o.[SalesPlanKey];
                    """;
                await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    int productId = checked((int)reader.GetInt64(0));
                    string planId = reader.GetString(1);
                    offers.Add((productId, planId), new(
                        productId, planId, reader.GetDecimal(2), reader.GetDecimal(3),
                        reader.GetInt32(4), reader.GetInt32(5), reader.GetBoolean(6)));
                }
            }

            await using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = """
                    WITH [OfferPairs] AS
                    (
                        SELECT p.[Id] AS [IdProduct], sp.[Id] AS [IdSalesPlan], sp.[SalesPlanKey], CONVERT(BIGINT, NULL) AS [IdOffer]
                        FROM [Store].[Product] p CROSS JOIN [Store].[SalesPlan] sp WHERE sp.[IsDefault] = 1
                        UNION ALL
                        SELECT o.[IdProduct], o.[IdSalesPlan], sp.[SalesPlanKey], o.[Id]
                        FROM [Store].[ProductSalesPlanOffer] o
                        INNER JOIN [Store].[SalesPlan] sp ON sp.[Id] = o.[IdSalesPlan]
                    )
                    SELECT pair.[IdProduct], pair.[SalesPlanKey], a.[AccessoryKey],
                           CASE WHEN pair.[IdOffer] IS NULL THEN pa.[PriceAmount] ELSE oa.[PriceAmount] END AS [PriceAmount]
                    FROM [OfferPairs] pair
                    INNER JOIN [Store].[ProductAccessory] pa ON pa.[IdProduct] = pair.[IdProduct]
                    INNER JOIN [Store].[Accessory] a ON a.[Id] = pa.[IdAccessory] AND a.[IsActive] = 1
                    LEFT JOIN [Store].[ProductSalesPlanOfferAccessory] oa ON oa.[IdProductSalesPlanOffer] = pair.[IdOffer] AND oa.[IdAccessory] = a.[Id]
                    WHERE (pair.[IdOffer] IS NULL OR oa.[IsAllowed] = 1)
                      AND CASE WHEN pair.[IdOffer] IS NULL THEN pa.[PriceAmount] ELSE oa.[PriceAmount] END IS NOT NULL
                    ORDER BY pair.[IdProduct], pair.[SalesPlanKey], pa.[SortOrder];
                    """;
                await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    int productId = checked((int)reader.GetInt64(0));
                    string planId = reader.GetString(1);
                    string accessoryKey = reader.GetString(2);
                    OfferBuilder offer = offers[(productId, planId)];
                    offer.AccessoryIds.Add(accessoryKey);
                    offer.AccessoryPriceAmounts[accessoryKey] = reader.GetDecimal(3);
                }
            }

            await using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT p.[Id], sp.[SalesPlanKey], ov.[SpecificationKey], ov.[Value]
                    FROM [Store].[ProductSalesPlanOfferSpecificationOverride] ov
                    INNER JOIN [Store].[ProductSalesPlanOffer] o ON o.[Id] = ov.[IdProductSalesPlanOffer]
                    INNER JOIN [Store].[Product] p ON p.[Id] = o.[IdProduct]
                    INNER JOIN [Store].[SalesPlan] sp ON sp.[Id] = o.[IdSalesPlan]
                    ORDER BY p.[Id], sp.[SalesPlanKey], ov.[SpecificationKey];
                    """;
                await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    offers[(checked((int)reader.GetInt64(0)), reader.GetString(1))]
                        .SpecificationOverrides.Add(reader.GetString(2), reader.GetString(3));
            }

            return new(plans, offers.Values.Select(offer => offer.Build()).ToArray());
        }
        finally
        {
            if (closeWhenDone) await connection.CloseAsync();
        }
    }

    public async Task<ProductPurchaseConfiguration?> ReadProductPurchaseConfigurationAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        bool closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);

        try
        {
            int maxPurchaseQuantity;
            await using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT [MaxPurchaseQuantity] FROM [Store].[Product] WHERE [Id] = @ProductId AND [IsActive] = 1;";
                AddProductIdParameter(command, productId);
                object? result = await command.ExecuteScalarAsync(cancellationToken);
                if (result is null or DBNull) return null;
                maxPurchaseQuantity = Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
            }

            Dictionary<string, List<ProductPurchaseOptionValueRow>> options = new(StringComparer.Ordinal);
            await using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT o.[OptionKey], v.[ValueKey], CONVERT(DECIMAL(19,0), ROUND(v.[PriceDelta] * 1000000, 0))
                    FROM [Store].[ProductPurchaseOption] o
                    INNER JOIN [Store].[ProductPurchaseOptionValue] v ON v.[IdProductPurchaseOption] = o.[Id]
                    WHERE o.[IdProduct] = @ProductId
                    ORDER BY o.[SortOrder], v.[SortOrder];
                    """;
                AddProductIdParameter(command, productId);
                await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    string optionKey = reader.GetString(0);
                    if (!options.TryGetValue(optionKey, out List<ProductPurchaseOptionValueRow>? values))
                        options[optionKey] = values = [];
                    values.Add(new(reader.GetString(1), reader.GetDecimal(2)));
                }
            }

            List<ProductAccessoryRow> accessories = [];
            await using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT a.[AccessoryKey], pa.[IsRequired]
                    FROM [Store].[ProductAccessory] pa
                    INNER JOIN [Store].[Accessory] a ON a.[Id] = pa.[IdAccessory] AND a.[IsActive] = 1
                    WHERE pa.[IdProduct] = @ProductId
                    ORDER BY pa.[SortOrder];
                    """;
                AddProductIdParameter(command, productId);
                await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken)) accessories.Add(new(reader.GetString(0), reader.GetBoolean(1)));
            }

            return new(maxPurchaseQuantity,
                options.Select(pair => new ProductPurchaseOptionRow(pair.Key, pair.Value)).ToArray(), accessories);
        }
        finally
        {
            if (closeWhenDone) await connection.CloseAsync();
        }
    }

    private static void AddProductIdParameter(DbCommand command, int productId)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "@ProductId";
        parameter.DbType = DbType.Int64;
        parameter.Value = productId;
        command.Parameters.Add(parameter);
    }

    private sealed class OfferBuilder(int productId, string salesPlanId, decimal priceAmount,
        decimal discountPercent, int stockQuantity, int maxPurchaseQuantity, bool isEnabled)
    {
        public Dictionary<string, decimal> AccessoryPriceAmounts { get; } = new(StringComparer.Ordinal);
        public List<string> AccessoryIds { get; } = [];
        public Dictionary<string, string> SpecificationOverrides { get; } = new(StringComparer.Ordinal);

        public StoreSalesOfferRow Build() => new(productId, salesPlanId, priceAmount, discountPercent,
            stockQuantity, maxPurchaseQuantity, isEnabled, AccessoryPriceAmounts, AccessoryIds, SpecificationOverrides);
    }
}
