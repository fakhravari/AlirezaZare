using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;
using Shop.Services;
using Shop.Models;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Store.Entities;

namespace Shop.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "ShopCookie")]
[Route("api/account")]
public sealed class AccountController(
    ShopDbContext db,
    ISalesPlanSource plans,
    IProductPurchaseConfigurationSource purchaseConfigurations,
    IWebHostEnvironment environment,
    IPasswordHasher<string> passwordHasher) : ControllerBase
{

    private long ProfileId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet("state")]
    public async Task<IActionResult> State(CancellationToken ct)
    {
        var profile = await db.AccountProfiles.AsNoTracking()
            .Where(x => x.Id == ProfileId)
            .Select(x => new
            {
                x.Id, x.Email, x.FirstName, x.LastName, x.Phone, x.IdGender, x.NewsletterEnabled, x.Password
            })
            .SingleOrDefaultAsync(ct);
        if (profile is null) return NotFound();

        var cartRows = await (
            from cart in db.AccountCarts.AsNoTracking()
            join item in db.AccountCartItems.AsNoTracking() on cart.Id equals item.IdCart
            join plan in db.SalesPlanReferences.AsNoTracking() on item.IdSalesPlan equals plan.Id
            where cart.IdProfile == ProfileId
            select new { item.Id, item.IdProduct, item.IdProductVariant, item.Quantity, plan.StoreSalesPlanKey })
            .ToListAsync(ct);
        long[] cartItemIds = cartRows.Select(item => item.Id).ToArray();
        var cartOptionRows = cartItemIds.Length == 0 ? [] : await (
            from selected in db.AccountCartItemPurchaseOptionValues.AsNoTracking()
            join value in db.StoreProductPurchaseOptionValues.AsNoTracking() on selected.IdProductPurchaseOptionValue equals value.Id
            join option in db.StoreProductPurchaseOptions.AsNoTracking() on value.IdProductPurchaseOption equals option.Id
            where cartItemIds.Contains(selected.IdCartItem)
            select new { selected.IdCartItem, value.Id, option.OptionKey, OptionLabel = option.Label,
                ValueKey = value.ValueKey, ValueLabel = value.Label, value.PriceDelta }).ToListAsync(ct);
        var cartAccessoryRows = cartItemIds.Length == 0 ? [] : await (
            from selected in db.AccountCartItemAccessories.AsNoTracking()
            join link in db.StoreProductAccessories.AsNoTracking() on selected.IdProductAccessory equals link.Id
            join accessory in db.StoreAccessories.AsNoTracking() on link.IdAccessory equals accessory.Id
            where cartItemIds.Contains(selected.IdCartItem)
            select new { selected.IdCartItem, ProductAccessoryId = link.Id, accessory.AccessoryKey,
                accessory.Name, link.IsRequired, link.PriceAmount }).ToListAsync(ct);
        var cartVariantRows = cartRows.Where(item => item.IdProductVariant.HasValue).Select(item => item.IdProductVariant!.Value).Distinct().ToArray();
        var cartVariants = cartVariantRows.Length == 0 ? [] : await db.ProductVariants.AsNoTracking()
            .Where(variant => cartVariantRows.Contains(variant.Id))
            .Select(variant => new { variant.Id, variant.Name, variant.AttributesJson }).ToListAsync(ct);

        var addresses = await (
            from address in db.AccountAddresses.AsNoTracking()
            join country in db.GeoCountries.AsNoTracking() on address.IdCountry equals country.Id
            join province in db.GeoProvinces.AsNoTracking() on new { address.IdProvince, address.IdCountry }
                equals new { IdProvince = province.Id, province.IdCountry }
            join city in db.GeoCities.AsNoTracking() on new { address.IdCity, address.IdProvince, address.IdCountry }
                equals new { IdCity = city.Id, city.IdProvince, city.IdCountry }
            where address.IdProfile == ProfileId
            select new
            {
                address.Id, address.Title, address.Recipient, address.Phone, country.NameFa,
                address.IdCountry, ProvinceName = province.NameFa, address.IdProvince,
                CityName = city.NameFa, address.IdCity, address.Street, address.PostalCode, address.IsDefault
            })
            .ToListAsync(ct);

        var favorites = await db.AccountFavorites.AsNoTracking().Where(x => x.IdProfile == ProfileId)
            .Select(x => x.IdProduct).ToListAsync(ct);
        var compare = await db.AccountCompareItems.AsNoTracking().Where(x => x.IdProfile == ProfileId)
            .Select(x => x.IdProduct).ToListAsync(ct);

        var alertRows = await db.AccountPriceAlerts.AsNoTracking().Where(x => x.IdProfile == ProfileId)
            .Select(x => new
            {
                x.IdProduct, x.Scope, x.VariantsJson, x.PriceAtRegistration, x.StockAtRegistration, x.CreatedAtUtc
            }).ToListAsync(ct);

        var interactions = await (
            from interaction in db.AccountArticleInteractions.AsNoTracking()
            join article in db.MagazineArticles.AsNoTracking() on interaction.IdMagazineArticle equals article.Id
            where interaction.IdProfile == ProfileId
            select new { slug = article.ArticleKey, type = interaction.InteractionType })
            .ToListAsync(ct);

        var feedback = await (
            from entry in db.AccountArticleFeedback.AsNoTracking()
            join article in db.MagazineArticles.AsNoTracking() on entry.IdMagazineArticle equals article.Id
            where entry.IdProfile == ProfileId
            select new
            {
                slug = article.ArticleKey, name = entry.DisplayName, text = entry.FeedbackText,
                entry.Rating, entry.Status, entry.CreatedAtUtc
            })
            .ToListAsync(ct);

        var checkout = await db.AccountCheckoutPreferences.AsNoTracking().Where(x => x.IdProfile == ProfileId)
            .Select(x => new { selectedAddressId = x.IdSelectedAddress, paymentMethod = x.PaymentMethod })
            .SingleOrDefaultAsync(ct);

        var orderRows = await db.StoreCustomerOrders.AsNoTracking().Where(x => x.IdProfile == ProfileId)
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id, x.OrderNumber, x.Status, x.ShippingCarrier, ShippingTrackingCode = x.ShippingTrackingCode,
                x.PaymentMethod, x.ExternalOrderKey, x.OrderedAtUtc, x.ItemCount, x.TotalAmount,
                x.SubtotalAmount, x.IdCoupon, x.CouponDiscountAmount
            })
            .ToListAsync(ct);
        long[] usedCouponIds = orderRows.Where(order => order.IdCoupon.HasValue)
            .Select(order => order.IdCoupon!.Value).Distinct().ToArray();
        var couponCodesById = usedCouponIds.Length == 0
            ? new Dictionary<long, string>()
            : await db.AccountCoupons.AsNoTracking().Where(coupon => usedCouponIds.Contains(coupon.Id))
                .ToDictionaryAsync(coupon => coupon.Id, coupon => coupon.Code, ct);
        var orderIds = orderRows.Select(x => x.Id).ToArray();
        var orderItemRows = orderIds.Length == 0
            ? []
            : await (
                from item in db.StoreCustomerOrderItems.AsNoTracking()
                join plan in db.SalesPlanReferences.AsNoTracking() on item.IdSalesPlan equals plan.Id
                where orderIds.Contains(item.IdCustomerOrder)
                select new
                {
                    item.Id, item.IdCustomerOrder, item.IdProduct, item.IdProductVariant, item.ProductNameSnapshot,
                    plan.StoreSalesPlanKey, item.Quantity, item.UnitAmount, item.OptionsJson
                })
                .ToListAsync(ct);
        var orderItemIds = orderItemRows.Select(x => x.Id).ToArray();
        var orderPriceReferences = orderItemIds.Length == 0 ? [] : await (from reference in db.StoreOrderItemPriceReferences.AsNoTracking()
            join version in db.StorePriceVersions.AsNoTracking() on reference.IdPriceVersion equals version.Id
            where orderItemIds.Contains(reference.IdOrderItem)
            select new { reference.IdOrderItem, reference.IdPriceVersion, version.PriceKind, version.PriceAmount, version.DiscountPercent })
            .ToListAsync(ct);
        var orderItemsById = orderItemRows.GroupBy(x => x.IdCustomerOrder)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var shippingHistory = orderIds.Length == 0 ? [] : await db.StoreOrderShippingHistories.AsNoTracking()
            .Where(x => orderIds.Contains(x.IdOrder)).OrderBy(x => x.Id).Select(x => new { x.IdOrder, x.PreviousStatus, x.NewStatus, x.ChangedAtUtc }).ToListAsync(ct);
        var trackingHistory = orderIds.Length == 0 ? [] : await db.StoreOrderShipmentTrackingHistories.AsNoTracking()
            .Where(x => orderIds.Contains(x.IdOrder)).OrderBy(x => x.Id).Select(x => new { x.IdOrder, x.Carrier, x.TrackingCode, x.ChangedAtUtc }).ToListAsync(ct);

        var state = new Dictionary<string, object?>
        {
            ["id"] = profile.Id,
            ["email"] = profile.Email,
            ["firstName"] = profile.FirstName,
            ["lastName"] = profile.LastName,
            ["phone"] = profile.Phone,
            ["idGender"] = profile.IdGender,
            ["newsletter"] = profile.NewsletterEnabled,
            ["hasPassword"] = profile.Password is not null,
            ["simulationAvailable"] = environment.IsDevelopment(),
            ["cart"] = cartRows.Select(item => new
            {
                id = item.IdProduct,
                variantId = item.IdProductVariant,
                item.Quantity,
                salesPlanId = item.StoreSalesPlanKey,
                options = new
                {
                    inventoryVariant = item.IdProductVariant is { } variantId && cartVariants.FirstOrDefault(variant => variant.Id == variantId) is { } variant
                        ? new { id = variant.Id, name = variant.Name, attributes = ParseJson(variant.AttributesJson) }
                        : null,
                    priceOptions = cartOptionRows.Where(option => option.IdCartItem == item.Id).Select(option => new
                    {
                        id = option.Id, key = option.OptionKey, label = option.OptionLabel,
                        value = option.ValueLabel, productValue = option.ValueKey, priceDelta = option.PriceDelta
                    }).ToArray(),
                    accessories = cartAccessoryRows.Where(accessory => accessory.IdCartItem == item.Id).Select(accessory => new
                    {
                        id = accessory.AccessoryKey, productAccessoryId = accessory.ProductAccessoryId,
                        name = accessory.Name, required = accessory.IsRequired, selected = true,
                        price = accessory.PriceAmount / 1_000_000m
                    }).ToArray()
                }
            }).ToArray(),
            ["addresses"] = addresses.Select(address => new
            {
                id = address.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                address.Title,
                address.Recipient,
                address.Phone,
                country = address.NameFa,
                countryId = address.IdCountry,
                province = address.ProvinceName,
                provinceId = address.IdProvince,
                city = address.CityName,
                cityId = address.IdCity,
                address.Street,
                postal = address.PostalCode,
                address.IsDefault
            }).ToArray(),
            ["favorites"] = favorites.Select(id => new { id }).ToArray(),
            ["compare"] = compare.Select(id => new { id }).ToArray(),
            ["priceAlerts"] = alertRows.Select(alert => new
            {
                productId = alert.IdProduct,
                alert.Scope,
                variants = ParseJson(alert.VariantsJson),
                alert.PriceAtRegistration,
                alert.StockAtRegistration,
                createdAt = AsUtc(alert.CreatedAtUtc)
            }).ToArray(),
            ["articleInteractions"] = interactions,
            ["articleFeedback"] = feedback.Select(entry => new
            {
                entry.slug,
                entry.name,
                entry.text,
                entry.Rating,
                entry.Status,
                createdAt = AsUtc(entry.CreatedAtUtc)
            }).ToArray(),
            ["checkout"] = checkout,
            ["orders"] = orderRows.Select(order => new
            {
                id = order.Id,
                number = order.OrderNumber,
                order.Status,
                order.ShippingCarrier,
                shippingTrackingCode = order.ShippingTrackingCode,
                isSample = order.PaymentMethod == "demo" && order.ExternalOrderKey?.StartsWith(
                    "demo-order-", StringComparison.OrdinalIgnoreCase) == true,
                orderedAtUtc = AsUtc(order.OrderedAtUtc),
                order.ItemCount,
                order.TotalAmount,
                order.SubtotalAmount,
                couponCode = order.IdCoupon is { } couponId ? couponCodesById.GetValueOrDefault(couponId) : null,
                order.CouponDiscountAmount,
                items = orderItemsById.GetValueOrDefault(order.Id, []).Select(item => new
                {
                    productId = item.IdProduct,
                    variantId = item.IdProductVariant,
                    name = item.ProductNameSnapshot,
                    salesPlanId = item.StoreSalesPlanKey,
                    item.Quantity,
                    item.UnitAmount,
                    options = ParseJson(item.OptionsJson),
                    priceReferences = orderPriceReferences.Where(reference => reference.IdOrderItem == item.Id).Select(reference => new
                    {
                        priceVersionId = reference.IdPriceVersion,
                        component = reference.PriceKind,
                        calculatedUnitAmount = reference.PriceKind is "option" or "accessory" or "order-snapshot" ? reference.PriceAmount : decimal.Round(reference.PriceAmount * (100m - reference.DiscountPercent) / 100m, 0, MidpointRounding.AwayFromZero)
                    }).ToArray()
                }).ToArray(),
                shippingHistory = shippingHistory.Where(entry => entry.IdOrder == order.Id).Select(entry => new { entry.PreviousStatus, entry.NewStatus, changedAtUtc = AsUtc(entry.ChangedAtUtc) }).ToArray(),
                trackingHistory = trackingHistory.Where(entry => entry.IdOrder == order.Id).Select(entry => new { entry.Carrier, entry.TrackingCode, changedAtUtc = AsUtc(entry.ChangedAtUtc) }).ToArray()
            }).ToArray()
        };

        string json = JsonSerializer.Serialize(state, AccountStateJsonOptions);
        return Content(json, "application/json; charset=utf-8");
    }

    [HttpGet("messages")]
    public async Task<IActionResult> Messages(CancellationToken ct)
    {
        var items = await db.AccountMessages.AsNoTracking().Where(x => x.IdProfile == ProfileId)
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Select(x => new { x.Id, x.Title, x.Category, x.Body, unread = !x.IsRead, x.CreatedAtUtc })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPut("messages/{id:long}/read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkMessageRead(long id, CancellationToken ct)
    {
        var item = await db.AccountMessages.SingleOrDefaultAsync(x => x.Id == id && x.IdProfile == ProfileId, ct);
        if (item is null) return NotFound();
        if (!item.IsRead)
        {
            item.IsRead = true;
            item.ReadAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return Ok(new { message = "پیام خوانده‌شده علامت خورد." });
    }

    private static readonly JsonSerializerOptions AccountStateJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static JsonElement? ParseJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    [HttpGet("recent-searches")]
    public async Task<IActionResult> RecentSearches(CancellationToken ct)
    {
        var items = await db.AccountRecentSearches.AsNoTracking()
            .Where(x => x.IdProfile == ProfileId)
            .OrderByDescending(x => x.SearchedAtUtc).ThenByDescending(x => x.Id)
            .Select(x => x.SearchText).Take(6).ToListAsync(ct);
        return Ok(new { items });
    }

    [HttpPost("recent-searches")]
    public async Task<IActionResult> AddRecentSearch([FromBody] RecentSearchRequest request, CancellationToken ct)
    {
        string searchText = (request.Query ?? string.Empty).Trim();
        if (searchText.Length is < 3 or > 80) return BadRequest(new { code = "invalid-search-query" });
        string searchKey = searchText.ToLowerInvariant();
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var existing = await db.AccountRecentSearches
            .SingleOrDefaultAsync(x => x.IdProfile == ProfileId && x.SearchKey == searchKey, ct);
        if (existing is null)
        {
            db.AccountRecentSearches.Add(new AccountRecentSearch
            {
                IdProfile = ProfileId,
                SearchKey = searchKey,
                SearchText = searchText,
                SearchedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.SearchText = searchText;
            existing.SearchedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        var keep = await db.AccountRecentSearches.Where(x => x.IdProfile == ProfileId)
            .OrderByDescending(x => x.SearchedAtUtc).ThenByDescending(x => x.Id)
            .Select(x => x.Id).Take(6).ToListAsync(ct);
        await db.AccountRecentSearches.Where(x => x.IdProfile == ProfileId && !keep.Contains(x.Id))
            .ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { saved = true });
    }

    [HttpDelete("recent-searches")]
    public async Task<IActionResult> ClearRecentSearches(CancellationToken ct)
    {
        await db.AccountRecentSearches.Where(x => x.IdProfile == ProfileId).ExecuteDeleteAsync(ct);
        return Ok(new { deleted = true });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(ProfileRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
            request.FirstName.Length > 100 || request.LastName.Length > 100 || (request.Phone?.Length ?? 0) > 30 ||
            request.IdGender is <= 0) return BadRequest(new { code = "invalid-profile" });
        if (request.IdGender.HasValue && !await db.AccountGenders.AnyAsync(x => x.Id == request.IdGender.Value, ct))
            return BadRequest(new { code = "invalid-profile" });
        var profile = await db.AccountProfiles.SingleOrDefaultAsync(x => x.Id == ProfileId, ct);
        if (profile is null) return Unauthorized();
        profile.FirstName = request.FirstName.Trim();
        profile.LastName = request.LastName.Trim();
        profile.Phone = request.Phone;
        profile.IdGender = request.IdGender;
        profile.NewsletterEnabled = request.Newsletter;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { saved = true });
    }

    [HttpPut("password")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("account-password")]
    public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.NewPassword) ||
            !string.Equals(request.NewPassword, request.NewPasswordConfirmation, StringComparison.Ordinal))
            return BadRequest(new { code = "invalid-new-password", message = "رمز جدید را وارد کنید و تکرار آن را مطابق بنویسید." });

        string email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var profile = await db.AccountProfiles.SingleOrDefaultAsync(x => x.Id == ProfileId, ct);
        if (string.IsNullOrWhiteSpace(email) || profile is null) return Unauthorized();
        string? currentPassword = profile.Password;
        if (currentPassword is not null &&
            (string.IsNullOrEmpty(request.CurrentPassword) ||
             (currentPassword.StartsWith("AQAAAA", StringComparison.Ordinal)
                 ? passwordHasher.VerifyHashedPassword(email, currentPassword, request.CurrentPassword) == PasswordVerificationResult.Failed
                 : currentPassword != request.CurrentPassword)))
            return BadRequest(new { code = "invalid-current-password", message = "رمز فعلی درست نیست." });
        if (currentPassword is null && !string.IsNullOrEmpty(request.CurrentPassword))
            return BadRequest(new { code = "invalid-current-password", message = "این حساب هنوز رمز عبور ندارد." });

        profile.Password = request.NewPassword;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { changed = true });
    }

    [HttpPut("cart")]
    public async Task<IActionResult> SaveCart([FromBody] CartRequest request, CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count > 99 || request.Items.Any(i =>
                i is null || i.ProductId is < 1 or > int.MaxValue || i.VariantId is < 1 || i.Quantity is < 1 or > 99 ||
                string.IsNullOrWhiteSpace(i.SalesPlanId) || i.SalesPlanId.Length > 64))
            return BadRequest(new { code = "invalid-cart" });
        var duplicate = request.Items.GroupBy(i => (i.ProductId, i.SalesPlanId, i.VariantId)).Any(g => g.Count() > 1);
        if (duplicate) return BadRequest(new { code = "duplicate-cart-line" });
        SalesPlanCatalog currentCatalog = await plans.ReadAsync(ct);
        foreach (var item in request.Items)
        {
            try
            {
                // Recheck the offer's current plan, quantity limit, and product-level stock
                // before accepting a cart snapshot. Variant stock is checked separately below.
                currentCatalog.Quote(checked((int)item.ProductId), item.SalesPlanId,
                    item.Quantity, DateTimeOffset.UtcNow);
            }
            catch (SalesPlanUnavailableException exception)
            {
                return Conflict(new { code = exception.ReasonCode });
            }

            ProductInventoryVariant[] variants = currentCatalog.InventoryVariants
                .Where(variant => variant.ProductId == item.ProductId).ToArray();
            if (variants.Length > 0 && (!item.VariantId.HasValue ||
                                        variants.All(variant => variant.VariantId != item.VariantId.Value)))
                return Conflict(new { code = "inventory-variant-required" });
            if (variants.Length == 0 && item.VariantId.HasValue || item.VariantId.HasValue &&
                !await ProductVariantExistsAsync(item.ProductId, item.VariantId.Value, ct))
                return Conflict(new { code = "product-variant-unavailable" });
            if (item.VariantId is { } selectedVariantId &&
                (!variants.Single(variant => variant.VariantId == selectedVariantId).StockBySalesPlan
                    .TryGetValue(item.SalesPlanId, out decimal stock) || stock < item.Quantity))
                return Conflict(new { code = "insufficient-stock" });

            long[] optionValueIds = item.OptionValueIds?.ToArray() ?? [];
            if (optionValueIds.Distinct().Count() != optionValueIds.Length || optionValueIds.Any(id => id < 1))
                return BadRequest(new { code = "invalid-product-options" });
            ProductPurchaseConfiguration? configuration = await purchaseConfigurations.ReadAsync(checked((int)item.ProductId), ct);
            if (configuration is null || configuration.Options.Any(option =>
                    option.Values.Count(value => optionValueIds.Contains(value.Id)) != 1) ||
                optionValueIds.Length != configuration.Options.Count)
                return Conflict(new { code = "invalid-product-options" });

            long[] accessoryIds = item.AccessoryIds?.ToArray() ?? [];
            if (accessoryIds.Distinct().Count() != accessoryIds.Length || accessoryIds.Any(id => id < 1))
                return BadRequest(new { code = "invalid-product-options" });
            var productAccessories = await (from accessory in db.StoreProductAccessories.AsNoTracking()
                join definition in db.StoreAccessories.AsNoTracking() on accessory.IdAccessory equals definition.Id
                where accessory.IdProduct == item.ProductId && definition.IsActive
                select new { accessory.Id, accessory.IsRequired, definition.AccessoryKey }).ToListAsync(ct);
            var selectedAccessories = productAccessories.Where(accessory => accessoryIds.Contains(accessory.Id)).ToArray();
            ProductSalesOffer? cartOffer = currentCatalog.Offers.SingleOrDefault(offer =>
                offer.ProductId == item.ProductId && offer.SalesPlanId == item.SalesPlanId);
            if (cartOffer is null || selectedAccessories.Length != accessoryIds.Length ||
                productAccessories.Any(accessory => accessory.IsRequired && !accessoryIds.Contains(accessory.Id)) ||
                selectedAccessories.Any(accessory => cartOffer.AccessoryIds is { } allowed &&
                    !allowed.Contains(accessory.AccessoryKey, StringComparer.Ordinal)) || item.Quantity > configuration.MaxPurchaseQuantity)
                return Conflict(new { code = "invalid-product-options" });

        }

        long[] productIds = request.Items.Select(x => x.ProductId).Distinct().ToArray();
        string[] planKeys = request.Items.Select(x => x.SalesPlanId).Distinct(StringComparer.Ordinal).ToArray();
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);
        var availableProducts = await db.Products.AsNoTracking()
            .Where(x => productIds.Contains(x.Id) && x.IsCatalogActive && x.IsStoreEnabled)
            .Select(x => x.Id).ToHashSetAsync(ct);
        var planIds = await db.SalesPlanReferences.AsNoTracking()
            .Where(x => planKeys.Contains(x.StoreSalesPlanKey) && x.IsActive && x.IdStockOwnerType == 1)
            .ToDictionaryAsync(x => x.StoreSalesPlanKey, x => x.Id, ct);
        if (availableProducts.Count != productIds.Length || planIds.Count != planKeys.Length)
            return Conflict(new { code = "product-or-plan-unavailable" });

        var cart = await db.AccountCarts.SingleOrDefaultAsync(x => x.IdProfile == ProfileId, ct);
        if (cart is null)
        {
            cart = new AccountCart { IdProfile = ProfileId, UpdatedAtUtc = DateTime.UtcNow };
            db.AccountCarts.Add(cart);
            await db.SaveChangesAsync(ct);
        }
        await db.AccountCartItems.Where(x => x.IdCart == cart.Id).ExecuteDeleteAsync(ct);
        DateTime now = DateTime.UtcNow;
        var cartItemEntities = request.Items.Select(item => new AccountCartItem
        {
            IdCart = cart.Id,
            IdProduct = item.ProductId,
            IdProductVariant = item.VariantId,
            IdSalesPlan = planIds[item.SalesPlanId],
            Quantity = item.Quantity,
            UpdatedAtUtc = now
        }).ToArray();
        db.AccountCartItems.AddRange(cartItemEntities);
        await db.SaveChangesAsync(ct);
        for (var index = 0; index < request.Items.Count; index++)
        {
            CartLineRequest line = request.Items[index];
            AccountCartItem savedItem = cartItemEntities[index];
            db.AccountCartItemPurchaseOptionValues.AddRange((line.OptionValueIds ?? []).Select(id =>
                new AccountCartItemPurchaseOptionValue { IdCartItem = savedItem.Id, IdProductPurchaseOptionValue = id }));
            db.AccountCartItemAccessories.AddRange((line.AccessoryIds ?? []).Select(id =>
                new AccountCartItemAccessory { IdCartItem = savedItem.Id, IdProductAccessory = id }));
        }
        cart.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { saved = true });
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress(AddressRequest a, CancellationToken ct)
    {
        if (new[]
            {
                a.Title, a.Recipient, a.Phone, a.Street, a.Postal
            }.Any(v => string.IsNullOrWhiteSpace(v)) ||
            a.Street!.Length > 500 || a.CountryId is null or < 1 || a.ProvinceId is null or < 1 || a.CityId is null or < 1)
            return BadRequest(new { code = "invalid-address" });
        if (!await IsAddressGeographyValidAsync(a.CountryId.Value, a.ProvinceId.Value, a.CityId.Value, ct))
            return BadRequest(new { code = "invalid-address-location" });
        var address = new AccountAddress
        {
            IdProfile = ProfileId,
            Title = a.Title!.Trim(),
            Recipient = a.Recipient!.Trim(),
            Phone = a.Phone!.Trim(),
            IdCountry = a.CountryId.Value,
            IdProvince = a.ProvinceId.Value,
            IdCity = a.CityId.Value,
            Street = a.Street!.Trim(),
            PostalCode = a.Postal!.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.AccountAddresses.Add(address);
        await db.SaveChangesAsync(ct);
        return Ok(new { saved = true, id = address.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) });
    }

    [HttpPut("checkout-preference")]
    public async Task<IActionResult> SaveCheckoutPreference([FromBody] CheckoutPreferenceRequest request,
        CancellationToken ct)
    {
        if (request.AddressId is null or < 1 || request.PaymentMethod != "zarinpal")
            return BadRequest(new { code = "invalid-checkout-preference" });
        if (!await db.AccountAddresses.AnyAsync(x => x.Id == request.AddressId && x.IdProfile == ProfileId, ct))
            return BadRequest(new { code = "address-not-owned" });
        await SetCheckoutPreferenceAsync(request.AddressId, request.PaymentMethod, ct);
        await db.SaveChangesAsync(ct);
        return Ok(new { saved = true });
    }

    [HttpPost("coupon/quote")]
    public async Task<IActionResult> QuoteCoupon([FromBody] CouponRequest request, CancellationToken ct)
    {
        string code = NormalizeCouponCode(request.Code);
        if (code.Length is < 1 or > 32 || code.Any(character => !char.IsAsciiLetterOrDigit(character)))
            return BadRequest(new { code = "invalid-coupon" });

        var coupon = await db.AccountCoupons.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Code == code, ct);
        if (coupon is null || !coupon.Enabled || coupon.ExpiresOn < DateOnly.FromDateTime(DateTime.UtcNow))
            return NotFound(new { code = "coupon-unavailable" });

        int uses = await db.StoreCouponUsages.CountAsync(item => item.IdCoupon == coupon.Id, ct);
        if (uses >= coupon.UsageLimit)
            return Conflict(new { code = "coupon-limit-reached" });

        var storedRows = await (
            from cart in db.AccountCarts.AsNoTracking()
            join item in db.AccountCartItems.AsNoTracking() on cart.Id equals item.IdCart
            join plan in db.SalesPlanReferences.AsNoTracking() on item.IdSalesPlan equals plan.Id
            where cart.IdProfile == ProfileId
            select new { item.Id, item.IdProduct, item.IdProductVariant, item.Quantity, SalesPlanId = plan.StoreSalesPlanKey }
        ).ToListAsync(ct);
        if (storedRows.Count == 0) return Conflict(new { code = "coupon-cart-empty" });

        var catalog = await plans.ReadAsync(ct);
        decimal eligibleAmount = 0;
        decimal subtotalAmount = 0;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (var row in storedRows)
        {
            SalesPlanQuote quote;
            try { quote = catalog.Quote(checked((int)row.IdProduct), row.SalesPlanId, row.Quantity, now); }
            catch (SalesPlanUnavailableException) { return Conflict(new { code = "price-or-stock-changed" }); }
            bool eligible = row.SalesPlanId == "regular" && quote.DiscountPercent <= 0;
            decimal unitAmount = quote.UnitAmount;
            ProductInventoryVariant[] productVariants = catalog.InventoryVariants
                .Where(variant => variant.ProductId == row.IdProduct).ToArray();
            ProductInventoryVariant? inventoryVariant = catalog.InventoryVariants.SingleOrDefault(variant =>
                variant.ProductId == row.IdProduct && variant.VariantId == row.IdProductVariant);
            if (productVariants.Length > 0)
            {
                if (inventoryVariant is null ||
                    !inventoryVariant.StockBySalesPlan.TryGetValue(row.SalesPlanId, out decimal variantStock) ||
                    variantStock < row.Quantity)
                    return Conflict(new { code = "inventory-variant-or-stock-changed" });
            }
            else if (row.IdProductVariant.HasValue)
                return Conflict(new { code = "product-variant-unavailable" });
            if (inventoryVariant is not null && inventoryVariant.PriceBySalesPlan.TryGetValue(row.SalesPlanId, out decimal variantPrice))
            {
                decimal variantDiscount = inventoryVariant.DiscountBySalesPlan.GetValueOrDefault(row.SalesPlanId);
                if (variantDiscount > 0) eligible = false;
                unitAmount = decimal.Round(variantPrice * (100m - variantDiscount) / 100m, 0,
                    MidpointRounding.AwayFromZero);
            }

            ProductPurchaseConfiguration? configuration =
                await purchaseConfigurations.ReadAsync(checked((int)row.IdProduct), ct);
            if (configuration is null)
                return Conflict(new { code = "product-options-unavailable" });
            if (row.Quantity > configuration.MaxPurchaseQuantity)
                return Conflict(new { code = "purchase-limit" });

            long[] selectedValueIds = await db.AccountCartItemPurchaseOptionValues.AsNoTracking()
                .Where(selected => selected.IdCartItem == row.Id)
                .Select(selected => selected.IdProductPurchaseOptionValue).ToArrayAsync(ct);
            if (selectedValueIds.Length != configuration.Options.Count) return Conflict(new { code = "invalid-product-options" });
            var matchedOptionValueIds = new HashSet<long>();
            foreach (var option in configuration.Options)
            {
                ProductPurchaseOptionValue[] values = option.Values
                    .Where(candidate => selectedValueIds.Contains(candidate.Id)).ToArray();
                if (values.Length != 1) return Conflict(new { code = "invalid-product-options" });
                matchedOptionValueIds.Add(values[0].Id);
                unitAmount += values[0].PriceDelta;
            }
            if (matchedOptionValueIds.Count != selectedValueIds.Length)
                return Conflict(new { code = "invalid-product-options" });

            string[] selectedAccessoryKeys = await (
                from selected in db.AccountCartItemAccessories.AsNoTracking()
                join link in db.StoreProductAccessories.AsNoTracking() on selected.IdProductAccessory equals link.Id
                join accessory in db.StoreAccessories.AsNoTracking() on link.IdAccessory equals accessory.Id
                where selected.IdCartItem == row.Id
                select accessory.AccessoryKey
            ).ToArrayAsync(ct);
            var offer = catalog.Offers.SingleOrDefault(candidate =>
                candidate.ProductId == row.IdProduct && candidate.SalesPlanId == row.SalesPlanId);
            if (offer is null) return Conflict(new { code = "price-or-stock-changed" });
            if (selectedAccessoryKeys.Distinct(StringComparer.Ordinal).Count() != selectedAccessoryKeys.Length ||
                configuration.Accessories.Any(accessory => accessory.IsRequired &&
                    !selectedAccessoryKeys.Contains(accessory.Key, StringComparer.Ordinal)) ||
                selectedAccessoryKeys.Any(key => configuration.Accessories.All(accessory => accessory.Key != key)) ||
                offer.AccessoryIds is { } allowedAccessories &&
                    selectedAccessoryKeys.Any(key => !allowedAccessories.Contains(key, StringComparer.Ordinal)))
                return Conflict(new { code = "invalid-product-options" });
            foreach (var accessory in configuration.Accessories)
                if (accessory.IsRequired || selectedAccessoryKeys.Contains(accessory.Key, StringComparer.Ordinal))
                {
                    if (!offer.AccessoryPriceAmounts.TryGetValue(accessory.Key, out decimal amount))
                        return Conflict(new { code = "invalid-product-options" });
                    unitAmount += amount;
                }
            decimal lineAmount = checked(unitAmount * row.Quantity);
            subtotalAmount += lineAmount;
            if (eligible) eligibleAmount += lineAmount;
        }

        if (eligibleAmount <= 0) return Conflict(new { code = "coupon-no-eligible-items" });
        decimal discountAmount = decimal.Floor(eligibleAmount * coupon.DiscountPercent / 100m);
        return Ok(new
        {
            code = coupon.Code,
            discountPercent = coupon.DiscountPercent,
            subtotalAmount,
            eligibleAmount,
            discountAmount,
            totalAfterDiscount = Math.Max(0, subtotalAmount - discountAmount)
        });
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        if (request.AddressId < 1 ||
            request.PaymentMethod != "zarinpal") return BadRequest(new { code = "invalid-checkout" });
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var storedCartRows = await (
            from cart in db.AccountCarts.AsNoTracking()
            join item in db.AccountCartItems.AsNoTracking() on cart.Id equals item.IdCart
            join plan in db.SalesPlanReferences.AsNoTracking() on item.IdSalesPlan equals plan.Id
            where cart.IdProfile == ProfileId
            orderby item.Id
            select new
            {
                item.Id, item.IdProduct, item.IdProductVariant, item.Quantity,
                SalesPlanId = plan.StoreSalesPlanKey
            }).ToListAsync(ct);
        if (storedCartRows.Count is < 1 or > 99)
            return Conflict(new { code = "account-cart-empty-or-invalid" });
        long[] storedCartItemIds = storedCartRows.Select(item => item.Id).ToArray();
        var storedOptionRows = await (
            from selected in db.AccountCartItemPurchaseOptionValues.AsNoTracking()
            join value in db.StoreProductPurchaseOptionValues.AsNoTracking() on selected.IdProductPurchaseOptionValue equals value.Id
            join option in db.StoreProductPurchaseOptions.AsNoTracking() on value.IdProductPurchaseOption equals option.Id
            where storedCartItemIds.Contains(selected.IdCartItem)
            select new { selected.IdCartItem, option.OptionKey, ValueKey = value.ValueKey }).ToListAsync(ct);
        var storedAccessoryRows = await (
            from selected in db.AccountCartItemAccessories.AsNoTracking()
            join link in db.StoreProductAccessories.AsNoTracking() on selected.IdProductAccessory equals link.Id
            join accessory in db.StoreAccessories.AsNoTracking() on link.IdAccessory equals accessory.Id
            where storedCartItemIds.Contains(selected.IdCartItem)
            select new { selected.IdCartItem, AccessoryKey = accessory.AccessoryKey }).ToListAsync(ct);
        var cartLines = storedCartRows.Select(item =>
        {
            var selections = new
            {
                priceOptions = storedOptionRows.Where(option => option.IdCartItem == item.Id)
                    .Select(option => new { key = option.OptionKey, productValue = option.ValueKey }).ToArray(),
                accessories = storedAccessoryRows.Where(accessory => accessory.IdCartItem == item.Id)
                    .Select(accessory => new { id = accessory.AccessoryKey, selected = true }).ToArray()
            };
            JsonElement options = JsonSerializer.SerializeToElement(selections, AccountStateJsonOptions);
            return new CartLineRequest(item.IdProduct, item.Quantity, item.SalesPlanId, options,
                item.IdProductVariant);
        }).ToList();
        if (cartLines.Any(item => item.ProductId is < 1 or > int.MaxValue || item.VariantId is < 1 ||
                item.Quantity is < 1 or > 99 || string.IsNullOrWhiteSpace(item.SalesPlanId)) ||
            cartLines.Select(item => (item.ProductId, item.SalesPlanId, item.VariantId)).Distinct().Count() != cartLines.Count)
            return Conflict(new { code = "invalid-account-cart" });
        var checkoutOptions = new List<CheckoutOptions>(cartLines.Count);
        foreach (var line in cartLines)
        {
            if (!CheckoutOptions.TryRead(line.Options, out var parsed))
                return Conflict(new { code = "invalid-account-cart-options" });
            checkoutOptions.Add(parsed);
        }
        var address = await (
            from addressRow in db.AccountAddresses.AsNoTracking()
            join country in db.GeoCountries.AsNoTracking() on addressRow.IdCountry equals country.Id
            join province in db.GeoProvinces.AsNoTracking() on new { addressRow.IdProvince, addressRow.IdCountry }
                equals new { IdProvince = province.Id, province.IdCountry }
            join city in db.GeoCities.AsNoTracking() on new { addressRow.IdCity, addressRow.IdProvince, addressRow.IdCountry }
                equals new { IdCity = city.Id, city.IdProvince, city.IdCountry }
            where addressRow.Id == request.AddressId && addressRow.IdProfile == ProfileId
            select new
            {
                addressRow.Title, addressRow.Recipient, addressRow.Phone, country = country.NameFa,
                province = province.NameFa, city = city.NameFa, addressRow.Street, postal = addressRow.PostalCode
            }).SingleOrDefaultAsync(ct);
        if (address is null) return BadRequest(new { code = "address-not-owned" });
        string addressSnapshot = JsonSerializer.Serialize(address, AccountStateJsonOptions);
        var catalog = await plans.ReadAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var priced = new List<(CartLineRequest Line, decimal UnitAmount, string ProductName, List<StoreOrderItemPriceReference> PriceReferences, bool CouponEligible)>();
        try
        {
            foreach (var (line, parsedOptions) in cartLines.Zip(checkoutOptions))
            {
                if (line.VariantId.HasValue &&
                    !await ProductVariantExistsAsync(line.ProductId, line.VariantId.Value, ct))
                    return Conflict(new { code = "product-variant-unavailable" });
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
                        return Conflict(new { code = "inventory-variant-or-stock-changed" });
                }
                else if (line.VariantId.HasValue)
                    return Conflict(new { code = "product-variant-unavailable" });

                string? productName = await db.Products.AsNoTracking()
                    .Where(x => x.Id == line.ProductId && x.IsCatalogActive && x.IsStoreEnabled)
                    .Select(x => x.Name).SingleOrDefaultAsync(ct);
                if (productName is null) return Conflict(new { code = "product-unavailable" });
                ProductPurchaseConfiguration? configuration =
                    await purchaseConfigurations.ReadAsync(checked((int)line.ProductId), ct);
                if (configuration is null) return Conflict(new { code = "product-options-unavailable" });
                if (line.Quantity > configuration.MaxPurchaseQuantity) return Conflict(new { code = "purchase-limit" });
                var variantValues = parsedOptions.PriceOptions;
                var accessoryIds = parsedOptions.AccessoryIds;

                if (variantValues.Count != configuration.Options.Count || configuration.Options.Any(option =>
                        !variantValues.TryGetValue(option.Key, out var value) ||
                        option.Values.All(candidate => candidate.Key != value)))
                    return Conflict(new { code = "invalid-product-options" });
                var offer = catalog.Offers.Single(candidate =>
                    candidate.ProductId == quote.ProductId && candidate.SalesPlanId == quote.SalesPlanId);
                if (configuration.Accessories.Any(a => a.IsRequired && !accessoryIds.Contains(a.Key)) ||
                    accessoryIds.Any(id => configuration.Accessories.All(a => a.Key != id)) ||
                    offer.AccessoryIds is { } allowed &&
                    accessoryIds.Any(id => !allowed.Contains(id, StringComparer.Ordinal)))
                    return Conflict(new { code = "invalid-product-options" });
                decimal unitAmount = quote.UnitAmount;
                var priceReferences = new List<StoreOrderItemPriceReference>();
                ProductInventoryVariant? selectedInventoryVariant = catalog.InventoryVariants.SingleOrDefault(variant =>
                    variant.ProductId == line.ProductId && variant.VariantId == line.VariantId);
                if (selectedInventoryVariant is not null &&
                    selectedInventoryVariant.PriceBySalesPlan.TryGetValue(line.SalesPlanId, out decimal variantPrice))
                {
                    decimal variantDiscount =
                        selectedInventoryVariant.DiscountBySalesPlan.GetValueOrDefault(line.SalesPlanId);
                    unitAmount = decimal.Round(variantPrice * (100m - variantDiscount) / 100m, 0,
                        MidpointRounding.AwayFromZero);
                }
                bool couponEligible = line.SalesPlanId == "regular" && quote.DiscountPercent == 0 &&
                    (selectedInventoryVariant is null ||
                     selectedInventoryVariant.DiscountBySalesPlan.GetValueOrDefault(line.SalesPlanId) <= 0);
                long basePriceVersionId = selectedInventoryVariant?.PriceVersionIdsBySalesPlan.GetValueOrDefault(line.SalesPlanId) ?? 0;
                if (basePriceVersionId == 0) basePriceVersionId = offer.PriceVersionId;
                if (basePriceVersionId > 0)
                    priceReferences.Add(new StoreOrderItemPriceReference { IdPriceVersion = basePriceVersionId });

                foreach (var option in configuration.Options)
                {
                    string selected = variantValues[option.Key];
                    var selectedValue = option.Values.Single(v => v.Key == selected);
                    unitAmount += selectedValue.PriceDelta;
                    if (selectedValue.PriceVersionId > 0)
                        priceReferences.Add(new StoreOrderItemPriceReference { IdPriceVersion = selectedValue.PriceVersionId });
                }

                foreach (var accessory in configuration.Accessories)
                    if (accessory.IsRequired || accessoryIds.Contains(accessory.Key))
                    {
                        if (!offer.AccessoryPriceAmounts.TryGetValue(accessory.Key, out var amount))
                            return Conflict(new { code = "invalid-product-options" });
                        unitAmount += amount;
                        if (offer.AccessoryPriceVersionIds.TryGetValue(accessory.Key, out long accessoryPriceId) && accessoryPriceId > 0)
                            priceReferences.Add(new StoreOrderItemPriceReference { IdPriceVersion = accessoryPriceId });
                    }

                priced.Add((line, unitAmount, productName, priceReferences, couponEligible));
            }
        }
        catch (SalesPlanUnavailableException)
        {
            return Conflict(new { code = "price-or-stock-changed" });
        }

        decimal subtotal = priced.Sum(x => checked(x.UnitAmount * x.Line.Quantity));
        Shop.Data.Domain.Account.Entities.AccountCoupon? appliedCoupon = null;
        decimal couponDiscountAmount = 0;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            string couponCode = NormalizeCouponCode(request.CouponCode);
            if (couponCode.Length is < 1 or > 32 || couponCode.Any(character => !char.IsAsciiLetterOrDigit(character)))
                return BadRequest(new { code = "invalid-coupon" });
            appliedCoupon = await db.AccountCoupons.SingleOrDefaultAsync(item => item.Code == couponCode, ct);
            if (appliedCoupon is null || !appliedCoupon.Enabled ||
                appliedCoupon.ExpiresOn < DateOnly.FromDateTime(DateTime.UtcNow))
                return Conflict(new { code = "coupon-unavailable" });
            int couponUses = await db.StoreCouponUsages.CountAsync(item => item.IdCoupon == appliedCoupon.Id, ct);
            if (couponUses >= appliedCoupon.UsageLimit)
                return Conflict(new { code = "coupon-limit-reached" });
            decimal eligibleAmount = priced.Where(item => item.CouponEligible)
                .Sum(item => checked(item.UnitAmount * item.Line.Quantity));
            if (eligibleAmount <= 0) return Conflict(new { code = "coupon-no-eligible-items" });
            couponDiscountAmount = decimal.Floor(eligibleAmount * appliedCoupon.DiscountPercent / 100m);
        }
        decimal total = subtotal - couponDiscountAmount;
        if (total > 9999999999999999999m)
            return Conflict(new { code = "order-amount-out-of-range" });
        int count = priced.Sum(x => x.Line.Quantity);
        string number = $"ND{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        string[] checkoutPlanKeys = priced.Select(x => x.Line.SalesPlanId).Distinct(StringComparer.Ordinal).ToArray();
        var checkoutPlanIds = await db.SalesPlanReferences.AsNoTracking()
            .Where(x => checkoutPlanKeys.Contains(x.StoreSalesPlanKey) && x.IdStockOwnerType == 1 && x.IsActive)
            .ToDictionaryAsync(x => x.StoreSalesPlanKey, x => x.Id, ct);
        if (checkoutPlanIds.Count != checkoutPlanKeys.Length)
            return Conflict(new { code = "price-or-stock-changed" });

        var paymentBank = await db.StoreBanks.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == request.PaymentMethod && x.IsActive, ct);
        if (paymentBank is null || paymentBank.Code != "zarinpal")
            return Conflict(new { code = "payment-method-unavailable" });

        var order = new StoreCustomerOrderReference
        {
            IdProfile = ProfileId,
            IdAddress = request.AddressId,
            OrderNumber = number,
            Status = "pending-payment",
            PaymentMethod = request.PaymentMethod,
            IdBank = paymentBank.Id,
            ItemCount = count,
            TotalAmount = total,
            SubtotalAmount = subtotal,
            IdCoupon = appliedCoupon?.Id,
            CouponDiscountAmount = couponDiscountAmount,
            AddressSnapshotJson = addressSnapshot,
            OrderedAtUtc = DateTime.UtcNow,
            Items = priced.Select(item => new StoreCustomerOrderItem
            {
                IdProduct = item.Line.ProductId,
                IdProductVariant = item.Line.VariantId,
                ProductNameSnapshot = item.ProductName,
                IdSalesPlan = checkoutPlanIds[item.Line.SalesPlanId],
                Quantity = item.Line.Quantity,
                UnitAmount = item.UnitAmount,
                OptionsJson = item.Line.Options is { ValueKind: System.Text.Json.JsonValueKind.Object } options
                    ? options.GetRawText() : "{}",
                PriceReferences = item.PriceReferences
            }).ToList()
        };
        db.StoreCustomerOrders.Add(order);
        await SetCheckoutPreferenceAsync(request.AddressId, request.PaymentMethod, ct);
        await db.AccountCartItems.Where(item => db.AccountCarts.Any(cart =>
                cart.Id == item.IdCart && cart.IdProfile == ProfileId))
            .ExecuteDeleteAsync(ct);
        await db.SaveChangesAsync(ct);
        if (appliedCoupon is not null)
        {
            db.StoreCouponUsages.Add(new StoreCouponUsage
            {
                IdCoupon = appliedCoupon.Id,
                IdCustomerOrder = order.Id
            });
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return Ok(new
        {
            id = order.Id, number, status = "pending-payment", itemCount = count, totalAmount = total,
            subtotalAmount = subtotal, couponCode = appliedCoupon?.Code,
            couponDiscountAmount,
            orderedAtUtc = DateTimeOffset.UtcNow, message = "سفارش ثبت شد و در انتظار پرداخت است.",
            simulatedPaymentUrl = environment.IsDevelopment() ? Url.Action("Index", "SimulatedPayment", new { id = order.Id }) : null
        });
    }

    private async Task<bool> ProductVariantExistsAsync(long productId, long variantId,
        CancellationToken cancellationToken) =>
        await db.ProductVariants.AsNoTracking().AnyAsync(variant =>
            variant.Id == variantId && variant.IdProduct == productId && variant.IsActive &&
            variant.Product.IsWarehouseEnabled && variant.Product.IsCatalogActive &&
            variant.Product.IsStoreEnabled, cancellationToken);

    private static string NormalizeCouponCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

    private async Task SetCheckoutPreferenceAsync(long? addressId, string paymentMethod, CancellationToken ct)
    {
        var preference = await db.AccountCheckoutPreferences
            .SingleOrDefaultAsync(x => x.IdProfile == ProfileId, ct);
        if (preference is null)
        {
            preference = new AccountCheckoutPreference { IdProfile = ProfileId };
            db.AccountCheckoutPreferences.Add(preference);
        }
        preference.IdSelectedAddress = addressId;
        preference.PaymentMethod = paymentMethod;
        preference.UpdatedAtUtc = DateTime.UtcNow;
    }

    [HttpDelete("addresses/{id:long}")]
    public async Task<IActionResult> DeleteAddress(long id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var preference = await db.AccountCheckoutPreferences
            .SingleOrDefaultAsync(x => x.IdProfile == ProfileId && x.IdSelectedAddress == id, ct);
        if (preference is not null)
        {
            preference.IdSelectedAddress = null;
            preference.UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.StoreCustomerOrders.Where(x => x.IdProfile == ProfileId && x.IdAddress == id)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.IdAddress, (long?)null), ct);
        var address = await db.AccountAddresses.SingleOrDefaultAsync(x => x.Id == id && x.IdProfile == ProfileId, ct);
        if (address is not null) db.AccountAddresses.Remove(address);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { deleted = address is not null });
    }

    [HttpPut("addresses/{id:long}")]
    public async Task<IActionResult> UpdateAddress(long id, AddressRequest a, CancellationToken ct)
    {
        if (id < 1 || new[]
            {
                a.Title, a.Recipient, a.Phone, a.Street, a.Postal
            }.Any(v => string.IsNullOrWhiteSpace(v)) ||
            a.Street!.Length > 500 || a.CountryId is null or < 1 || a.ProvinceId is null or < 1 || a.CityId is null or < 1)
            return BadRequest(new { code = "invalid-address" });
        if (!await IsAddressGeographyValidAsync(a.CountryId.Value, a.ProvinceId.Value, a.CityId.Value, ct))
            return BadRequest(new { code = "invalid-address-location" });
        var address = await db.AccountAddresses.SingleOrDefaultAsync(x => x.Id == id && x.IdProfile == ProfileId, ct);
        if (address is null) return NotFound(new { code = "address-not-found" });
        address.Title = a.Title!.Trim();
        address.Recipient = a.Recipient!.Trim();
        address.Phone = a.Phone!.Trim();
        address.IdCountry = a.CountryId.Value;
        address.IdProvince = a.ProvinceId.Value;
        address.IdCity = a.CityId.Value;
        address.Street = a.Street!.Trim();
        address.PostalCode = a.Postal!.Trim();
        await db.SaveChangesAsync(ct);
        return Ok(new { saved = true, id });
    }

    private async Task<bool> IsAddressGeographyValidAsync(long countryId, long provinceId, long cityId,
        CancellationToken cancellationToken) =>
        await db.GeoCities.AsNoTracking().AnyAsync(city =>
            city.Id == cityId && city.IdProvince == provinceId && city.IdCountry == countryId &&
            db.GeoProvinces.Any(province => province.Id == city.IdProvince && province.IdCountry == city.IdCountry),
            cancellationToken);

    [HttpPost("lists/{kind}/{productId:long}")]
    public async Task<IActionResult> ToggleList(string kind, long productId, CancellationToken ct)
    {
        if (kind is not ("favorites" or "compare") || productId < 1)
            return BadRequest(new { code = "invalid-list-item" });
        if (!await db.Products.AsNoTracking().AnyAsync(x => x.Id == productId && x.IsCatalogActive && x.IsStoreEnabled, ct))
            return Conflict(new { code = "product-unavailable" });
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        bool changed;
        if (kind == "favorites")
        {
            var existing = await db.AccountFavorites.SingleOrDefaultAsync(x => x.IdProfile == ProfileId && x.IdProduct == productId, ct);
            changed = existing is null;
            if (existing is null) db.AccountFavorites.Add(new AccountFavorite { IdProfile = ProfileId, IdProduct = productId, CreatedAtUtc = DateTime.UtcNow });
            else db.AccountFavorites.Remove(existing);
        }
        else
        {
            var existing = await db.AccountCompareItems.SingleOrDefaultAsync(x => x.IdProfile == ProfileId && x.IdProduct == productId, ct);
            changed = existing is null;
            if (existing is null) db.AccountCompareItems.Add(new AccountCompareItem { IdProfile = ProfileId, IdProduct = productId, CreatedAtUtc = DateTime.UtcNow });
            else db.AccountCompareItems.Remove(existing);
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { changed });
    }

    [HttpPut("lists/{kind}")]
    public async Task<IActionResult> SaveList(string kind, [FromBody] ListRequest request, CancellationToken ct)
    {
        if (kind is not ("favorites" or "compare") || request.ProductIds is null || request.ProductIds.Count > 100 ||
            request.ProductIds.Any(id => id < 1) ||
            request.ProductIds.Distinct().Count() != request.ProductIds.Count)
            return BadRequest(new { code = "invalid-list" });
        long[] productIds = request.ProductIds.ToArray();
        int availableProductCount = await db.Products.AsNoTracking()
            .CountAsync(x => productIds.Contains(x.Id) && x.IsCatalogActive && x.IsStoreEnabled, ct);
        if (availableProductCount != productIds.Length)
            return Conflict(new { code = "product-unavailable" });
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (kind == "favorites")
        {
            await db.AccountFavorites.Where(x => x.IdProfile == ProfileId).ExecuteDeleteAsync(ct);
            db.AccountFavorites.AddRange(productIds.Select(id => new AccountFavorite { IdProfile = ProfileId, IdProduct = id, CreatedAtUtc = DateTime.UtcNow }));
        }
        else
        {
            await db.AccountCompareItems.Where(x => x.IdProfile == ProfileId).ExecuteDeleteAsync(ct);
            db.AccountCompareItems.AddRange(productIds.Select(id => new AccountCompareItem { IdProfile = ProfileId, IdProduct = id, CreatedAtUtc = DateTime.UtcNow }));
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { saved = true });
    }

    [HttpPut("price-alerts")]
    public async Task<IActionResult> SavePriceAlerts([FromBody] PriceAlertListRequest request, CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count > 100)
            return BadRequest(new { code = "invalid-price-alerts" });
        var normalized = new List<object>();
        var configurations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in request.Items)
        {
            if (item is null || !long.TryParse(item.ProductId, out long productId) || productId < 1 ||
                item.Scope is not ("all" or "variant") || item.PriceAtRegistration is < 0 or > 9999999999999999999m ||
                decimal.Truncate(item.PriceAtRegistration) != item.PriceAtRegistration ||
                (item.StockAtRegistration?.Length ?? 0) > 100 ||
                item.Scope == "variant" && item.Variants is not { ValueKind: System.Text.Json.JsonValueKind.Object })
                return BadRequest(new { code = "invalid-price-alert" });
            string? variants = item.Variants is { ValueKind: System.Text.Json.JsonValueKind.Object } value
                ? value.GetRawText()
                : null;
            string signature = variants ?? "";
            if (signature.Length > 500) return BadRequest(new { code = "invalid-price-alert" });
            if (!configurations.Add($"{productId}:{item.Scope}:{signature}"))
                return BadRequest(new { code = "duplicate-price-alert" });
            normalized.Add(new
            {
                productId, scope = item.Scope, signature, variantsJson = variants is null ? (System.Text.Json.JsonElement?)null : item.Variants, item.PriceAtRegistration,
                item.StockAtRegistration, item.CreatedAt
            });
        }

        long[] alertProductIds = normalized.Select(item => (long)item.GetType().GetProperty("productId")!.GetValue(item)!).Distinct().ToArray();
        int availableAlertProducts = await db.Products.AsNoTracking()
            .CountAsync(x => alertProductIds.Contains(x.Id) && x.IsCatalogActive && x.IsStoreEnabled, ct);
        if (availableAlertProducts != alertProductIds.Length)
            return Conflict(new { code = "product-unavailable" });

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await db.AccountPriceAlerts.Where(x => x.IdProfile == ProfileId).ExecuteDeleteAsync(ct);
        foreach (dynamic item in normalized)
        {
            long productId = item.productId;
            string scope = item.scope;
            string signature = item.signature;
            string? variantsJson = item.variantsJson?.GetRawText();
            DateTime createdAt = item.CreatedAt is DateTime date ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : DateTime.UtcNow;
            db.AccountPriceAlerts.Add(new AccountPriceAlert
            {
                IdProfile = ProfileId,
                IdProduct = productId,
                Scope = scope,
                VariantSignature = signature,
                VariantsJson = variantsJson,
                PriceAtRegistration = item.PriceAtRegistration,
                StockAtRegistration = item.StockAtRegistration,
                CreatedAtUtc = createdAt
            });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { saved = true });
    }

    [HttpPost("articles/{slug}/interactions/{type}")]
    public async Task<IActionResult> ToggleArticleInteraction(string slug, string type, CancellationToken ct)
    {
        if (slug.Length is < 1 or > 160 || !slug.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ||
            type is not ("like" or "bookmark" or "read")) return BadRequest(new { code = "invalid-interaction" });
        var articleId = await db.MagazineArticles.AsNoTracking()
            .Where(x => x.ArticleKey == slug && x.IsPublished).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct);
        if (articleId is null) return NotFound(new { code = "article-not-found" });
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var interaction = await db.AccountArticleInteractions.SingleOrDefaultAsync(x =>
            x.IdProfile == ProfileId && x.IdMagazineArticle == articleId.Value && x.InteractionType == type, ct);
        if (interaction is null)
            db.AccountArticleInteractions.Add(new AccountArticleInteraction
                { IdProfile = ProfileId, IdMagazineArticle = articleId.Value, InteractionType = type });
        else
            db.AccountArticleInteractions.Remove(interaction);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { saved = true });
    }

    [HttpPost("articles/{slug}/feedback")]
    public async Task<IActionResult> AddArticleFeedback(string slug, [FromBody] ArticleFeedbackRequest request,
        CancellationToken ct)
    {
        string name = (request.Name ?? "").Trim();
        string text = (request.Text ?? "").Trim();
        if (slug.Length is < 1 or > 160 || !slug.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ||
            name.Length is < 3 or > 80 || name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2 ||
            text.Length is < 1 or > 1000 ||
            request.Rating is < 1 or > 5) return BadRequest(new { code = "invalid-feedback" });
        var articleId = await db.MagazineArticles.AsNoTracking()
            .Where(x => x.ArticleKey == slug && x.IsPublished).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct);
        if (articleId is null) return NotFound(new { code = "article-not-found" });
        var feedback = new AccountArticleFeedback
        {
            IdProfile = ProfileId,
            IdMagazineArticle = articleId.Value,
            DisplayName = name,
            FeedbackText = text,
            Rating = checked((byte)request.Rating.GetValueOrDefault()),
            Status = "pending",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.AccountArticleFeedback.Add(feedback);
        await db.SaveChangesAsync(ct);
        return Ok(new { saved = true, id = feedback.Id, status = "pending" });
    }

    public sealed record ProfileRequest(
        string FirstName,
        string LastName,
        string? Phone,
        int? IdGender = null,
        bool Newsletter = false);

    public sealed record PasswordChangeRequest(string? CurrentPassword, string? NewPassword, string? NewPasswordConfirmation);

    public sealed record CartRequest(IReadOnlyList<CartLineRequest>? Items);

    public sealed record CartLineRequest(
        long ProductId,
        int Quantity,
        string SalesPlanId,
        System.Text.Json.JsonElement? Options,
        long? VariantId = null,
        IReadOnlyList<long>? OptionValueIds = null,
        IReadOnlyList<long>? AccessoryIds = null);

    public sealed record CheckoutRequest(long AddressId, string PaymentMethod, string? CouponCode = null);

    public sealed record CouponRequest(string? Code);

    public sealed record CheckoutPreferenceRequest(long? AddressId, string PaymentMethod);

    public sealed record ListRequest(IReadOnlyList<long>? ProductIds);

    public sealed record PriceAlertListRequest(IReadOnlyList<PriceAlertRequest>? Items);

    public sealed record PriceAlertRequest(
        string? ProductId,
        string? Scope,
        System.Text.Json.JsonElement? Variants,
        decimal PriceAtRegistration,
        string? StockAtRegistration,
        DateTimeOffset CreatedAt);

    public sealed record ArticleFeedbackRequest(string? Name, string? Text, byte? Rating);

    public sealed record RecentSearchRequest(string? Query);

    public sealed record AddressRequest(
        string? Title,
        string? Recipient,
        string? Phone,
        long? CountryId,
        long? ProvinceId,
        long? CityId,
        string? Street,
        string? Postal);
}
