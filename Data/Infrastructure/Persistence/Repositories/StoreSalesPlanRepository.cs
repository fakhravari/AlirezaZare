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
        List<StoreSalesPlanRow> plans = planEntities.Select(x => new StoreSalesPlanRow(x.StoreSalesPlanKey, x.Code,
                x.DisplayName, x.PlanType, x.IsRegular, x.IsActive, x.StartsAtUtc, x.EndsAtUtc, x.Label,
                x.ShowCountdown))
            .ToList();

        var siteWarehouseIds = await dbContext.StoreSiteIdentities.AsNoTracking().Where(x => x.IdWarehouse.HasValue)
            .Select(x => x.IdWarehouse!.Value).Distinct().ToListAsync(cancellationToken);
        var stockRows = await dbContext.StockBalances
            .AsNoTracking()
            .Join(
                dbContext.InventoryItems.AsNoTracking(),
                balance => balance.IdInventoryItem,
                inventory => inventory.Id,
                (balance, inventory) => new { balance, inventory })
            .Join(
                dbContext.SalesPlanReferences.AsNoTracking(),
                balanceInventory => balanceInventory.balance.IdSalesPlanReference,
                plan => plan.Id,
                (balanceInventory, plan) => new { balanceInventory.balance, balanceInventory.inventory, plan })
            .Where(balanceInventoryPlan =>
                siteWarehouseIds.Contains(balanceInventoryPlan.balance.IdWarehouse) &&
                balanceInventoryPlan.inventory.IsActive && balanceInventoryPlan.inventory.Product.IsWarehouseEnabled &&
                ((balanceInventoryPlan.inventory.IdVariant.HasValue &&
                  dbContext.ProductVariants.Any(variant =>
                      variant.Id == balanceInventoryPlan.inventory.IdVariant.Value && variant.IsActive) &&
                  dbContext.StoreProductVariantSalesPlanOffers.Any(offer =>
                      offer.IdVariant == balanceInventoryPlan.inventory.IdVariant &&
                      offer.IdSalesPlan == balanceInventoryPlan.plan.Id && offer.IsEnabled) &&
                  dbContext.StorePriceVersions.Any(version =>
                      version.IdVariant == balanceInventoryPlan.inventory.IdVariant &&
                      version.IdSalesPlan == balanceInventoryPlan.plan.Id && version.IsActive)) ||
                 (!balanceInventoryPlan.inventory.IdVariant.HasValue &&
                  !dbContext.ProductVariants.Any(variant =>
                      variant.IdProduct == balanceInventoryPlan.inventory.IdProduct && variant.IsActive))))
            .GroupBy(balanceInventoryPlan => new
            {
                balanceInventoryPlan.inventory.IdProduct,
                balanceInventoryPlan.balance.IdSalesPlanReference
            })
            .Select(stockGroup => new
            {
                stockGroup.Key.IdProduct,
                stockGroup.Key.IdSalesPlanReference,
                Quantity = stockGroup.Sum(balanceInventoryPlan => balanceInventoryPlan.balance.Quantity)
            })
            .ToListAsync(cancellationToken);
        var siteStock = stockRows.ToDictionary(x => (x.IdProduct, x.IdSalesPlanReference), x => x.Quantity);
        var products = await dbContext.Products.AsNoTracking().Where(x => x.IsStoreEnabled)
            .ToListAsync(cancellationToken);
        var customOffers = await dbContext.StoreProductSalesPlanOffers.AsNoTracking().ToListAsync(cancellationToken);
        var activePriceRows = await dbContext.StorePriceVersions.AsNoTracking()
            .Where(x => x.IsActive && products.Select(p => p.Id).Contains(x.IdProduct)).OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
        var planProductPrices = activePriceRows.Where(x => !x.IdVariant.HasValue &&
                                                           !x.IdAccessory.HasValue)
            .ToDictionary(x => (x.IdProduct, x.IdSalesPlan));
        var variantPrices = activePriceRows.Where(x => x.IdVariant.HasValue)
            .ToDictionary(x => (x.IdVariant!.Value, x.IdSalesPlan));
        var accessoryPrices = activePriceRows.Where(x => x.IdAccessory.HasValue)
            .ToDictionary(x => (x.IdProduct, x.IdAccessory!.Value, x.IdSalesPlan));
        var variantOfferRows = await dbContext.StoreProductVariantSalesPlanOffers
            .AsNoTracking()
            .Join(
                dbContext.ProductVariants.AsNoTracking(),
                offer => offer.IdVariant,
                variant => variant.Id,
                (offer, variant) => new { offer, variant })
            .Join(
                dbContext.SalesPlanReferences.AsNoTracking(),
                offerVariant => offerVariant.offer.IdSalesPlan,
                plan => plan.Id,
                (offerVariant, plan) => new { offerVariant.offer, offerVariant.variant, plan })
            .Where(offerVariantPlan =>
                offerVariantPlan.offer.IsEnabled && offerVariantPlan.plan.IdStockOwnerType == 1 &&
                offerVariantPlan.variant.IsActive && offerVariantPlan.variant.Product.IsStoreEnabled &&
                dbContext.InventoryItems.Any(inventory =>
                    inventory.IdVariant == offerVariantPlan.variant.Id && inventory.IsActive))
            .OrderBy(offerVariantPlan => offerVariantPlan.offer.IdVariant)
            .ThenBy(offerVariantPlan => offerVariantPlan.plan.StoreSalesPlanKey)
            .Select(offerVariantPlan => new
            {
                IdProductVariant = offerVariantPlan.offer.IdVariant,
                ProductId = offerVariantPlan.variant.IdProduct,
                VariantId = offerVariantPlan.variant.Id,
                PlanId = offerVariantPlan.plan.Id,
                offerVariantPlan.plan.StoreSalesPlanKey
            })
            .ToListAsync(cancellationToken);
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
            int maxQuantity = custom?.MaxPurchaseQuantity ?? product.MaxPurchaseQuantity;
            bool enabled = (custom?.IsEnabled ?? true) && product.IsStoreEnabled && plan.IsActive;
            offers[(checked((int)product.Id), plan.StoreSalesPlanKey)] = new(checked((int)product.Id),
                plan.StoreSalesPlanKey, price, discount, stockQuantity, maxQuantity, enabled, priceVersion?.Id ?? 0,
                priceVersion?.FinalPriceAmount ?? 0m);
        }

        Dictionary<(int ProductId, long VariantId), InventoryVariantBuilder> inventoryVariants = [];
        var eligibleVariants = await dbContext.ProductVariants
            .AsNoTracking()
            .Join(
                dbContext.Products.AsNoTracking(),
                variant => variant.IdProduct,
                product => product.Id,
                (variant, product) => new { variant, product })
            .Where(variantProduct =>
                variantProduct.variant.IsActive && variantProduct.product.IsStoreEnabled &&
                dbContext.InventoryItems.Any(inventory =>
                    inventory.IdVariant == variantProduct.variant.Id && inventory.IsActive))
            .Select(variantProduct => new
            {
                ProductId = variantProduct.variant.IdProduct,
                VariantId = variantProduct.variant.Id,
                Name = variantProduct.variant.VariantName.Name,
                variantProduct.variant.IsDefault
            })
            .ToListAsync(cancellationToken);
        long[] eligibleVariantIds = eligibleVariants.Select(x => x.VariantId).ToArray();
        var attributeRows = await dbContext.ProductVariantAttributeValues
            .AsNoTracking()
            .Join(
                dbContext.ProductAttributeValues.AsNoTracking(),
                link => link.IdProductAttributeValue,
                value => value.Id,
                (link, value) => new { link, value })
            .Join(
                dbContext.ProductAttributes.AsNoTracking(),
                linkValue => linkValue.value.IdProductAttribute,
                attribute => attribute.Id,
                (linkValue, attribute) => new { linkValue.link, linkValue.value, attribute })
            .Where(linkValueAttribute =>
                eligibleVariantIds.Contains(linkValueAttribute.link.IdProductVariant) &&
                linkValueAttribute.value.IsActive && linkValueAttribute.attribute.IsActive)
            .OrderBy(linkValueAttribute => linkValueAttribute.attribute.SortOrder)
            .ThenBy(linkValueAttribute => linkValueAttribute.attribute.Id)
            .ThenBy(linkValueAttribute => linkValueAttribute.value.Id)
            .Select(linkValueAttribute => new
            {
                linkValueAttribute.link.IdProductVariant,
                linkValueAttribute.attribute.Key,
                AttributeLabel = linkValueAttribute.attribute.Label,
                linkValueAttribute.value.ValueKey,
                linkValueAttribute.value.DisplayValue,
                linkValueAttribute.value.Swatch
            })
            .ToListAsync(cancellationToken);
        var attributesByVariant = attributeRows.GroupBy(x => x.IdProductVariant).ToDictionary(group => group.Key,
            group => (JsonElement?)JsonSerializer.SerializeToElement(group.Select(x => new
            {
                key = x.Key, label = x.AttributeLabel, value = x.ValueKey, display = x.DisplayValue, swatch = x.Swatch
            }).ToArray()));
        foreach (var row in eligibleVariants)
            inventoryVariants[(checked((int)row.ProductId), row.VariantId)] = new(checked((int)row.ProductId),
                row.VariantId, row.Name, null, attributesByVariant.GetValueOrDefault(row.VariantId), row.IsDefault);
        var inventoryStock = await dbContext.InventoryItems
            .AsNoTracking()
            .Join(
                dbContext.StockBalances.AsNoTracking(),
                inventory => inventory.Id,
                balance => balance.IdInventoryItem,
                (inventory, balance) => new { inventory, balance })
            .Join(
                dbContext.SalesPlanReferences.AsNoTracking(),
                inventoryBalance => inventoryBalance.balance.IdSalesPlanReference,
                plan => plan.Id,
                (inventoryBalance, plan) => new { inventoryBalance.inventory, inventoryBalance.balance, plan })
            .Where(inventoryBalancePlan =>
                inventoryBalancePlan.inventory.IsActive && inventoryBalancePlan.inventory.Product.IsWarehouseEnabled &&
                inventoryBalancePlan.inventory.IdVariant.HasValue &&
                siteWarehouseIds.Contains(inventoryBalancePlan.balance.IdWarehouse) &&
                inventoryBalancePlan.plan.IdStockOwnerType == 1 &&
                dbContext.StoreProductVariantSalesPlanOffers.Any(offer =>
                    offer.IdVariant == inventoryBalancePlan.inventory.IdVariant &&
                    offer.IdSalesPlan == inventoryBalancePlan.plan.Id && offer.IsEnabled) &&
                dbContext.StorePriceVersions.Any(version =>
                    version.IdVariant == inventoryBalancePlan.inventory.IdVariant &&
                    version.IdSalesPlan == inventoryBalancePlan.plan.Id && version.IsActive))
            .Select(inventoryBalancePlan => new
            {
                IdProductVariant = inventoryBalancePlan.inventory.IdVariant,
                inventoryBalancePlan.inventory.IdProduct,
                inventoryBalancePlan.plan.StoreSalesPlanKey,
                inventoryBalancePlan.balance.Quantity
            })
            .ToListAsync(cancellationToken);
        var productByVariantId = eligibleVariants.GroupBy(x => x.VariantId)
            .ToDictionary(x => x.Key, x => checked((int)x.First().ProductId));
        foreach (var stock in inventoryStock)
            if (stock.IdProductVariant.HasValue &&
                productByVariantId.TryGetValue(stock.IdProductVariant.Value, out int productId) &&
                inventoryVariants.TryGetValue((productId, stock.IdProductVariant.Value), out var variant))
                variant.StockBySalesPlan[stock.StoreSalesPlanKey] =
                    variant.StockBySalesPlan.GetValueOrDefault(stock.StoreSalesPlanKey) + stock.Quantity;

        var variantById = inventoryVariants.Values.ToDictionary(item => item.VariantId);
        var disabledVariantPlans = await dbContext.StoreProductVariantSalesPlanOffers
            .AsNoTracking()
            .Join(
                dbContext.SalesPlanReferences.AsNoTracking(),
                offer => offer.IdSalesPlan,
                plan => plan.Id,
                (offer, plan) => new { offer, plan })
            .Where(offerPlan =>
                !offerPlan.offer.IsEnabled && offerPlan.plan.IdStockOwnerType == 1 &&
                eligibleVariantIds.Contains(offerPlan.offer.IdVariant))
            .Select(offerPlan => new { IdProductVariant = offerPlan.offer.IdVariant, offerPlan.plan.StoreSalesPlanKey })
            .ToListAsync(cancellationToken);
        foreach (var row in disabledVariantPlans)
            if (variantById.TryGetValue(row.IdProductVariant, out var variant))
                variant.DisabledSalesPlanIds.Add(row.StoreSalesPlanKey);
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

        var accessoryBases = await dbContext.StoreProductAccessories
            .AsNoTracking()
            .Join(
                dbContext.StoreAccessories.AsNoTracking(),
                productAccessory => productAccessory.IdAccessory,
                accessory => accessory.Id,
                (productAccessory, accessory) => new { productAccessory, accessory })
            .Where(productAccessory => productAccessory.accessory.IsActive)
            .OrderBy(productAccessory => productAccessory.productAccessory.IdProduct)
            .ThenBy(productAccessory => productAccessory.productAccessory.SortOrder)
            .Select(productAccessory => new
            {
                productAccessory.productAccessory.IdProduct,
                productAccessory.productAccessory.IdAccessory,
                productAccessory.productAccessory.IsRequired
            })
            .ToListAsync(cancellationToken);
        var offerAccessoryOverrides = await dbContext.StoreProductSalesPlanOfferAccessories.AsNoTracking()
            .ToListAsync(cancellationToken);
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
                    var overrideRow = offerAccessoryOverrides.FirstOrDefault(x =>
                        x.IdOffer == customOffer!.Id && x.IdAccessory == accessory.IdAccessory);
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

        var specificationRows = await dbContext.StoreProductSalesPlanOfferSpecificationOverrides
            .AsNoTracking()
            .Join(
                dbContext.StoreProductSalesPlanOffers.AsNoTracking(),
                overrideRow => overrideRow.IdOffer,
                offer => offer.Id,
                (overrideRow, offer) => new { overrideRow, offer })
            .Join(
                dbContext.Products.AsNoTracking(),
                overrideOffer => overrideOffer.offer.IdProduct,
                product => product.Id,
                (overrideOffer, product) => new { overrideOffer.overrideRow, overrideOffer.offer, product })
            .Join(
                dbContext.SalesPlanReferences.AsNoTracking(),
                overrideOfferProduct => overrideOfferProduct.offer.IdSalesPlan,
                plan => plan.Id,
                (overrideOfferProduct, plan) => new
                {
                    overrideOfferProduct.overrideRow,
                    overrideOfferProduct.product,
                    plan
                })
            .Where(overrideProductPlan => overrideProductPlan.product.IsStoreEnabled)
            .OrderBy(overrideProductPlan => overrideProductPlan.product.Id)
            .ThenBy(overrideProductPlan => overrideProductPlan.plan.StoreSalesPlanKey)
            .ThenBy(overrideProductPlan => overrideProductPlan.overrideRow.SpecificationKey)
            .Select(overrideProductPlan => new
            {
                overrideProductPlan.product.Id,
                overrideProductPlan.plan.StoreSalesPlanKey,
                overrideProductPlan.overrideRow.SpecificationKey,
                overrideProductPlan.overrideRow.Value
            })
            .ToListAsync(cancellationToken);
        foreach (var row in specificationRows)
            if (offers.TryGetValue((checked((int)row.Id), row.StoreSalesPlanKey), out var offer))
                offer.SpecificationOverrides[row.SpecificationKey] = row.Value;

        return new StoreSalesPlanSnapshot(plans, offers.Values.Select(offer => offer.Build()).ToArray(),
            inventoryVariants.Values.Select(variant => variant.Build()).ToArray());
    }

    public async Task<ProductPurchaseConfiguration?> ReadProductPurchaseConfigurationAsync(int productId,
        string salesPlanId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking().Where(x => x.Id == productId && x.IsStoreEnabled)
            .Select(x => new { x.MaxPurchaseQuantity }).SingleOrDefaultAsync(cancellationToken);
        if (product is null) return null;

        int maxPurchaseQuantity = product.MaxPurchaseQuantity;

        long? planId = await dbContext.SalesPlanReferences.AsNoTracking()
            .Where(x => x.IdStockOwnerType == 1 && x.StoreSalesPlanKey == salesPlanId).Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!planId.HasValue) return null;
        var accessoryRows = await dbContext.StoreProductAccessories
            .AsNoTracking()
            .Join(
                dbContext.StoreAccessories.AsNoTracking(),
                productAccessory => productAccessory.IdAccessory,
                accessory => accessory.Id,
                (productAccessory, accessory) => new { productAccessory, accessory })
            .Where(productAccessory => productAccessory.productAccessory.IdProduct == productId &&
                                       productAccessory.accessory.IsActive)
            .OrderBy(productAccessory => productAccessory.productAccessory.SortOrder)
            .Select(productAccessory => new
            {
                productAccessory.accessory.Id,
                productAccessory.productAccessory.IsRequired
            })
            .ToListAsync(cancellationToken);
        var accessories = accessoryRows.Select(x =>
                new ProductAccessoryRow(x.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), x.IsRequired))
            .ToArray();

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
        decimal priceVersionId,
        decimal finalPriceAmount)
    {
        public bool IsEnabled { get; private set; } = isEnabled;
        public Dictionary<string, decimal> AccessoryPriceAmounts { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, decimal> AccessoryPriceVersionIds { get; } = new(StringComparer.Ordinal);
        public List<string> AccessoryIds { get; } = [];
        public Dictionary<string, string> SpecificationOverrides { get; } = new(StringComparer.Ordinal);

        public StoreSalesOfferRow Build() => new(productId, salesPlanId, priceAmount, discountPercent,
            stockQuantity, maxPurchaseQuantity, IsEnabled, AccessoryPriceAmounts, AccessoryIds,
            SpecificationOverrides)
        {
            PriceVersionId = priceVersionId, FinalPriceAmount = finalPriceAmount,
            AccessoryPriceVersionIds = AccessoryPriceVersionIds
        };

        public void Disable() => IsEnabled = false;
    }

    private sealed class InventoryVariantBuilder
    {
        public InventoryVariantBuilder(int productId, long variantId, string name, string? code,
            JsonElement? attributes, bool isDefault)
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
        public Dictionary<string, decimal> PriceVersionIdsBySalesPlan { get; } = new(StringComparer.Ordinal);
        public List<string> DisabledSalesPlanIds { get; } = [];

        public StoreInventoryVariantRow Build() => new(ProductId, VariantId, Name, Code, Attributes, IsDefault,
            StockBySalesPlan, PriceBySalesPlan, DiscountBySalesPlan)
        {
            PriceVersionIdsBySalesPlan = PriceVersionIdsBySalesPlan, FinalPriceBySalesPlan = FinalPriceBySalesPlan,
            DisabledSalesPlanIds = DisabledSalesPlanIds
        };
    }
}
