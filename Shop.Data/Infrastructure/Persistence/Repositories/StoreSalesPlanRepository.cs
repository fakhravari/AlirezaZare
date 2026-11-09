using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shop.Database.Application.Store.Contracts;
using Shop.Database.Application.Store.Models;

namespace Shop.Database.Infrastructure.Persistence.Repositories;

public sealed class StoreSalesPlanRepository(ShopDbContext dbContext) : IStoreSalesPlanRepository
{
    private DbCommand CreateCommand(DbConnection connection)
    {
        DbCommand command = connection.CreateCommand();
        command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        return command;
    }
    public async Task<StoreSalesPlanSnapshot> ReadSalesPlanSnapshotAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        bool closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);

        try
        {
            List<StoreSalesPlanRow> plans = [];
            await using (DbCommand command = CreateCommand(connection))
            {
                command.CommandText = """
                                      SELECT [SalesPlanKey], [Code], [Name], [PlanType], [IsDefault], [IsEnabled],
                                             [StartsAtUtc], [EndsAtUtc], [Label], [ShowCountdown]
                                      FROM [Warehouse].[SalesPlanReference]
                                      WHERE [IdStockOwnerType] = 1
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
            await using (DbCommand command = CreateCommand(connection))
            {
                command.CommandText = """
                                      WITH [SiteStock] AS
                                      (
                                          SELECT wp.[Id] AS [IdProduct], spr.[Id] AS [IdSalesPlan],
                                                 SUM(sb.[Quantity]) AS [Quantity]
                                          FROM [Store].[SiteSetting] setting
                                          INNER JOIN [Warehouse].[StockBalance] sb ON sb.[IdWarehouse] = setting.[IdWarehouse]
                                          INNER JOIN [Warehouse].[InventoryItem] ii ON ii.[Id] = sb.[IdInventoryItem]
                                          INNER JOIN [Warehouse].[Product] wp ON wp.[Id] = ii.[IdProduct]
                                          INNER JOIN [Warehouse].[SalesPlanReference] spr ON spr.[Id] = sb.[IdSalesPlanReference]
                                          WHERE ii.[IsActive] = 1 AND
                                          (
                                              (ii.[IdProductVariant] IS NOT NULL AND EXISTS
                                                  (SELECT 1 FROM [Warehouse].[ProductVariant] activeVariant WHERE activeVariant.[Id] = ii.[IdProductVariant] AND activeVariant.[IsActive] = 1))
                                              OR
                                              (ii.[IdProductVariant] IS NULL AND NOT EXISTS
                                                  (SELECT 1 FROM [Warehouse].[ProductVariant] activeVariant WHERE activeVariant.[IdProduct] = wp.[Id] AND activeVariant.[IsActive] = 1))
                                          )
                                          GROUP BY wp.[Id], spr.[Id]
                                      ), [OfferRows] AS
                                      (
                                          SELECT p.[Id] AS [IdProduct], sp.[Id] AS [IdSalesPlan], sp.[SalesPlanKey],
                                                 CONVERT(DECIMAL(19,0), ROUND(p.[PriceAmount] * 100.0 /
                                                     (100.0 - ISNULL(p.[DiscountPercent], 0)), 0)) AS [PriceAmount],
                                                 ISNULL(p.[DiscountPercent], 0) AS [DiscountPercent],
                                                 p.[MaxPurchaseQuantity], CONVERT(BIT, CASE WHEN p.[IsCatalogActive] = 1 AND p.[IsStoreEnabled] = 1 THEN 1 ELSE 0 END) AS [IsEnabled]
                                          FROM [Warehouse].[Product] p
                                          CROSS JOIN [Warehouse].[SalesPlanReference] sp
                                          WHERE sp.[IsDefault] = 1 AND p.[IsCatalogActive] = 1 AND p.[IsStoreEnabled] = 1
                                          UNION ALL
                                          SELECT p.[Id], sp.[Id], sp.[SalesPlanKey], o.[PriceAmount], o.[DiscountPercent],
                                                 o.[MaxPurchaseQuantity], CONVERT(BIT, CASE WHEN o.[IsEnabled] = 1 AND p.[IsCatalogActive] = 1 AND p.[IsStoreEnabled] = 1 AND sp.[IsEnabled] = 1 THEN 1 ELSE 0 END)
                                          FROM [Store].[ProductSalesPlanOffer] o
                                          INNER JOIN [Warehouse].[Product] p ON p.[Id] = o.[IdProduct]
                                          INNER JOIN [Warehouse].[SalesPlanReference] sp ON sp.[Id] = o.[IdSalesPlan]
                                          WHERE p.[IsCatalogActive] = 1 AND p.[IsStoreEnabled] = 1
                                      )
                                      SELECT o.[IdProduct], o.[SalesPlanKey], o.[PriceAmount], o.[DiscountPercent],
                                             CONVERT(INT, ISNULL(s.[Quantity], 0)), o.[MaxPurchaseQuantity],
                                             CONVERT(BIT, CASE WHEN o.[IsEnabled] = 1 AND sp.[IsEnabled] = 1 THEN 1 ELSE 0 END)
                                      FROM [OfferRows] o
                                      INNER JOIN [Warehouse].[SalesPlanReference] sp ON sp.[Id] = o.[IdSalesPlan]
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

            Dictionary<(int ProductId, string Key), ProductPriceOptionBuilder> priceOptions = [];
            await using (DbCommand command = CreateCommand(connection))
            {
                command.CommandText = """
                                      SELECT p.[Id], opt.[OptionKey], opt.[Label], val.[ValueKey], val.[Label],
                                             CONVERT(DECIMAL(19,0), ROUND(val.[PriceDelta] * 1000000, 0))
                                      FROM [Store].[ProductPurchaseOption] opt
                                      INNER JOIN [Warehouse].[Product] p ON p.[Id] = opt.[IdProduct] AND p.[IsCatalogActive] = 1 AND p.[IsStoreEnabled] = 1
                                      INNER JOIN [Store].[ProductPurchaseOptionValue] val ON val.[IdProductPurchaseOption] = opt.[Id]
                                      ORDER BY p.[Id], opt.[SortOrder], val.[SortOrder];
                                      """;
                await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    int productId = checked((int)reader.GetInt64(0));
                    string key = reader.GetString(1);
                    var lookup = (productId, key);
                    if (!priceOptions.TryGetValue(lookup, out ProductPriceOptionBuilder? option))
                        priceOptions[lookup] = option = new(productId, key, reader.GetString(2));
                    option.Values.Add(new(reader.GetString(3), reader.GetString(4), reader.GetDecimal(5)));
                }
            }

            Dictionary<(int ProductId, long VariantId), InventoryVariantBuilder> inventoryVariants = [];
            await using (DbCommand command = CreateCommand(connection))
            {
                command.CommandText = """
                                      SELECT p.[Id], v.[Id], v.[Name], v.[Code], v.[AttributesJson],
                                             storePlan.[SalesPlanKey], SUM(sb.[Quantity])
                                      FROM [Warehouse].[ProductVariant] v
                                      INNER JOIN [Warehouse].[Product] p ON p.[Id] = v.[IdProduct] AND p.[IsWarehouseEnabled] = 1 AND p.[IsCatalogActive] = 1 AND p.[IsStoreEnabled] = 1
                                      INNER JOIN [Warehouse].[InventoryItem] ii ON ii.[IdProductVariant] = v.[Id] AND ii.[IsActive] = 1
                                      LEFT JOIN [Store].[SiteSetting] setting ON setting.[IdWarehouse] IS NOT NULL
                                      LEFT JOIN [Warehouse].[StockBalance] sb ON sb.[IdWarehouse] = setting.[IdWarehouse] AND sb.[IdInventoryItem] = ii.[Id]
                                      LEFT JOIN [Warehouse].[SalesPlanReference] spr ON spr.[Id] = sb.[IdSalesPlanReference]
                                      LEFT JOIN [Warehouse].[SalesPlanReference] storePlan ON storePlan.[Id] = spr.[Id] AND storePlan.[IdStockOwnerType] = 1
                                      WHERE v.[IsActive] = 1
                                      GROUP BY p.[Id], v.[Id], v.[Name], v.[Code], v.[AttributesJson], storePlan.[SalesPlanKey]
                                      ORDER BY p.[Id], v.[Id], storePlan.[SalesPlanKey];
                                      """;
                await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    int productId = checked((int)reader.GetInt64(0));
                    long variantId = reader.GetInt64(1);
                    var key = (productId, variantId);
                    if (!inventoryVariants.TryGetValue(key, out InventoryVariantBuilder? variant))
                        inventoryVariants[key] = variant = new(productId, variantId, reader.GetString(2),
                            reader.IsDBNull(3) ? null : reader.GetString(3),
                            reader.IsDBNull(4) ? null : reader.GetString(4));
                    if (!reader.IsDBNull(5))
                        variant.StockBySalesPlan[reader.GetString(5)] = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6);
                }
            }

            await using (DbCommand command = CreateCommand(connection))
            {
                command.CommandText = """
                                      SELECT offer.[IdProductVariant], [plan].[SalesPlanKey], offer.[PriceAmount], offer.[DiscountPercent]
                                      FROM [Store].[ProductVariantSalesPlanOffer] offer
                                      INNER JOIN [Warehouse].[SalesPlanReference] [plan] ON [plan].[Id] = offer.[IdSalesPlan]
                                      INNER JOIN [Warehouse].[ProductVariant] variant ON variant.[Id] = offer.[IdProductVariant] AND variant.[IsActive] = 1
                                      WHERE offer.[IsEnabled] = 1
                                      ORDER BY offer.[IdProductVariant], [plan].[SalesPlanKey];
                                      """;
                await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    long variantId = reader.GetInt64(0);
                    InventoryVariantBuilder? variant =
                        inventoryVariants.Values.SingleOrDefault(item => item.VariantId == variantId);
                    if (variant is null) continue;
                    string planId = reader.GetString(1);
                    variant.PriceBySalesPlan[planId] = reader.GetDecimal(2);
                    variant.DiscountBySalesPlan[planId] = reader.GetDecimal(3);
                }
            }

            await using (DbCommand command = CreateCommand(connection))
            {
                command.CommandText = """
                                      WITH [OfferPairs] AS
                                      (
                                          SELECT p.[Id] AS [IdProduct], sp.[Id] AS [IdSalesPlan], sp.[SalesPlanKey], CONVERT(BIGINT, NULL) AS [IdOffer]
                                          FROM [Warehouse].[Product] p CROSS JOIN [Warehouse].[SalesPlanReference] sp WHERE sp.[IsDefault] = 1 AND p.[IsCatalogActive] = 1 AND p.[IsStoreEnabled] = 1
                                          UNION ALL
                                          SELECT o.[IdProduct], o.[IdSalesPlan], sp.[SalesPlanKey], o.[Id]
                                          FROM [Store].[ProductSalesPlanOffer] o
                                          INNER JOIN [Warehouse].[SalesPlanReference] sp ON sp.[Id] = o.[IdSalesPlan]
                                          INNER JOIN [Warehouse].[Product] p ON p.[Id] = o.[IdProduct] AND p.[IsCatalogActive] = 1 AND p.[IsStoreEnabled] = 1
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

            await using (DbCommand command = CreateCommand(connection))
            {
                command.CommandText = """
                                      SELECT p.[Id], sp.[SalesPlanKey], ov.[SpecificationKey], ov.[Value]
                                      FROM [Store].[ProductSalesPlanOfferSpecificationOverride] ov
                                      INNER JOIN [Store].[ProductSalesPlanOffer] o ON o.[Id] = ov.[IdProductSalesPlanOffer]
                                      INNER JOIN [Warehouse].[Product] p ON p.[Id] = o.[IdProduct]
                                      INNER JOIN [Warehouse].[SalesPlanReference] sp ON sp.[Id] = o.[IdSalesPlan]
                                          WHERE p.[IsCatalogActive] = 1 AND p.[IsStoreEnabled] = 1
                                      ORDER BY p.[Id], sp.[SalesPlanKey], ov.[SpecificationKey];
                                      """;
                await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    offers[(checked((int)reader.GetInt64(0)), reader.GetString(1))]
                        .SpecificationOverrides.Add(reader.GetString(2), reader.GetString(3));
            }

            return new StoreSalesPlanSnapshot(plans, offers.Values.Select(offer => offer.Build()).ToArray(),
                inventoryVariants.Values.Select(variant => variant.Build()).ToArray())
            {
                PriceOptions = priceOptions.Values.Select(option => option.Build()).ToArray()
            };
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
            await using (DbCommand command = CreateCommand(connection))
            {
                command.CommandText =
                    "SELECT [MaxPurchaseQuantity] FROM [Warehouse].[Product] WHERE [Id] = @ProductId AND [IsCatalogActive] = 1 AND [IsStoreEnabled] = 1;";
                AddProductIdParameter(command, productId);
                object? result = await command.ExecuteScalarAsync(cancellationToken);
                if (result is null or DBNull) return null;
                maxPurchaseQuantity = Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
            }

            Dictionary<string, List<ProductPurchaseOptionValueRow>> options = new(StringComparer.Ordinal);
            await using (DbCommand command = CreateCommand(connection))
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
            await using (DbCommand command = CreateCommand(connection))
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
                while (await reader.ReadAsync(cancellationToken))
                    accessories.Add(new(reader.GetString(0), reader.GetBoolean(1)));
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

    private sealed class OfferBuilder(
        int productId,
        string salesPlanId,
        decimal priceAmount,
        decimal discountPercent,
        int stockQuantity,
        int maxPurchaseQuantity,
        bool isEnabled)
    {
        public Dictionary<string, decimal> AccessoryPriceAmounts { get; } = new(StringComparer.Ordinal);
        public List<string> AccessoryIds { get; } = [];
        public Dictionary<string, string> SpecificationOverrides { get; } = new(StringComparer.Ordinal);

        public StoreSalesOfferRow Build() => new(productId, salesPlanId, priceAmount, discountPercent,
            stockQuantity, maxPurchaseQuantity, isEnabled, AccessoryPriceAmounts, AccessoryIds, SpecificationOverrides);
    }

    private sealed class ProductPriceOptionBuilder(int productId, string key, string label)
    {
        public List<StoreProductPriceOptionValueRow> Values { get; } = [];
        public StoreProductPriceOptionRow Build() => new(productId, key, label, Values);
    }

    private sealed class InventoryVariantBuilder
    {
        public InventoryVariantBuilder(int productId, long variantId, string name, string? code, string? attributesJson)
        {
            ProductId = productId;
            VariantId = variantId;
            Name = name;
            Code = code;
            AttributesJson = attributesJson;
        }

        private int ProductId { get; }
        public long VariantId { get; }
        private string Name { get; }
        private string? Code { get; }
        private string? AttributesJson { get; }
        public Dictionary<string, decimal> StockBySalesPlan { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, decimal> PriceBySalesPlan { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, decimal> DiscountBySalesPlan { get; } = new(StringComparer.Ordinal);

        public StoreInventoryVariantRow Build() => new(ProductId, VariantId, Name, Code, AttributesJson,
            StockBySalesPlan, PriceBySalesPlan, DiscountBySalesPlan);
    }
}
