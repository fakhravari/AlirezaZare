using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Account.Contracts;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Domain.Store.Entities;

namespace Shop.Data.Infrastructure.Account;

public sealed class AccountStateQueryService(ShopDbContext db) : IAccountStateQueryService
{
    private static readonly JsonSerializerOptions AccountStateJsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private static JsonElement? ParseJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        using var document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public async Task<string?> ReadJsonAsync(long profileId, CancellationToken ct)
    {
        var profile = await db.AccountProfiles
            .AsNoTracking()
            .Where(x => x.Id == profileId)
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.FirstName,
                x.LastName,
                x.Phone,
                x.IdGender,
                x.NewsletterEnabled,
                x.Password
            })
            .SingleOrDefaultAsync(ct);
        if (profile is null) return null;

        var cartRows = await db.AccountCarts
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
                row => row.item.IdSalesPlan,
                plan => plan.Id,
                (row, plan) => new { row.cart, row.item, plan })
            .Where(row => row.cart.IdProfile == profileId)
            .Select(row => new
            {
                row.item.Id,
                row.item.IdProduct,
                row.item.IdVariant,
                row.item.Quantity,
                row.plan.StoreSalesPlanKey
            })
            .ToListAsync(ct);
        decimal[] cartItemIds = cartRows.Select(item => item.Id).ToArray();
        var cartAccessoryRows = cartItemIds.Length == 0
            ? []
            : await db.AccountCartItemAccessories
                .AsNoTracking()
                .Join(
                    db.AccountCartItems
                        .AsNoTracking(),
                    selected => selected.IdCartItem,
                    cartItem => cartItem.Id,
                    (selected, cartItem) => new { selected, cartItem })
                .Join(
                    db.StoreProductAccessories
                        .AsNoTracking(),
                    row => row.selected.IdProductAccessory,
                    link => link.Id,
                    (row, link) => new { row.selected, row.cartItem, link })
                .Join(
                    db.StoreAccessories
                        .AsNoTracking(),
                    row => row.link.IdAccessory,
                    accessory => accessory.Id,
                    (row, accessory) => new { row.selected, row.cartItem, row.link, accessory })
                .Where(row => cartItemIds.Contains(row.selected.IdCartItem))
                .Select(row => new
                {
                    row.selected.IdCartItem,
                    ProductAccessoryId = row.link.Id,
                    AccessoryId = row.accessory.Id,
                    row.accessory.Name,
                    row.link.IsRequired,
                    PriceAmount = db.StorePriceVersions
                        .AsNoTracking()
                        .Where(version => version.IdProduct == row.link.IdProduct &&
                                          version.IdAccessory == row.link.IdAccessory &&
                                          version.IdSalesPlan == row.cartItem.IdSalesPlan && version.IsActive)
                        .Select(version => (decimal?)version.FinalPriceAmount)
                        .FirstOrDefault()
                })
                .ToListAsync(ct);
        var cartVariantRows = cartRows.Where(item => item.IdVariant.HasValue)
            .Select(item => item.IdVariant!.Value).Distinct().ToArray();
        var cartVariants = cartVariantRows.Length == 0
            ? []
            : await db.ProductVariants
                .AsNoTracking()
                .Where(variant => cartVariantRows.Contains(variant.Id))
                .Select(variant => new
                {
                    variant.Id,
                    Name = variant.VariantName.TreePathNames
                })
                .ToListAsync(ct);
        var cartVariantAttributes = cartVariantRows.Length == 0
            ? []
            : await db.ProductVariantAttributeValues
                .AsNoTracking()
                .Join(
                    db.ProductAttributeValues
                        .AsNoTracking(),
                    link => link.IdProductAttributeValue,
                    value => value.Id,
                    (link, value) => new { link, value })
                .Join(
                    db.ProductAttributes
                        .AsNoTracking(),
                    row => row.value.IdProductAttribute,
                    attribute => attribute.Id,
                    (row, attribute) => new { row.link, row.value, attribute })
                .Where(row => cartVariantRows.Contains(row.link.IdProductVariant))
                .OrderBy(row => row.attribute.SortOrder)
                .ThenBy(row => row.attribute.Id)
                .Select(row => new
                {
                    row.link.IdProductVariant,
                    row.attribute.Key,
                    AttributeLabel = row.attribute.Label,
                    row.value.ValueKey,
                    row.value.DisplayValue,
                    row.value.Swatch
                })
                .ToListAsync(ct);
        var cartAttributesByVariant = cartVariantAttributes.GroupBy(x => x.IdProductVariant).ToDictionary(x => x.Key,
            x => (JsonElement?)JsonSerializer.SerializeToElement(x.Select(a => new
            {
                key = a.Key, label = a.AttributeLabel, value = a.ValueKey, display = a.DisplayValue, swatch = a.Swatch
            }).ToArray()));

        var addresses = await db.AccountAddresses
            .AsNoTracking()
            .Join(
                db.GeoCountries
                    .AsNoTracking(),
                address => address.IdCountry,
                country => country.Id,
                (address, country) => new { address, country })
            .Join(
                db.GeoProvinces
                    .AsNoTracking(),
                row => new { row.address.IdProvince, row.address.IdCountry },
                province => new { IdProvince = province.Id, province.IdCountry },
                (row, province) => new { row.address, row.country, province })
            .Join(
                db.GeoCities
                    .AsNoTracking(),
                row =>
                    new { row.address.IdCity, row.address.IdProvince, row.address.IdCountry },
                city => new { IdCity = city.Id, city.IdProvince, city.IdCountry },
                (row, city) => new { row.address, row.country, row.province, city })
            .Where(row => row.address.IdProfile == profileId)
            .Select(row => new
            {
                row.address.Id,
                row.address.Title,
                row.address.Recipient,
                row.address.Phone,
                row.country.NameFa,
                row.address.IdCountry,
                ProvinceName = row.province.NameFa,
                row.address.IdProvince,
                CityName = row.city.NameFa,
                row.address.IdCity,
                row.address.Street,
                row.address.PostalCode,
                row.address.IsDefault
            })
            .ToListAsync(ct);

        var favorites = await db.AccountFavorites
            .AsNoTracking()
            .Where(x => x.IdProfile == profileId)
            .Select(x => x.IdProduct)
            .ToListAsync(ct);
        var compare = await db.AccountCompareItems
            .AsNoTracking()
            .Where(x => x.IdProfile == profileId)
            .Select(x => x.IdProduct)
            .ToListAsync(ct);

        var alertRows = await db.AccountPriceAlerts
            .AsNoTracking()
            .Where(x => x.IdProfile == profileId)
            .Select(x => new
            {
                x.IdProduct,
                x.Scope,
                x.VariantsJson,
                x.PriceAtRegistration,
                x.StockAtRegistration,
                x.CreatedAtUtc
            })
            .ToListAsync(ct);

        var interactions = await db.AccountArticleInteractions
            .AsNoTracking()
            .Join(
                db.MagazineArticles
                    .AsNoTracking(),
                interaction => interaction.IdMagazineArticle,
                article => article.Id,
                (interaction, article) => new { interaction, article })
            .Where(row => row.interaction.IdProfile == profileId)
            .Select(row => new
            {
                slug = row.article.ArticleKey,
                type = row.interaction.InteractionType
            })
            .ToListAsync(ct);

        var feedback = await db.AccountArticleFeedback
            .AsNoTracking()
            .Join(
                db.MagazineArticles
                    .AsNoTracking(),
                entry => entry.IdMagazineArticle,
                article => article.Id,
                (entry, article) => new { entry, article })
            .Where(row => row.entry.IdProfile == profileId)
            .Select(row => new
            {
                slug = row.article.ArticleKey,
                name = row.entry.DisplayName,
                text = row.entry.FeedbackText,
                row.entry.Rating,
                Status = row.entry.StatusReference.Code,
                StatusName = row.entry.StatusReference.NameFa,
                row.entry.CreatedAtUtc
            })
            .ToListAsync(ct);

        var checkout = await db.StoreCustomerOrders
            .AsNoTracking()
            .Where(x => x.IdProfile == profileId)
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                selectedAddressId = x.IdAddress,
                paymentMethod = x.Bank.Code
            })
            .FirstOrDefaultAsync(ct);

        var orderRows = await db.StoreCustomerOrders
            .AsNoTracking()
            .Where(x => x.IdProfile == profileId)
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                StatusNameEn = x.StatusReference.NameEn,
                StatusName = x.StatusReference.NameFa,
                PaymentMethod = x.Bank.Code,
                Address = new
                {
                    recipient = x.Address.Recipient,
                    phone = x.Address.Phone,
                    street = x.Address.Street,
                    postal = x.Address.PostalCode,
                    province = db.GeoProvinces.Where(row => row.Id == x.Address.IdProvince)
                        .Select(row => row.NameFa).FirstOrDefault(),
                    city = db.GeoCities.Where(row => row.Id == x.Address.IdCity)
                        .Select(row => row.NameFa).FirstOrDefault()
                },
                x.CreatedAtUtc,
                ItemCount = x.Items.Sum(item => item.Quantity),
                x.TotalAmount,
                x.SubtotalAmount,
                x.IdCoupon,
                x.CouponDiscountAmount
            }).ToListAsync(ct);
        decimal[] usedCouponIds = orderRows.Where(order => order.IdCoupon.HasValue).Select(order => order.IdCoupon!.Value)
            .Distinct().ToArray();
        var couponCodesById = usedCouponIds.Length == 0
            ? new Dictionary<decimal, string>()
            : await db.AccountCoupons
                .AsNoTracking()
                .Where(coupon => usedCouponIds.Contains(coupon.Id))
                .ToDictionaryAsync(coupon => coupon.Id, coupon => coupon.Code, ct);
        var orderIds = orderRows.Select(x => x.Id).ToArray();
        var orderItemRows = orderIds.Length == 0
            ? []
            : await db.StoreCustomerOrderItems
                .AsNoTracking()
                .Join(
                    db.SalesPlanReferences
                        .AsNoTracking(),
                    item => item.IdSalesPlan,
                    plan => plan.Id,
                    (item, plan) => new { item, plan })
                .Join(
                    db.Products
                        .AsNoTracking(),
                    itemPlan => itemPlan.item.IdProduct,
                    product => product.Id,
                    (itemPlan, product) => new { itemPlan.item, itemPlan.plan, product })
                .Where(itemPlanProduct => orderIds.Contains(itemPlanProduct.item.IdOrder))
                .Select(itemPlanProduct => new
                {
                    itemPlanProduct.item.Id,
                    itemPlanProduct.item.IdOrder,
                    itemPlanProduct.item.IdProduct,
                    itemPlanProduct.item.IdVariant,
                    itemPlanProduct.item.IdSalesPlan,
                    ProductName = itemPlanProduct.product.NameFa,
                    VariantName = db.ProductVariants
                        .AsNoTracking()
                        .Where(variant => variant.Id == itemPlanProduct.item.IdVariant)
                        .Select(variant => variant.VariantName.TreePathNames)
                        .FirstOrDefault(),
                    itemPlanProduct.plan.StoreSalesPlanKey,
                    SalesPlanName = itemPlanProduct.plan.DisplayName,
                    itemPlanProduct.item.Quantity,
                    itemPlanProduct.item.IdPriceVersion,
                    PriceAmount = itemPlanProduct.item.PriceVersion.PriceBeforeDiscountAmount,
                    itemPlanProduct.item.PriceVersion.DiscountPercent,
                    itemPlanProduct.item.PriceVersion.FinalPriceAmount
                })
                .ToListAsync(ct);
        var orderItemIds = orderItemRows.Select(x => x.Id).ToArray();
        var orderPriceReferences = orderItemIds.Length == 0
            ? []
            : await db.StoreOrderAccessories
                .AsNoTracking()
                .Join(
                    db.StorePriceVersions
                        .AsNoTracking(),
                    reference => reference.IdPriceVersion,
                    version => version.Id,
                    (reference, version) => new { reference, version })
                .Where(referenceVersion => orderIds.Contains(referenceVersion.reference.IdOrder))
                .Select(referenceVersion => new
                {
                    referenceVersion.reference.IdOrder,
                    referenceVersion.reference.IdProduct,
                    referenceVersion.reference.IdSalesPlan,
                    referenceVersion.reference.IdPriceVersion,
                    referenceVersion.version.IdVariant,
                    referenceVersion.reference.IdAccessory,
                    PriceAmount = referenceVersion.version.FinalPriceAmount,
                    referenceVersion.version.PriceBeforeDiscountAmount,
                    referenceVersion.version.DiscountPercent,
                    referenceVersion.version.FinalPriceAmount,
                    AccessoryName = referenceVersion.reference.Accessory.Name
                })
                .ToListAsync(ct);
        var orderItemsById = orderItemRows.GroupBy(x => x.IdOrder)
            .ToDictionary(group => group.Key, group => group.ToArray());
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
            ["cart"] = cartRows.Select(item => new
            {
                id = item.IdProduct,
                variantId = item.IdVariant,
                item.Quantity,
                salesPlanId = item.StoreSalesPlanKey,
                options = new
                {
                    inventoryVariant = item.IdVariant is { } variantId &&
                                       cartVariants.FirstOrDefault(variant => variant.Id == variantId) is { } variant
                        ? new
                        {
                            id = variant.Id, name = variant.Name,
                            attributes = cartAttributesByVariant.GetValueOrDefault(variant.Id)
                        }
                        : null,
                    accessories = cartAccessoryRows.Where(accessory => accessory.IdCartItem == item.Id)
                        .Select(accessory => new
                        {
                            id = accessory.AccessoryId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            productAccessoryId = accessory.ProductAccessoryId,
                            name = accessory.Name,
                            required = accessory.IsRequired,
                            selected = true,
                            price = (accessory.PriceAmount ?? 0m) / 1_000_000m
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
                entry.StatusName,
                createdAt = AsUtc(entry.CreatedAtUtc)
            }).ToArray(),
            ["checkout"] = checkout,
            ["orders"] = orderRows.Select(order => new
            {
                id = order.Id,
                number = order.Id.ToString(CultureInfo.InvariantCulture),
                Status = StoreOrderStatusNames.CodeFromNameEn(order.StatusNameEn),
                order.StatusName,
                order.PaymentMethod,
                order.Address,
                createdAtUtc = AsUtc(order.CreatedAtUtc),
                order.ItemCount,
                order.TotalAmount,
                order.SubtotalAmount,
                couponCode = order.IdCoupon is { } couponId ? couponCodesById.GetValueOrDefault(couponId) : null,
                order.CouponDiscountAmount,
                items = orderItemsById.GetValueOrDefault(order.Id, []).Select(item => new
                {
                    productId = item.IdProduct,
                    variantId = item.IdVariant,
                    name = item.ProductName,
                    variantName = item.VariantName,
                    salesPlanId = item.StoreSalesPlanKey,
                    salesPlanName = item.SalesPlanName,
                    item.Quantity,
                    unitAmount = item.FinalPriceAmount + orderPriceReferences
                        .Where(reference =>
                            reference.IdOrder == item.IdOrder && reference.IdProduct == item.IdProduct &&
                            reference.IdSalesPlan == item.IdSalesPlan).Sum(reference => reference.PriceAmount),
                    options = new
                    {
                        accessories = orderPriceReferences
                            .Where(reference =>
                                reference.IdOrder == item.IdOrder && reference.IdProduct == item.IdProduct &&
                                reference.IdSalesPlan == item.IdSalesPlan).Select(reference =>
                                new { id = reference.IdAccessory.ToString(), selected = true }).ToArray()
                    },
                    priceReferences = orderPriceReferences.Where(reference =>
                        reference.IdOrder == item.IdOrder && reference.IdProduct == item.IdProduct &&
                        reference.IdSalesPlan == item.IdSalesPlan).Select(reference => new
                    {
                        priceVersionId = reference.IdPriceVersion,
                        component = StorePriceVersionReference.GetComponent(reference.IdVariant,
                            reference.IdAccessory),
                        name = reference.AccessoryName,
                        calculatedUnitAmount = reference.PriceAmount,
                        priceBeforeDiscountAmount = reference.FinalPriceAmount == reference.PriceAmount
                            ? (decimal?)reference.PriceBeforeDiscountAmount
                            : null,
                        discountPercent = reference.FinalPriceAmount == reference.PriceAmount
                            ? (decimal?)reference.DiscountPercent
                            : null
                    }).Prepend(new
                    {
                        priceVersionId = item.IdPriceVersion,
                        component = item.IdVariant.HasValue ? "variant" : "product",
                        name = item.VariantName ?? item.ProductName, calculatedUnitAmount = item.FinalPriceAmount,
                        priceBeforeDiscountAmount = (decimal?)item.PriceAmount,
                        discountPercent = (decimal?)item.DiscountPercent
                    }).ToArray()
                }).ToArray()
            }).ToArray()
        };

        string json = JsonSerializer.Serialize(state, AccountStateJsonOptions);

        return json;
    }
}
