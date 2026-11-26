using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Application.Account.Contracts;
using Shop.Data.Application.Store;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Application.Store.Models;

namespace Shop.Data.Infrastructure.Account;

public sealed class AccountCheckoutData(
    ShopDbContext db,
    ICheckoutSalesPlans plans,
    IStoreSalesPlanRepository purchaseConfigurations,
    IAccountDataCommands accountData) : IAccountCheckoutData
{
    private sealed record CheckoutLine(decimal ProductId, int Quantity, string SalesPlanId, JsonElement Options, long? VariantId);

    private static readonly JsonSerializerOptions AccountStateJsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static string NormalizeCouponCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

    private Task<bool> ProductVariantExistsAsync(decimal productId, long variantId, CancellationToken cancellationToken) =>
        accountData.ProductVariantExistsAsync(productId, variantId, cancellationToken);
    public async Task<CheckoutResult> QuoteCouponAsync(long profileId, string? couponCode, CancellationToken ct)
    {
        string code = NormalizeCouponCode(couponCode);
        if (code.Length is < 1 or > 32 || code.Any(character => !char.IsAsciiLetterOrDigit(character)))
            return CheckoutResult.BadRequest(new { code = "invalid-coupon" });

        var coupon = await db.AccountCoupons
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Code == code, ct);
        if (coupon is null || !coupon.Enabled || (coupon.IdProfile.HasValue && coupon.IdProfile != profileId) ||
            coupon.ExpiresOn < DateOnly.FromDateTime(DateTime.UtcNow))
            return CheckoutResult.NotFound(new { code = "coupon-unavailable" });

        int uses = await db.StoreCouponUsages
            .AsNoTracking()
            .CountAsync(item => item.IdCoupon == coupon.Id, ct);
        if (uses >= coupon.UsageLimit)
            return CheckoutResult.Conflict(new { code = "coupon-limit-reached" });

        var storedRows = await db.AccountCarts
            .AsNoTracking()
            .Join(
                db.AccountCartItems
                    .AsNoTracking(),
                cart => cart.Id,
                item => item.IdCart,
                (cart, item) => new { cart, item })
            .Join(
                db.SalesPlanReferences
                    .AsNoTracking(),
                cartItem => cartItem.item.IdSalesPlan,
                plan => plan.Id,
                (cartItem, plan) => new { cartItem.cart, cartItem.item, plan })
            .Where(cartItemPlan => cartItemPlan.cart.IdProfile == profileId)
            .Select(cartItemPlan => new
            {
                cartItemPlan.item.Id,
                cartItemPlan.item.IdProduct,
                cartItemPlan.item.IdVariant,
                cartItemPlan.item.Quantity,
                SalesPlanId = cartItemPlan.plan.StoreSalesPlanKey
            })
            .ToListAsync(ct);
        if (storedRows.Count == 0) return CheckoutResult.Conflict(new { code = "coupon-cart-empty" });

        var catalog = await plans.ReadAsync(ct);
        decimal eligibleAmount = 0;
        decimal subtotalAmount = 0;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (var row in storedRows)
        {
            SalesPlanQuote quote;
            try
            {
                quote = catalog.Quote(checked((int)row.IdProduct), row.SalesPlanId, row.Quantity, now);
            }
            catch (SalesPlanUnavailableException)
            {
                return CheckoutResult.Conflict(new { code = "price-or-stock-changed" });
            }

            bool eligible = row.SalesPlanId == "regular" && quote.DiscountPercent <= 0;
            decimal unitAmount = quote.UnitAmount;
            ProductInventoryVariant[] productVariants =
                catalog.InventoryVariants.Where(variant => variant.ProductId == row.IdProduct).ToArray();
            ProductInventoryVariant? inventoryVariant = catalog.InventoryVariants.SingleOrDefault(variant =>
                variant.ProductId == row.IdProduct && variant.VariantId == row.IdVariant);
            if (productVariants.Length > 0)
            {
                if (inventoryVariant is null ||
                    !inventoryVariant.StockBySalesPlan.TryGetValue(row.SalesPlanId, out decimal variantStock) ||
                    variantStock < row.Quantity)
                    return CheckoutResult.Conflict(new { code = "inventory-variant-or-stock-changed" });
                if (!inventoryVariant.PriceBySalesPlan.TryGetValue(row.SalesPlanId, out decimal variantPrice))
                    return CheckoutResult.Conflict(new { code = "variant-price-unavailable" });
                decimal variantDiscount = inventoryVariant.DiscountBySalesPlan.GetValueOrDefault(row.SalesPlanId);
                eligible = row.SalesPlanId == "regular" && variantDiscount <= 0;
                if (!inventoryVariant.FinalPriceBySalesPlan.TryGetValue(row.SalesPlanId, out unitAmount))
                    return CheckoutResult.Conflict(new { code = "variant-price-unavailable" });
            }
            else if (row.IdVariant.HasValue)
                return CheckoutResult.Conflict(new { code = "product-variant-unavailable" });

            ProductPurchaseConfiguration? configuration =
                await purchaseConfigurations.ReadProductPurchaseConfigurationAsync(checked((int)row.IdProduct), row.SalesPlanId, ct);
            if (configuration is null)
                return CheckoutResult.Conflict(new { code = "product-options-unavailable" });

            if (row.Quantity > configuration.MaxPurchaseQuantity)
                return CheckoutResult.Conflict(new { code = "purchase-limit" });

            decimal[] selectedAccessoryIds = (await db.AccountCartItemAccessories
                .AsNoTracking()
                .Join(
                    db.StoreProductAccessories
                        .AsNoTracking(),
                    selected => selected.IdProductAccessory,
                    link => link.Id,
                    (selected, link) => new { selected, link })
                .Join(
                    db.StoreAccessories
                        .AsNoTracking(),
                    selectedLink => selectedLink.link.IdAccessory,
                    accessory => accessory.Id,
                    (selectedLink, accessory) => new { selectedLink.selected, accessory })
                .Where(selectedAccessory => selectedAccessory.selected.IdCartItem == row.Id)
                .Select(selectedAccessory => selectedAccessory.accessory.Id)
                .ToListAsync(ct))
                .ToArray();
            string[] selectedAccessoryKeys = selectedAccessoryIds
                .Select(id => id.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            var offer = catalog.Offers.SingleOrDefault(candidate =>
                candidate.ProductId == row.IdProduct && candidate.SalesPlanId == row.SalesPlanId);
            if (offer is null) return CheckoutResult.Conflict(new { code = "price-or-stock-changed" });

            if (selectedAccessoryKeys.Distinct(StringComparer.Ordinal).Count() != selectedAccessoryKeys.Length ||
                configuration.Accessories.Any(accessory => accessory.IsRequired &&
                                                           !selectedAccessoryKeys.Contains(accessory.Key,
                                                               StringComparer.Ordinal)) ||
                selectedAccessoryKeys.Any(key => configuration.Accessories.All(accessory => accessory.Key != key)) ||
                offer.AccessoryIds is { } allowedAccessories &&
                selectedAccessoryKeys.Any(key => !allowedAccessories.Contains(key, StringComparer.Ordinal)))
                return CheckoutResult.Conflict(new { code = "invalid-product-options" });

            foreach (var accessory in configuration.Accessories)
                if (accessory.IsRequired || selectedAccessoryKeys.Contains(accessory.Key, StringComparer.Ordinal))
                {
                    if (!offer.AccessoryPriceAmounts.TryGetValue(accessory.Key, out decimal amount))
                        return CheckoutResult.Conflict(new { code = "invalid-product-options" });

                    unitAmount += amount;
                }

            if (unitAmount < 0) return CheckoutResult.Conflict(new { code = "invalid-price" });
            decimal lineAmount = checked(unitAmount * row.Quantity);
            subtotalAmount += lineAmount;
            if (eligible) eligibleAmount += lineAmount;
        }

        if (eligibleAmount <= 0) return CheckoutResult.Conflict(new { code = "coupon-no-eligible-items" });

        decimal discountAmount = decimal.Floor(eligibleAmount * coupon.DiscountPercent / 100m);

        return CheckoutResult.Ok(new
        {
            code = coupon.Code,
            discountPercent = coupon.DiscountPercent,
            subtotalAmount,
            eligibleAmount,
            discountAmount,
            totalAfterDiscount = Math.Max(0, subtotalAmount - discountAmount)
        });
    }

    public async Task<CheckoutResult> CheckoutAsync(long profileId, long addressId, string paymentMethod, string? requestedCouponCode, CancellationToken ct)
    {
        if (addressId < 1 ||
            paymentMethod != "zarinpal") return CheckoutResult.BadRequest(new { code = "invalid-checkout" });

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var storedCartRows = await db.AccountCarts
            .AsNoTracking()
            .Join(
                db.AccountCartItems
                    .AsNoTracking(),
                cart => cart.Id,
                item => item.IdCart,
                (cart, item) => new { cart, item })
            .Join(
                db.SalesPlanReferences
                    .AsNoTracking(),
                cartItem => cartItem.item.IdSalesPlan,
                plan => plan.Id,
                (cartItem, plan) => new { cartItem.cart, cartItem.item, plan })
            .Where(cartItemPlan => cartItemPlan.cart.IdProfile == profileId)
            .OrderBy(cartItemPlan => cartItemPlan.item.Id)
            .Select(cartItemPlan => new
            {
                cartItemPlan.item.Id,
                cartItemPlan.item.IdProduct,
                cartItemPlan.item.IdVariant,
                cartItemPlan.item.Quantity,
                SalesPlanId = cartItemPlan.plan.StoreSalesPlanKey
            })
            .ToListAsync(ct);
        if (storedCartRows.Count is < 1 or > 99)
            return CheckoutResult.Conflict(new { code = "account-cart-empty-or-invalid" });

        decimal[] storedCartItemIds = storedCartRows.Select(item => item.Id).ToArray();
        var storedAccessoryRows = await db.AccountCartItemAccessories
            .AsNoTracking()
            .Join(
                db.StoreProductAccessories
                    .AsNoTracking(),
                selected => selected.IdProductAccessory,
                link => link.Id,
                (selected, link) => new { selected, link })
            .Join(
                db.StoreAccessories
                    .AsNoTracking(),
                selectedLink => selectedLink.link.IdAccessory,
                accessory => accessory.Id,
                (selectedLink, accessory) => new { selectedLink.selected, accessory })
            .Where(selectedAccessory => storedCartItemIds.Contains(selectedAccessory.selected.IdCartItem))
            .Select(selectedAccessory => new
            {
                selectedAccessory.selected.IdCartItem,
                AccessoryId = selectedAccessory.accessory.Id
            })
            .ToListAsync(ct);
        var cartLines = storedCartRows.Select(item =>
        {
            var selections = new
            {
                accessories = storedAccessoryRows.Where(accessory => accessory.IdCartItem == item.Id)
                    .Select(accessory => new
                    {
                        id = accessory.AccessoryId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        selected = true
                    }).ToArray()
            };
            JsonElement options = JsonSerializer.SerializeToElement(selections, AccountStateJsonOptions);

            return new CheckoutLine(item.IdProduct, item.Quantity, item.SalesPlanId, options, item.IdVariant);
        }).ToList();
        if (cartLines.Any(item => item.ProductId is < 1 or > int.MaxValue || item.VariantId is < 1 ||
                                  item.Quantity is < 1 or > 99 || string.IsNullOrWhiteSpace(item.SalesPlanId)))
            return CheckoutResult.Conflict(new { code = "invalid-account-cart" });

        var checkoutOptions = new List<CheckoutOptions>(cartLines.Count);
        foreach (var line in cartLines)
        {
            if (!CheckoutOptions.TryRead(line.Options, out var parsed))
                return CheckoutResult.Conflict(new { code = "invalid-account-cart-options" });

            checkoutOptions.Add(parsed);
        }

        var address = await db.AccountAddresses
            .AsNoTracking()
            .Join(
                db.GeoCountries
                    .AsNoTracking(),
                addressRow => addressRow.IdCountry,
                country => country.Id,
                (addressRow, country) => new { addressRow, country })
            .Join(
                db.GeoProvinces
                    .AsNoTracking(),
                addressCountry => new { addressCountry.addressRow.IdProvince, addressCountry.addressRow.IdCountry },
                province => new { IdProvince = province.Id, province.IdCountry },
                (addressCountry, province) => new { addressCountry.addressRow, addressCountry.country, province })
            .Join(
                db.GeoCities
                    .AsNoTracking(),
                addressCountryProvince => new
                {
                    addressCountryProvince.addressRow.IdCity,
                    addressCountryProvince.addressRow.IdProvince,
                    addressCountryProvince.addressRow.IdCountry
                },
                city => new { IdCity = city.Id, city.IdProvince, city.IdCountry },
                (addressCountryProvince, city) => new
                {
                    addressCountryProvince.addressRow,
                    addressCountryProvince.country,
                    addressCountryProvince.province,
                    city
                })
            .Where(addressDetails =>
                addressDetails.addressRow.Id == addressId && addressDetails.addressRow.IdProfile == profileId)
            .Select(addressDetails => new
            {
                addressDetails.addressRow.Title,
                addressDetails.addressRow.Recipient,
                addressDetails.addressRow.Phone,
                country = addressDetails.country.NameFa,
                province = addressDetails.province.NameFa,
                city = addressDetails.city.NameFa,
                addressDetails.addressRow.Street,
                postal = addressDetails.addressRow.PostalCode
            })
            .SingleOrDefaultAsync(ct);
        if (address is null) return CheckoutResult.BadRequest(new { code = "address-not-owned" });

        var catalog = await plans.ReadAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var priced =
            new List<(CheckoutLine Line, decimal UnitAmount, decimal IdPriceVersion,
                List<(decimal IdAccessory, decimal IdPriceVersion, decimal Amount)> Accessories, bool CouponEligible)>();
        try
        {
            foreach (var (line, parsedOptions) in cartLines.Zip(checkoutOptions))
            {
                if (line.VariantId.HasValue &&
                    !await ProductVariantExistsAsync(line.ProductId, line.VariantId.Value, ct))
                    return CheckoutResult.Conflict(new { code = "product-variant-unavailable" });

                var quote = catalog.Quote(checked((int)line.ProductId), line.SalesPlanId, line.Quantity, now);
                ProductInventoryVariant[] warehouseVariants = catalog.InventoryVariants
                    .Where(variant => variant.ProductId == line.ProductId).ToArray();
                if (warehouseVariants.Length > 0)
                {
                    ProductInventoryVariant? selectedWarehouseVariant = line.VariantId is { } inventoryVariantId
                        ? warehouseVariants.SingleOrDefault(variant => variant.VariantId == inventoryVariantId)
                        : null;
                    if (selectedWarehouseVariant is null ||
                        !selectedWarehouseVariant.StockBySalesPlan.TryGetValue(line.SalesPlanId,
                            out decimal variantStock) ||
                        variantStock < line.Quantity)
                        return CheckoutResult.Conflict(new { code = "inventory-variant-or-stock-changed" });
                }
                else if (line.VariantId.HasValue)
                    return CheckoutResult.Conflict(new { code = "product-variant-unavailable" });

                string? productName = await db.Products
                    .AsNoTracking()
                    .Where(x => x.Id == line.ProductId && x.IsStoreEnabled)
                    .Select(x => x.NameFa)
                    .SingleOrDefaultAsync(ct);
                if (productName is null) return CheckoutResult.Conflict(new { code = "product-unavailable" });

                ProductPurchaseConfiguration? configuration =
                    await purchaseConfigurations.ReadProductPurchaseConfigurationAsync(checked((int)line.ProductId), line.SalesPlanId, ct);
                if (configuration is null) return CheckoutResult.Conflict(new { code = "product-options-unavailable" });

                if (line.Quantity > configuration.MaxPurchaseQuantity) return CheckoutResult.Conflict(new { code = "purchase-limit" });

                var accessoryIds = parsedOptions.AccessoryIds;

                var offer = catalog.Offers.Single(candidate =>
                    candidate.ProductId == quote.ProductId && candidate.SalesPlanId == quote.SalesPlanId);
                if (configuration.Accessories.Any(a => a.IsRequired && !accessoryIds.Contains(a.Key)) ||
                    accessoryIds.Any(id => configuration.Accessories.All(a => a.Key != id)) ||
                    offer.AccessoryIds is { } allowed &&
                    accessoryIds.Any(id => !allowed.Contains(id, StringComparer.Ordinal)))
                    return CheckoutResult.Conflict(new { code = "invalid-product-options" });

                decimal unitAmount = quote.UnitAmount;
                var selectedAccessories = new List<(decimal IdAccessory, decimal IdPriceVersion, decimal Amount)>();
                ProductInventoryVariant? selectedInventoryVariant = catalog.InventoryVariants.SingleOrDefault(variant =>
                    variant.ProductId == line.ProductId && variant.VariantId == line.VariantId);
                if (selectedInventoryVariant is not null)
                {
                    if (!selectedInventoryVariant.PriceBySalesPlan.TryGetValue(line.SalesPlanId,
                            out decimal variantPrice))
                        return CheckoutResult.Conflict(new { code = "variant-price-unavailable" });
                    decimal variantDiscount =
                        selectedInventoryVariant.DiscountBySalesPlan.GetValueOrDefault(line.SalesPlanId);
                    if (!selectedInventoryVariant.FinalPriceBySalesPlan.TryGetValue(line.SalesPlanId, out unitAmount))
                        return CheckoutResult.Conflict(new { code = "variant-price-unavailable" });
                }

                bool hasVariantPrice = selectedInventoryVariant?.PriceBySalesPlan.ContainsKey(line.SalesPlanId) == true;
                decimal effectiveDiscount = hasVariantPrice
                    ? selectedInventoryVariant!.DiscountBySalesPlan.GetValueOrDefault(line.SalesPlanId)
                    : quote.DiscountPercent;
                bool couponEligible = line.SalesPlanId == "regular" && effectiveDiscount <= 0;
                decimal basePriceVersionId = selectedInventoryVariant is null
                    ? offer.PriceVersionId
                    : selectedInventoryVariant.PriceVersionIdsBySalesPlan.GetValueOrDefault(line.SalesPlanId);
                if (basePriceVersionId <= 0) return CheckoutResult.Conflict(new { code = "price-or-stock-changed" });

                foreach (var accessory in configuration.Accessories)
                    if (accessory.IsRequired || accessoryIds.Contains(accessory.Key))
                    {
                        if (!offer.AccessoryPriceAmounts.TryGetValue(accessory.Key, out var amount))
                            return CheckoutResult.Conflict(new { code = "invalid-product-options" });

                        unitAmount += amount;
                        if (!offer.AccessoryPriceVersionIds.TryGetValue(accessory.Key, out decimal accessoryPriceId) ||
                            accessoryPriceId <= 0)
                            return CheckoutResult.Conflict(new { code = "price-or-stock-changed" });
                        if (!long.TryParse(accessory.Key, out long accessoryId))
                            return CheckoutResult.Conflict(new { code = "invalid-product-options" });
                        selectedAccessories.Add((accessoryId, accessoryPriceId, amount));
                    }

                if (unitAmount < 0) return CheckoutResult.Conflict(new { code = "invalid-price" });
                priced.Add((line, unitAmount, basePriceVersionId, selectedAccessories, couponEligible));
            }
        }
        catch (SalesPlanUnavailableException)
        {
            return CheckoutResult.Conflict(new { code = "price-or-stock-changed" });
        }

        foreach (var group in priced.GroupBy(item => (item.Line.ProductId, item.Line.SalesPlanId)))
        {
            string signature = string.Join(",", group.First().Accessories.Select(x => x.IdAccessory).Order());
            if (group.Any(item => string.Join(",", item.Accessories.Select(x => x.IdAccessory).Order()) != signature))
                return CheckoutResult.Conflict(new { code = "inconsistent-accessories-for-product-plan" });
        }

        decimal subtotal = priced.Sum(x => checked(x.UnitAmount * x.Line.Quantity));
        Data.Domain.Account.Entities.AccountCoupon? appliedCoupon = null;
        decimal couponDiscountAmount = 0;
        if (!string.IsNullOrWhiteSpace(requestedCouponCode))
        {
            string couponCode = NormalizeCouponCode(requestedCouponCode);
            if (couponCode.Length is < 1 or > 32 || couponCode.Any(character => !char.IsAsciiLetterOrDigit(character)))
                return CheckoutResult.BadRequest(new { code = "invalid-coupon" });

            appliedCoupon = await db.AccountCoupons
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == couponCode, ct);
            if (appliedCoupon is null || !appliedCoupon.Enabled ||
                (appliedCoupon.IdProfile.HasValue && appliedCoupon.IdProfile != profileId) ||
                appliedCoupon.ExpiresOn < DateOnly.FromDateTime(DateTime.UtcNow))
                return CheckoutResult.Conflict(new { code = "coupon-unavailable" });

            int couponUses = await db.StoreCouponUsages
                .AsNoTracking()
                .CountAsync(item => item.IdCoupon == appliedCoupon.Id, ct);
            if (couponUses >= appliedCoupon.UsageLimit)
                return CheckoutResult.Conflict(new { code = "coupon-limit-reached" });

            decimal eligibleAmount = priced.Where(item => item.CouponEligible)
                .Sum(item => checked(item.UnitAmount * item.Line.Quantity));
            if (eligibleAmount <= 0) return CheckoutResult.Conflict(new { code = "coupon-no-eligible-items" });

            couponDiscountAmount = decimal.Floor(eligibleAmount * appliedCoupon.DiscountPercent / 100m);
        }

        decimal total = subtotal - couponDiscountAmount;
        if (total > 9999999999999999999m)
            return CheckoutResult.Conflict(new { code = "order-amount-out-of-range" });

        int count = priced.Sum(x => x.Line.Quantity);
        string[] checkoutPlanKeys = priced.Select(x => x.Line.SalesPlanId).Distinct(StringComparer.Ordinal).ToArray();
        var checkoutPlanIds = await db.SalesPlanReferences
            .AsNoTracking()
            .Where(x => checkoutPlanKeys.Contains(x.StoreSalesPlanKey) && x.IdStockOwnerType == 1 && x.IsActive)
            .ToDictionaryAsync(x => x.StoreSalesPlanKey, x => x.Id, ct);
        if (checkoutPlanIds.Count != checkoutPlanKeys.Length)
            return CheckoutResult.Conflict(new { code = "price-or-stock-changed" });

        var paymentBank = await db.StoreBanks
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == paymentMethod && x.IsActive, ct);
        if (paymentBank is null || paymentBank.Code != "zarinpal")
            return CheckoutResult.Conflict(new { code = "payment-method-unavailable" });

        int pendingPaymentStatusId = await db.OrderStatuses
            .AsNoTracking()
            .Where(x => x.NameEn == StoreOrderStatusNames.PendingPayment)
            .Select(x => x.Id)
            .SingleAsync(ct);
        var order = new StoreCustomerOrderReference
        {
            IdProfile = profileId,
            IdAddress = addressId,
            IdStatus = pendingPaymentStatusId,
            IdBank = paymentBank.Id,
            TotalAmount = total,
            SubtotalAmount = subtotal,
            IdCoupon = appliedCoupon?.Id,
            CouponDiscountAmount = couponDiscountAmount,
            CreatedAtUtc = DateTime.UtcNow,
            Items = priced.Select(item => new StoreCustomerOrderItem
            {
                IdProduct = item.Line.ProductId,
                IdVariant = item.Line.VariantId,
                IdSalesPlan = checkoutPlanIds[item.Line.SalesPlanId],
                Quantity = item.Line.Quantity,
                IdPriceVersion = item.IdPriceVersion,
            }).ToList()
        };
        db.StoreCustomerOrders.Add(order);
        await db.AccountCartItems
            .Where(item => db.AccountCarts
                .AsNoTracking()
                .Any(cart => cart.Id == item.IdCart && cart.IdProfile == profileId))
            .ExecuteDeleteAsync(ct);
        await db.SaveChangesAsync(ct);

        var orderAccessories = priced.SelectMany(item => item.Accessories.Select(accessory => new
            {
                item.Line.ProductId,
                IdSalesPlan = checkoutPlanIds[item.Line.SalesPlanId],
                accessory.IdAccessory,
                accessory.IdPriceVersion,
                accessory.Amount
            }))
            .GroupBy(item => (item.ProductId, item.IdSalesPlan, item.IdAccessory))
            .Select(group => new StoreOrderAccessory
            {
                IdOrder = order.Id,
                IdProduct = group.Key.ProductId,
                IdSalesPlan = group.Key.IdSalesPlan,
                IdAccessory = group.Key.IdAccessory,
                IdPriceVersion = group.First().IdPriceVersion
            }).ToArray();
        if (orderAccessories.Length > 0)
        {
            db.StoreOrderAccessories.AddRange(orderAccessories);
            await db.SaveChangesAsync(ct);
        }

        if (appliedCoupon is not null)
        {
            db.StoreCouponUsages.Add(new StoreCouponUsage { IdCoupon = appliedCoupon.Id, IdCustomerOrder = order.Id });
            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);

        return CheckoutResult.Ok(new
        {
            id = order.Id,
            number = order.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            status = "pending-payment",
            itemCount = count,
            totalAmount = total,
            subtotalAmount = subtotal,
            couponCode = appliedCoupon?.Code,
            couponDiscountAmount,
            createdAtUtc = new DateTimeOffset(DateTime.SpecifyKind(order.CreatedAtUtc, DateTimeKind.Utc)),
            message = "سفارش ثبت شد و در انتظار پرداخت است."
        });
    }
}
