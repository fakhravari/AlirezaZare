using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
                               where siteWarehouseIds.Contains(balance.IdWarehouse) && inventory.IsActive && inventory.Product.IsWarehouseEnabled &&
                                   ((inventory.IdProductVariant.HasValue && dbContext.ProductVariants.Any(v => v.Id == inventory.IdProductVariant.Value && v.IsActive) &&
                                     dbContext.StoreProductVariantSalesPlanOffers.Any(o => o.IdProductVariant == inventory.IdProductVariant && o.IdSalesPlan == plan.Id && o.IsEnabled) &&
                                     dbContext.StorePriceVersions.Any(v => v.IdProductVariant == inventory.IdProductVariant && v.IdSalesPlan == plan.Id && v.IsActive)) ||
                                    (!inventory.IdProductVariant.HasValue && !dbContext.ProductVariants.Any(v => v.IdProduct == inventory.IdProduct && v.IsActive)))
                               group balance.Quantity by new { inventory.IdProduct, balance.IdSalesPlanReference } into grouped
                               select new { grouped.Key.IdProduct, grouped.Key.IdSalesPlanReference, Quantity = grouped.Sum() }).ToListAsync(cancellationToken);
        var siteStock = stockRows.ToDictionary(x => (x.IdProduct, x.IdSalesPlanReference), x => x.Quantity);
        var products = await dbContext.Products.AsNoTracking().Where(x => x.IsStoreEnabled).ToListAsync(cancellationToken);
        var customOffers = await dbContext.StoreProductSalesPlanOffers.AsNoTracking().ToListAsync(cancellationToken);
        var activePriceRows = await dbContext.StorePriceVersions.AsNoTracking().Where(x => x.IsActive && products.Select(p => p.Id).Contains(x.IdProduct)).OrderByDescending(x => x.Id).ToListAsync(cancellationToken);
        var planProductPrices = activePriceRows.Where(x => !x.IdProductVariant.HasValue &&
                                                           !x.IdAccessory.HasValue)
            .ToDictionary(x => (x.IdProduct, x.IdSalesPlan));
        var variantPrices = activePriceRows.Where(x => x.IdProductVariant.HasValue).ToDictionary(x => (x.IdProductVariant!.Value, x.IdSalesPlan));
        var accessoryPrices = activePriceRows.Where(x => x.IdAccessory.HasValue)
            .ToDictionary(x => (x.IdProduct, x.IdAccessory!.Value, x.IdSalesPlan));
        var variantOfferRows = await (from offer in dbContext.StoreProductVariantSalesPlanOffers.AsNoTracking()
                                      join variant in dbContext.ProductVariants.AsNoTracking() on offer.IdProductVariant equals variant.Id
                                      join plan in dbContext.SalesPlanReferences.AsNoTracking() on offer.IdSalesPlan equals plan.Id
                                      where offer.IsEnabled && plan.IdStockOwnerType == 1 && variant.IsActive && variant.Product.IsStoreEnabled &&
                                               dbContext.InventoryItems.Any(inventory => inventory.IdProductVariant == variant.Id && inventory.IsActive)
                                      orderby offer.IdProductVariant, plan.StoreSalesPlanKey
                                      select new { offer.IdProductVariant, ProductId = variant.IdProduct, VariantId = variant.Id, PlanId = plan.Id, plan.StoreSalesPlanKey }).ToListAsync(cancellationToken);
        var variantOnlyOfferKeys = variantOfferRows
            .Where(row => variantPrices.ContainsKey((row.VariantId, row.PlanId)))
            .Select(row => (row.ProductId, row.PlanId)).ToHashSet();
        Dictionary<(int ProductId, string PlanId), OfferBuilder> offers = [];
        foreach (var product in products)
            foreach (var plan in planEntities)
            {
                var custom = customOffers.SingleOrDefault(x => x.IdProduct == product.Id && x.IdSalesPlan == plan.Id);
                if (custom is null && !plan.IsRegular) continue;
                planProductPrices.TryGetValue((product.Id, plan.Id), out var priceVersion);
                if (priceVersion is null && !variantOnlyOfferKeys.Contains((product.Id, plan.Id))) continue;
                decimal discount = priceVersion?.DiscountPercent ?? 0;
                decimal price = priceVersion?.PriceBeforeDiscountAmount ?? 0;
                decimal stock = siteStock.GetValueOrDefault((product.Id, plan.Id));
                int stockQuantity = stock >= int.MaxValue ? int.MaxValue : checked((int)Math.Floor(stock));
                int maxQuantity = custom?.MaxPurchaseQuantity ?? product.MaxPurchaseQuantity ?? 99;
                bool enabled = (custom?.IsEnabled ?? true) && product.IsStoreEnabled && plan.IsActive;
                offers[(checked((int)product.Id), plan.StoreSalesPlanKey)] = new(checked((int)product.Id), plan.StoreSalesPlanKey, price, discount, stockQuantity, maxQuantity, enabled, priceVersion?.Id ?? 0, priceVersion?.FinalPriceAmount ?? 0m);
            }

        Dictionary<(int ProductId, long VariantId), InventoryVariantBuilder> inventoryVariants = [];
        var eligibleVariants = await (from variant in dbContext.ProductVariants.AsNoTracking()
                                      join product in dbContext.Products.AsNoTracking() on variant.IdProduct equals product.Id
                                      where variant.IsActive && product.IsStoreEnabled &&
                                            dbContext.InventoryItems.Any(inventory => inventory.IdProductVariant == variant.Id && inventory.IsActive)
                                      select new { ProductId = variant.IdProduct, VariantId = variant.Id, Name = variant.VariantName.Name, variant.IsDefault }).ToListAsync(cancellationToken);
        long[] eligibleVariantIds = eligibleVariants.Select(x => x.VariantId).ToArray();
        var attributeRows = await (from link in dbContext.ProductVariantAttributeValues.AsNoTracking()
                                   join value in dbContext.ProductAttributeValues.AsNoTracking() on link.IdProductAttributeValue equals value.Id
                                   join attribute in dbContext.ProductAttributes.AsNoTracking() on value.IdProductAttribute equals attribute.Id
                                   where eligibleVariantIds.Contains(link.IdProductVariant) && value.IsActive && attribute.IsActive
                                   orderby attribute.SortOrder, attribute.Id, value.Id
                                   select new { link.IdProductVariant, attribute.Key, AttributeLabel = attribute.Label, value.ValueKey, value.DisplayValue, value.Swatch }).ToListAsync(cancellationToken);
        var attributesByVariant = attributeRows.GroupBy(x => x.IdProductVariant).ToDictionary(group => group.Key,
            group => (JsonElement?)JsonSerializer.SerializeToElement(group.Select(x => new { key = x.Key, label = x.AttributeLabel, value = x.ValueKey, display = x.DisplayValue, swatch = x.Swatch }).ToArray()));
        foreach (var row in eligibleVariants) inventoryVariants[(checked((int)row.ProductId), row.VariantId)] = new(checked((int)row.ProductId), row.VariantId, row.Name, null, attributesByVariant.GetValueOrDefault(row.VariantId), row.IsDefault);
        var inventoryStock = await (from inventory in dbContext.InventoryItems.AsNoTracking()
                                    join balance in dbContext.StockBalances.AsNoTracking() on inventory.Id equals balance.IdInventoryItem
                                    join plan in dbContext.SalesPlanReferences.AsNoTracking() on balance.IdSalesPlanReference equals plan.Id
                                    where inventory.IsActive && inventory.Product.IsWarehouseEnabled && inventory.IdProductVariant.HasValue && siteWarehouseIds.Contains(balance.IdWarehouse) && plan.IdStockOwnerType == 1 &&
                                        dbContext.StoreProductVariantSalesPlanOffers.Any(o => o.IdProductVariant == inventory.IdProductVariant && o.IdSalesPlan == plan.Id && o.IsEnabled) &&
                                        dbContext.StorePriceVersions.Any(v => v.IdProductVariant == inventory.IdProductVariant && v.IdSalesPlan == plan.Id && v.IsActive)
                                    select new { inventory.IdProductVariant, inventory.IdProduct, plan.StoreSalesPlanKey, balance.Quantity }).ToListAsync(cancellationToken);
        var productByVariantId = eligibleVariants.GroupBy(x => x.VariantId).ToDictionary(x => x.Key, x => checked((int)x.First().ProductId));
        foreach (var stock in inventoryStock)
            if (stock.IdProductVariant.HasValue && productByVariantId.TryGetValue(stock.IdProductVariant.Value, out int productId) && inventoryVariants.TryGetValue((productId, stock.IdProductVariant.Value), out var variant))
                variant.StockBySalesPlan[stock.StoreSalesPlanKey] = variant.StockBySalesPlan.GetValueOrDefault(stock.StoreSalesPlanKey) + stock.Quantity;

        var variantById = inventoryVariants.Values.ToDictionary(item => item.VariantId);
        var disabledVariantPlans = await (from offer in dbContext.StoreProductVariantSalesPlanOffers.AsNoTracking()
                                          join plan in dbContext.SalesPlanReferences.AsNoTracking() on offer.IdSalesPlan equals plan.Id
                                          where !offer.IsEnabled && plan.IdStockOwnerType == 1 && eligibleVariantIds.Contains(offer.IdProductVariant)
                                          select new { offer.IdProductVariant, plan.StoreSalesPlanKey }).ToListAsync(cancellationToken);
        foreach (var row in disabledVariantPlans)
            if (variantById.TryGetValue(row.IdProductVariant, out var variant)) variant.DisabledSalesPlanIds.Add(row.StoreSalesPlanKey);
        foreach (var row in variantOfferRows)
        {
            if (!variantById.TryGetValue(row.IdProductVariant, out var variant)) continue;
            variantPrices.TryGetValue((row.IdProductVariant, row.PlanId), out var priceVersion);
            if (priceVersion is null) continue;
            variant.PriceBySalesPlan[row.StoreSalesPlanKey] = priceVersion.PriceBeforeDiscountAmount;
            variant.DiscountBySalesPlan[row.StoreSalesPlanKey] = priceVersion.DiscountPercent;
            variant.FinalPriceBySalesPlan[row.StoreSalesPlanKey] = priceVersion.FinalPriceAmount;

            variant.PriceVersionIdsBySalesPlan[row.StoreSalesPlanKey] = priceVersion.Id;
        }

        var accessoryBases = await (from productAccessory in dbContext.StoreProductAccessories.AsNoTracking()
                                    join accessory in dbContext.StoreAccessories.AsNoTracking() on productAccessory.IdAccessory equals accessory.Id
                                    where accessory.IsActive
                                    orderby productAccessory.IdProduct, productAccessory.SortOrder
                                    select new { productAccessory.IdProduct, productAccessory.IdAccessory, productAccessory.IsRequired }).ToListAsync(cancellationToken);
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
                if (hasCustom)
                {
                    var overrideRow = offerAccessoryOverrides.FirstOrDefault(x => x.IdProductSalesPlanOffer == customOffer!.Id && x.IdAccessory == accessory.IdAccessory);
                    if (overrideRow is null || !overrideRow.IsAllowed) continue;
                }
                if (!accessoryPrices.TryGetValue((productId, accessory.IdAccessory, plan.Id), out var accessoryVersion))
                {
                    if (accessory.IsRequired) pair.Value.Disable();
                    continue;
                }
                string accessoryId = accessory.IdAccessory.ToString(System.Globalization.CultureInfo.InvariantCulture);
                pair.Value.AccessoryPriceVersionIds[accessoryId] = accessoryVersion.Id;
                pair.Value.AccessoryIds.Add(accessoryId);
                pair.Value.AccessoryPriceAmounts[accessoryId] = accessoryVersion.FinalPriceAmount;
            }
        }

        var specificationRows = await (from overrideRow in dbContext.StoreProductSalesPlanOfferSpecificationOverrides.AsNoTracking()
                                       join offer in dbContext.StoreProductSalesPlanOffers.AsNoTracking() on overrideRow.IdProductSalesPlanOffer equals offer.Id
                                       join product in dbContext.Products.AsNoTracking() on offer.IdProduct equals product.Id
                                       join plan in dbContext.SalesPlanReferences.AsNoTracking() on offer.IdSalesPlan equals plan.Id
                                       where product.IsStoreEnabled
                                       orderby product.Id, plan.StoreSalesPlanKey, overrideRow.SpecificationKey
                                       select new { product.Id, plan.StoreSalesPlanKey, overrideRow.SpecificationKey, overrideRow.Value }).ToListAsync(cancellationToken);
        foreach (var row in specificationRows)
            if (offers.TryGetValue((checked((int)row.Id), row.StoreSalesPlanKey), out var offer))
                offer.SpecificationOverrides[row.SpecificationKey] = row.Value;

        return new StoreSalesPlanSnapshot(plans, offers.Values.Select(offer => offer.Build()).ToArray(),
            inventoryVariants.Values.Select(variant => variant.Build()).ToArray());
    }

    public async Task<ProductPurchaseConfiguration?> ReadProductPurchaseConfigurationAsync(int productId, string salesPlanId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking().Where(x => x.Id == productId && x.IsStoreEnabled)
            .Select(x => new { x.MaxPurchaseQuantity }).SingleOrDefaultAsync(cancellationToken);
        if (product is null) return null;

        int maxPurchaseQuantity = product.MaxPurchaseQuantity ?? 99;

        long? planId = await dbContext.SalesPlanReferences.AsNoTracking().Where(x => x.IdStockOwnerType == 1 && x.StoreSalesPlanKey == salesPlanId).Select(x => (long?)x.Id).SingleOrDefaultAsync(cancellationToken);
        if (!planId.HasValue) return null;
        var accessoryRows = await (from productAccessory in dbContext.StoreProductAccessories.AsNoTracking()
                                 join accessory in dbContext.StoreAccessories.AsNoTracking() on productAccessory.IdAccessory equals accessory.Id
                                 where productAccessory.IdProduct == productId && accessory.IsActive
                                 orderby productAccessory.SortOrder
                                 select new { accessory.Id, productAccessory.IsRequired }).ToListAsync(cancellationToken);
        var accessories = accessoryRows.Select(x => new ProductAccessoryRow(x.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), x.IsRequired)).ToArray();

        return new(maxPurchaseQuantity, accessories);
    }

    private sealed class OfferBuilder(
        int productId,
        string salesPlanId,
        decimal priceAmount,
        decimal discountPercent,
        int stockQuantity,
        int maxPurchaseQuantity,
        bool isEnabled,
        long priceVersionId,
        decimal finalPriceAmount)
    {
        public bool IsEnabled { get; private set; } = isEnabled;
        public Dictionary<string, decimal> AccessoryPriceAmounts { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, long> AccessoryPriceVersionIds { get; } = new(StringComparer.Ordinal);
        public List<string> AccessoryIds { get; } = [];
        public Dictionary<string, string> SpecificationOverrides { get; } = new(StringComparer.Ordinal);

        public StoreSalesOfferRow Build() => new(productId, salesPlanId, priceAmount, discountPercent,
            stockQuantity, maxPurchaseQuantity, IsEnabled, AccessoryPriceAmounts, AccessoryIds, SpecificationOverrides)
        { PriceVersionId = priceVersionId, FinalPriceAmount = finalPriceAmount, AccessoryPriceVersionIds = AccessoryPriceVersionIds };

        public void Disable() => IsEnabled = false;
    }

    private sealed class InventoryVariantBuilder
    {
        public InventoryVariantBuilder(int productId, long variantId, string name, string? code, JsonElement? attributes, bool isDefault)
        {
            ProductId = productId;
            VariantId = variantId;
            Name = name;
            Code = code;
            Attributes = attributes;
            IsDefault = isDefault;
        }

        private int ProductId { get; }
        public long VariantId { get; }
        private string Name { get; }
        private string? Code { get; }
        private JsonElement? Attributes { get; }
        private bool IsDefault { get; }
        public Dictionary<string, decimal> StockBySalesPlan { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, decimal> PriceBySalesPlan { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, decimal> DiscountBySalesPlan { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, decimal> FinalPriceBySalesPlan { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, long> PriceVersionIdsBySalesPlan { get; } = new(StringComparer.Ordinal);
        public List<string> DisabledSalesPlanIds { get; } = [];

        public StoreInventoryVariantRow Build() => new(ProductId, VariantId, Name, Code, Attributes, IsDefault,
            StockBySalesPlan, PriceBySalesPlan, DiscountBySalesPlan)
        { PriceVersionIdsBySalesPlan = PriceVersionIdsBySalesPlan, FinalPriceBySalesPlan = FinalPriceBySalesPlan, DisabledSalesPlanIds = DisabledSalesPlanIds };
    }
}
