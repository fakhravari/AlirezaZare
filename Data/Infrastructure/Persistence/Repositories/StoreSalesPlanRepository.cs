using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Application.Store.Models;
using Shop.Data.Domain.Store.Entities;

namespace Shop.Data.Infrastructure.Persistence.Repositories;

public sealed class StoreSalesPlanRepository(ShopDbContext dbContext) : IStoreSalesPlanRepository
{
    public async Task<StoreSalesPlanSnapshot> ReadSalesPlanSnapshotAsync(CancellationToken cancellationToken)
    {
            var planEntities = await dbContext.SalesPlanReferences.AsNoTracking().Where(x => x.IdStockOwnerType == 1)
                .OrderByDescending(x => x.IsRegular).ThenBy(x => x.Code).ToListAsync(cancellationToken);
            List<StoreSalesPlanRow> plans = planEntities.Select(x => new StoreSalesPlanRow(x.StoreSalesPlanKey, x.Code, x.DisplayName, x.PlanType, x.IsRegular, x.IsActive, x.StartsAtUtc, x.EndsAtUtc, x.Label, x.ShowCountdown)).ToList();

            var siteWarehouseIds = await dbContext.StoreSiteIdentities.AsNoTracking().Where(x => x.IdWarehouse.HasValue)
                .Select(x => x.IdWarehouse!.Value).Distinct().ToListAsync(cancellationToken);
            var stockRows = await (from balance in dbContext.StockBalances.AsNoTracking()
                                   join inventory in dbContext.InventoryItems.AsNoTracking() on balance.IdInventoryItem equals inventory.Id
                                   join plan in dbContext.SalesPlanReferences.AsNoTracking() on balance.IdSalesPlanReference equals plan.Id
                                   where siteWarehouseIds.Contains(balance.IdWarehouse) && inventory.IsActive &&
                                       ((inventory.IdProductVariant.HasValue && dbContext.ProductVariants.Any(v => v.Id == inventory.IdProductVariant.Value && v.IsActive)) ||
                                        (!inventory.IdProductVariant.HasValue && !dbContext.ProductVariants.Any(v => v.IdProduct == inventory.IdProduct && v.IsActive)))
                                   group balance.Quantity by new { inventory.IdProduct, balance.IdSalesPlanReference } into grouped
                                   select new { grouped.Key.IdProduct, grouped.Key.IdSalesPlanReference, Quantity = grouped.Sum() }).ToListAsync(cancellationToken);
            var siteStock = stockRows.ToDictionary(x => (x.IdProduct, x.IdSalesPlanReference), x => x.Quantity);
            var products = await dbContext.Products.AsNoTracking().Where(x => x.IsCatalogActive && x.IsStoreEnabled).ToListAsync(cancellationToken);
            var customOffers = await dbContext.StoreProductSalesPlanOffers.AsNoTracking().ToListAsync(cancellationToken);
            var activePriceRows = await dbContext.StorePriceVersions.AsNoTracking()
                .Where(x => x.IsActive && products.Select(p => p.Id).Contains(x.IdProduct))
                .OrderByDescending(x => x.Id).ToListAsync(cancellationToken);
            var pricesByScope = activePriceRows.GroupBy(x => x.ScopeKey, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            Dictionary<(int ProductId, string PlanId), OfferBuilder> offers = [];
            foreach (var product in products)
            foreach (var plan in planEntities)
            {
                var custom = customOffers.SingleOrDefault(x => x.IdProduct == product.Id && x.IdSalesPlan == plan.Id);
                if (custom is null && !plan.IsRegular) continue;
                string priceScope = StorePriceScopes.Product(product.Id, plan.Id);
                pricesByScope.TryGetValue(priceScope, out var priceVersion);
                decimal discount = priceVersion?.DiscountPercent ?? custom?.DiscountPercent ?? product.DiscountPercent ?? 0m;
                decimal price = priceVersion?.PriceAmount ?? custom?.PriceAmount ?? decimal.Round((product.PriceAmount ?? 0m) * 100m / (100m - discount), 0, MidpointRounding.AwayFromZero);
                decimal stock = siteStock.GetValueOrDefault((product.Id, plan.Id));
                int stockQuantity = stock >= int.MaxValue ? int.MaxValue : checked((int)Math.Floor(stock));
                int maxQuantity = custom?.MaxPurchaseQuantity ?? product.MaxPurchaseQuantity ?? 99;
                bool enabled = (custom?.IsEnabled ?? true) && product.IsCatalogActive && product.IsStoreEnabled && plan.IsActive;
                offers[(checked((int)product.Id), plan.StoreSalesPlanKey)] = new(checked((int)product.Id), plan.StoreSalesPlanKey, price, discount, stockQuantity, maxQuantity, enabled, priceVersion?.Id ?? 0);
            }

            var optionRows = await (from option in dbContext.StoreProductPurchaseOptions.AsNoTracking()
                                    join product in dbContext.Products.AsNoTracking() on option.IdProduct equals product.Id
                                    join value in dbContext.StoreProductPurchaseOptionValues.AsNoTracking() on option.Id equals value.IdProductPurchaseOption
                                    where product.IsCatalogActive && product.IsStoreEnabled
                                    orderby product.Id, option.SortOrder, value.SortOrder
                                    select new { option.IdProduct, option.OptionKey, option.Label, value.Id, value.ValueKey, ValueLabel = value.Label, value.PriceDelta }).ToListAsync(cancellationToken);
            Dictionary<(int ProductId, string Key), ProductPriceOptionBuilder> priceOptions = [];
            foreach (var row in optionRows)
            {
                var key = (checked((int)row.IdProduct), row.OptionKey);
                if (!priceOptions.TryGetValue(key, out var option)) priceOptions[key] = option = new(key.Item1, row.OptionKey, row.Label);
                string scope = StorePriceScopes.Option(row.Id);
                pricesByScope.TryGetValue(scope, out var optionVersion);
                option.Values.Add(new(row.ValueKey, row.ValueLabel, optionVersion?.PriceAmount ?? decimal.Round(row.PriceDelta * 1_000_000m, 0, MidpointRounding.AwayFromZero)) { Id = row.Id, PriceVersionId = optionVersion?.Id ?? 0 });
            }

            Dictionary<(int ProductId, long VariantId), InventoryVariantBuilder> inventoryVariants = [];
            var eligibleVariants = await (from variant in dbContext.ProductVariants.AsNoTracking()
                                          join product in dbContext.Products.AsNoTracking() on variant.IdProduct equals product.Id
                                          where variant.IsActive && product.IsWarehouseEnabled && product.IsCatalogActive && product.IsStoreEnabled &&
                                                dbContext.InventoryItems.Any(inventory => inventory.IdProductVariant == variant.Id && inventory.IsActive)
                                          select new { ProductId = variant.IdProduct, VariantId = variant.Id, variant.Name, variant.Code, variant.AttributesJson, variant.IsDefault }).ToListAsync(cancellationToken);
            foreach (var row in eligibleVariants) inventoryVariants[(checked((int)row.ProductId), row.VariantId)] = new(checked((int)row.ProductId), row.VariantId, row.Name, row.Code, row.AttributesJson, row.IsDefault);
            var inventoryStock = await (from inventory in dbContext.InventoryItems.AsNoTracking()
                                        join balance in dbContext.StockBalances.AsNoTracking() on inventory.Id equals balance.IdInventoryItem
                                        join plan in dbContext.SalesPlanReferences.AsNoTracking() on balance.IdSalesPlanReference equals plan.Id
                                        where inventory.IsActive && inventory.IdProductVariant.HasValue && siteWarehouseIds.Contains(balance.IdWarehouse) && plan.IdStockOwnerType == 1
                                        select new { inventory.IdProductVariant, inventory.IdProduct, plan.StoreSalesPlanKey, balance.Quantity }).ToListAsync(cancellationToken);
            var productByVariantId = eligibleVariants.GroupBy(x => x.VariantId).ToDictionary(x => x.Key, x => checked((int)x.First().ProductId));
            foreach (var stock in inventoryStock)
                if (stock.IdProductVariant.HasValue && productByVariantId.TryGetValue(stock.IdProductVariant.Value, out int productId) && inventoryVariants.TryGetValue((productId, stock.IdProductVariant.Value), out var variant))
                    variant.StockBySalesPlan[stock.StoreSalesPlanKey] = variant.StockBySalesPlan.GetValueOrDefault(stock.StoreSalesPlanKey) + stock.Quantity;

            var variantOfferRows = await (from offer in dbContext.StoreProductVariantSalesPlanOffers.AsNoTracking()
                                         join plan in dbContext.SalesPlanReferences.AsNoTracking() on offer.IdSalesPlan equals plan.Id
                                         join variant in dbContext.ProductVariants.AsNoTracking() on offer.IdProductVariant equals variant.Id
                                         where offer.IsEnabled && variant.IsActive
                                         orderby offer.IdProductVariant, plan.StoreSalesPlanKey
                                         select new { offer.IdProductVariant, plan.Id, plan.StoreSalesPlanKey, offer.PriceAmount, offer.DiscountPercent }).ToListAsync(cancellationToken);
            var variantById = inventoryVariants.Values.ToDictionary(item => item.VariantId);
            foreach (var row in variantOfferRows)
            {
                if (!variantById.TryGetValue(row.IdProductVariant, out var variant)) continue;
                string scope = StorePriceScopes.Variant(row.IdProductVariant, row.Id);
                pricesByScope.TryGetValue(scope, out var priceVersion);
                variant.PriceBySalesPlan[row.StoreSalesPlanKey] = priceVersion?.PriceAmount ?? row.PriceAmount;
                variant.DiscountBySalesPlan[row.StoreSalesPlanKey] = priceVersion?.DiscountPercent ?? row.DiscountPercent;
                if (priceVersion is not null) variant.PriceVersionIdsBySalesPlan[row.StoreSalesPlanKey] = priceVersion.Id;
            }

            var accessoryBases = await (from productAccessory in dbContext.StoreProductAccessories.AsNoTracking()
                                        join accessory in dbContext.StoreAccessories.AsNoTracking() on productAccessory.IdAccessory equals accessory.Id
                                        where accessory.IsActive
                                        orderby productAccessory.IdProduct, productAccessory.SortOrder
                                        select new { productAccessory.IdProduct, productAccessory.IdAccessory, accessory.AccessoryKey, productAccessory.PriceAmount }).ToListAsync(cancellationToken);
            var offerAccessoryOverrides = await dbContext.StoreProductSalesPlanOfferAccessories.AsNoTracking().ToListAsync(cancellationToken);
            var customOfferByKey = customOffers.ToDictionary(x => (x.IdProduct, x.IdSalesPlan));
            foreach (var pair in offers)
            {
                long productId = pair.Key.ProductId;
                var plan = planEntities.FirstOrDefault(x => x.StoreSalesPlanKey == pair.Key.PlanId);
                if (plan is null) continue;
                bool hasCustom = customOfferByKey.TryGetValue((productId, plan.Id), out var customOffer);
                foreach (var accessory in accessoryBases.Where(x => x.IdProduct == productId))
                {
                    decimal price;
                    if (hasCustom)
                    {
                        var overrideRow = offerAccessoryOverrides.FirstOrDefault(x => x.IdProductSalesPlanOffer == customOffer!.Id && x.IdAccessory == accessory.IdAccessory);
                        if (overrideRow is null || !overrideRow.IsAllowed) continue;
                        string scope = StorePriceScopes.Accessory(productId, accessory.IdAccessory, plan.Id);
                        pricesByScope.TryGetValue(scope, out var accessoryVersion);
                        price = accessoryVersion?.PriceAmount ?? overrideRow.PriceAmount;
                        if (accessoryVersion is not null) pair.Value.AccessoryPriceVersionIds[accessory.AccessoryKey] = accessoryVersion.Id;
                    }
                    else
                    {
                        string scope = StorePriceScopes.Accessory(productId, accessory.IdAccessory, plan.Id);
                        pricesByScope.TryGetValue(scope, out var accessoryVersion);
                        price = accessoryVersion?.PriceAmount ?? accessory.PriceAmount;
                        if (accessoryVersion is not null) pair.Value.AccessoryPriceVersionIds[accessory.AccessoryKey] = accessoryVersion.Id;
                    }
                    pair.Value.AccessoryIds.Add(accessory.AccessoryKey);
                    pair.Value.AccessoryPriceAmounts[accessory.AccessoryKey] = price;
                }
            }

            var specificationRows = await (from overrideRow in dbContext.StoreProductSalesPlanOfferSpecificationOverrides.AsNoTracking()
                                          join offer in dbContext.StoreProductSalesPlanOffers.AsNoTracking() on overrideRow.IdProductSalesPlanOffer equals offer.Id
                                          join product in dbContext.Products.AsNoTracking() on offer.IdProduct equals product.Id
                                          join plan in dbContext.SalesPlanReferences.AsNoTracking() on offer.IdSalesPlan equals plan.Id
                                          where product.IsCatalogActive && product.IsStoreEnabled
                                          orderby product.Id, plan.StoreSalesPlanKey, overrideRow.SpecificationKey
                                          select new { product.Id, plan.StoreSalesPlanKey, overrideRow.SpecificationKey, overrideRow.Value }).ToListAsync(cancellationToken);
            foreach (var row in specificationRows)
                if (offers.TryGetValue((checked((int)row.Id), row.StoreSalesPlanKey), out var offer))
                    offer.SpecificationOverrides[row.SpecificationKey] = row.Value;

            return new StoreSalesPlanSnapshot(plans, offers.Values.Select(offer => offer.Build()).ToArray(),
                inventoryVariants.Values.Select(variant => variant.Build()).ToArray())
            {
                PriceOptions = priceOptions.Values.Select(option => option.Build()).ToArray()
            };
    }

    public async Task<ProductPurchaseConfiguration?> ReadProductPurchaseConfigurationAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking()
            .Where(x => x.Id == productId && x.IsCatalogActive && x.IsStoreEnabled)
            .Select(x => new { x.MaxPurchaseQuantity }).SingleOrDefaultAsync(cancellationToken);
        if (product is null) return null;
        int maxPurchaseQuantity = product.MaxPurchaseQuantity ?? 99;

        var optionRows = await (from option in dbContext.StoreProductPurchaseOptions.AsNoTracking()
                                join value in dbContext.StoreProductPurchaseOptionValues.AsNoTracking() on option.Id equals value.IdProductPurchaseOption
                                where option.IdProduct == productId
                                orderby option.SortOrder, value.SortOrder
                                select new { option.OptionKey, ValueKey = value.ValueKey, value.Id, value.PriceDelta }).ToListAsync(cancellationToken);
        var optionIds = optionRows.Select(x => x.Id).ToArray();
        var optionVersions = await dbContext.StorePriceVersions.AsNoTracking()
            .Where(x => x.IsActive && x.PriceKind == StorePriceKinds.Option && optionIds.Contains(x.TargetId!.Value))
            .OrderByDescending(x => x.Id).ToListAsync(cancellationToken);
        var optionVersionById = optionVersions.GroupBy(x => x.TargetId!.Value).ToDictionary(group => group.Key, group => group.First());
        var options = optionRows.GroupBy(x => x.OptionKey, StringComparer.Ordinal)
            .Select(group => new ProductPurchaseOptionRow(group.Key,
                group.Select(x => new ProductPurchaseOptionValueRow(x.ValueKey,
                    optionVersionById.TryGetValue(x.Id, out var version) ? version.PriceAmount : decimal.Round(x.PriceDelta * 1_000_000m, 0, MidpointRounding.AwayFromZero))
                    { Id = x.Id, PriceVersionId = optionVersionById.GetValueOrDefault(x.Id)?.Id ?? 0 }).ToArray()))
            .ToArray();
        var accessories = await (from productAccessory in dbContext.StoreProductAccessories.AsNoTracking()
                                 join accessory in dbContext.StoreAccessories.AsNoTracking() on productAccessory.IdAccessory equals accessory.Id
                                 where productAccessory.IdProduct == productId && accessory.IsActive
                                 orderby productAccessory.SortOrder
                                 select new ProductAccessoryRow(accessory.AccessoryKey, productAccessory.IsRequired))
            .ToListAsync(cancellationToken);
        return new(maxPurchaseQuantity, options, accessories);
    }

    private sealed class OfferBuilder(
        int productId,
        string salesPlanId,
        decimal priceAmount,
        decimal discountPercent,
        int stockQuantity,
        int maxPurchaseQuantity,
        bool isEnabled,
        long priceVersionId)
    {
        public Dictionary<string, decimal> AccessoryPriceAmounts { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, long> AccessoryPriceVersionIds { get; } = new(StringComparer.Ordinal);
        public List<string> AccessoryIds { get; } = [];
        public Dictionary<string, string> SpecificationOverrides { get; } = new(StringComparer.Ordinal);

        public StoreSalesOfferRow Build() => new(productId, salesPlanId, priceAmount, discountPercent,
            stockQuantity, maxPurchaseQuantity, isEnabled, AccessoryPriceAmounts, AccessoryIds, SpecificationOverrides)
        { PriceVersionId = priceVersionId, AccessoryPriceVersionIds = AccessoryPriceVersionIds };
    }

    private sealed class ProductPriceOptionBuilder(int productId, string key, string label)
    {
        public List<StoreProductPriceOptionValueRow> Values { get; } = [];
        public StoreProductPriceOptionRow Build() => new(productId, key, label, Values);
    }

    private sealed class InventoryVariantBuilder
    {
        public InventoryVariantBuilder(int productId, long variantId, string name, string? code, string? attributesJson, bool isDefault)
        {
            ProductId = productId;
            VariantId = variantId;
            Name = name;
            Code = code;
            AttributesJson = attributesJson;
            IsDefault = isDefault;
        }

        private int ProductId { get; }
        public long VariantId { get; }
        private string Name { get; }
        private string? Code { get; }
        private string? AttributesJson { get; }
        private bool IsDefault { get; }
        public Dictionary<string, decimal> StockBySalesPlan { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, decimal> PriceBySalesPlan { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, decimal> DiscountBySalesPlan { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, long> PriceVersionIdsBySalesPlan { get; } = new(StringComparer.Ordinal);

        public StoreInventoryVariantRow Build() => new(ProductId, VariantId, Name, Code, AttributesJson, IsDefault,
            StockBySalesPlan, PriceBySalesPlan, DiscountBySalesPlan) { PriceVersionIdsBySalesPlan = PriceVersionIdsBySalesPlan };
    }
}
