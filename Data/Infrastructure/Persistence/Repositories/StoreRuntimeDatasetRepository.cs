using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;

namespace Shop.Data.Infrastructure.Persistence.Repositories;

public sealed class StoreRuntimeDatasetRepository(ShopDbContext dbContext, IMediaCdnBaseUrlProvider mediaCdn)
    : IStoreRuntimeDatasetRepository
{
    public async Task<string?> ReadCatalogJsonAsync(CancellationToken cancellationToken)
    {
        var groups = await dbContext.StoreSpecificationGroups.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var specCategories = await dbContext.StoreSpecificationCategories.AsNoTracking().OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var features = await dbContext.StoreSpecificationFeatures.AsNoTracking().OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var attributes = await dbContext.StoreAttributeDefinitions.AsNoTracking().OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var brands = await dbContext.StoreBrands.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var countries = await dbContext.GeoCountries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var categories = await dbContext.StoreCategories.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var colors = await dbContext.StoreColors.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var assets = await dbContext.StoreProductAssets.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var badges = await dbContext.StoreProductBadges.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var products = await dbContext.Products.AsNoTracking().Where(x => x.IsStoreEnabled).OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var productIds = products.Select(x => x.Id).ToArray();
        var regularPrices = await dbContext.StorePriceVersions
            .AsNoTracking()
            .Join(
                dbContext.SalesPlanReferences.AsNoTracking(),
                version => version.IdSalesPlan,
                plan => (long?)plan.Id,
                (version, plan) => new { version, plan })
            .Where(pricePlan =>
                productIds.Contains(pricePlan.version.IdProduct) && pricePlan.version.IsActive && pricePlan.plan.IsRegular &&
                pricePlan.plan.IdStockOwnerType == 1 && pricePlan.version.IdProductVariant == null &&
                pricePlan.version.IdAccessory == null)
            .OrderBy(pricePlan => pricePlan.plan.Id)
            .ThenByDescending(pricePlan => pricePlan.version.Id)
            .Select(pricePlan => pricePlan.version)
            .ToListAsync(cancellationToken);
        var regularPriceByProduct = regularPrices.GroupBy(x => x.IdProduct).ToDictionary(x => x.Key, x => x.First());
        var inventoryTotals = await dbContext.InventoryItems
            .AsNoTracking()
            .Join(
                dbContext.StockBalances.AsNoTracking(),
                inventory => inventory.Id,
                balance => balance.IdInventoryItem,
                (inventory, balance) => new { inventory, balance })
            .Where(inventoryBalance => productIds.Contains(inventoryBalance.inventory.IdProduct) &&
                                      inventoryBalance.inventory.Product.IsWarehouseEnabled)
            .GroupBy(inventoryBalance => inventoryBalance.inventory.IdProduct)
            .Select(productGroup => new
            {
                IdProduct = productGroup.Key,
                Quantity = productGroup.Sum(inventoryBalance => inventoryBalance.balance.Quantity)
            })
            .ToDictionaryAsync(x => x.IdProduct, x => x.Quantity, cancellationToken);
        var productColors = await dbContext.StoreProductColors.AsNoTracking()
            .Where(x => productIds.Contains(x.IdProduct)).ToListAsync(cancellationToken);
        var catalogFeatureValues = await dbContext.StoreProductCatalogFeatureValues
            .AsNoTracking()
            .Join(
                dbContext.StoreCatalogFeatures.AsNoTracking(),
                assignment => assignment.IdFeature,
                feature => feature.Id,
                (assignment, feature) => new { assignment, feature })
            .Join(
                dbContext.StoreCatalogFeatureValues.AsNoTracking(),
                assignmentFeature => assignmentFeature.assignment.IdFeatureValue,
                value => value.Id,
                (assignmentFeature, value) => new { assignmentFeature.assignment, assignmentFeature.feature, value })
            .Where(assignmentFeatureValue =>
                productIds.Contains(assignmentFeatureValue.assignment.IdProduct) &&
                assignmentFeatureValue.feature.IsActive && assignmentFeatureValue.value.IsActive)
            .Select(assignmentFeatureValue => new
            {
                assignmentFeatureValue.assignment.IdProduct,
                FeatureId = assignmentFeatureValue.feature.Id,
                FeatureName = assignmentFeatureValue.feature.Name,
                ValueId = assignmentFeatureValue.value.Id,
                ValueName = assignmentFeatureValue.value.Name
            })
            .ToListAsync(cancellationToken);
        var variantFeatureValues = await dbContext.StoreVariantCatalogFeatureValues
            .AsNoTracking()
            .Join(
                dbContext.ProductVariants.AsNoTracking(),
                assignment => assignment.IdProductVariant,
                variant => variant.Id,
                (assignment, variant) => new { assignment, variant })
            .Join(
                dbContext.StoreCatalogFeatures.AsNoTracking(),
                assignmentVariant => assignmentVariant.assignment.IdFeature,
                feature => feature.Id,
                (assignmentVariant, feature) => new { assignmentVariant.assignment, assignmentVariant.variant, feature })
            .Join(
                dbContext.StoreCatalogFeatureValues.AsNoTracking(),
                assignmentVariantFeature => assignmentVariantFeature.assignment.IdFeatureValue,
                value => value.Id,
                (assignmentVariantFeature, value) => new
                {
                    assignmentVariantFeature.variant,
                    assignmentVariantFeature.feature,
                    value
                })
            .Where(variantFeatureValue => productIds.Contains(variantFeatureValue.variant.IdProduct) &&
                                          variantFeatureValue.variant.IsActive &&
                                          variantFeatureValue.feature.IsActive && variantFeatureValue.value.IsActive)
            .Select(variantFeatureValue => new
            {
                variantFeatureValue.variant.IdProduct,
                FeatureId = variantFeatureValue.feature.Id,
                FeatureName = variantFeatureValue.feature.Name,
                ValueId = variantFeatureValue.value.Id,
                ValueName = variantFeatureValue.value.Name
            })
            .ToListAsync(cancellationToken);
        var catalogFeatureOptions = await dbContext.StoreCatalogFeatures
            .AsNoTracking()
            .Join(
                dbContext.StoreCatalogFeatureValues.AsNoTracking(),
                feature => feature.Id,
                value => value.IdFeature,
                (feature, value) => new { feature, value })
            .Where(featureValue => featureValue.feature.IsActive && featureValue.value.IsActive)
            .Select(featureValue => new
            {
                FeatureId = featureValue.feature.Id,
                FeatureName = featureValue.feature.Name,
                ValueId = featureValue.value.Id,
                ValueName = featureValue.value.Name,
                featureValue.feature.Kind
            })
            .ToListAsync(cancellationToken);
        var productAccessories = await dbContext.StoreProductAccessories
            .AsNoTracking()
            .Join(
                dbContext.StoreAccessories.AsNoTracking(),
                link => link.IdAccessory,
                accessory => accessory.Id,
                (link, accessory) => new { link, accessory })
            .Where(linkAccessory => productIds.Contains(linkAccessory.link.IdProduct) && linkAccessory.accessory.IsActive)
            .OrderBy(linkAccessory => linkAccessory.link.SortOrder)
            .ThenBy(linkAccessory => linkAccessory.link.Id)
            .Select(linkAccessory => new
            {
                linkAccessory.link.IdProduct,
                linkAccessory.link.Id,
                AccessoryId = linkAccessory.accessory.Id,
                linkAccessory.accessory.Name,
                linkAccessory.link.IsRequired,
                linkAccessory.link.DisplayDiscountPercent
            })
            .ToListAsync(cancellationToken);
        var regularAccessoryPrices = await dbContext.StorePriceVersions
            .AsNoTracking()
            .Join(
                dbContext.SalesPlanReferences.AsNoTracking(),
                version => version.IdSalesPlan,
                plan => (long?)plan.Id,
                (version, plan) => new { version, plan })
            .Where(pricePlan => productIds.Contains(pricePlan.version.IdProduct) && pricePlan.version.IsActive &&
                                pricePlan.version.IdAccessory != null && pricePlan.plan.IsRegular &&
                                pricePlan.plan.IdStockOwnerType == 1)
            .OrderBy(pricePlan => pricePlan.plan.Id)
            .ThenByDescending(pricePlan => pricePlan.version.Id)
            .Select(pricePlan => pricePlan.version)
            .ToListAsync(cancellationToken);
        var regularAccessoryPriceByTarget = regularAccessoryPrices.GroupBy(x => (x.IdProduct, x.IdAccessory!.Value))
            .ToDictionary(x => x.Key, x => x.First().FinalPriceAmount);
        var productBenefits = await dbContext.StoreProductBenefits.AsNoTracking()
            .Where(x => productIds.Contains(x.IdProduct) && x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var collections = await dbContext.StoreProductCollections.AsNoTracking().OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        bool homeTabsReady = await dbContext.Database
            .SqlQueryRaw<int>(
                "SELECT CASE WHEN OBJECT_ID(N'Store.HomeCategoryTab', N'U') IS NULL THEN 0 ELSE 1 END AS [Value]")
            .SingleAsync(cancellationToken) == 1;
        var homeCategoryTabs = homeTabsReady
            ? await dbContext.StoreHomeCategoryTabs
                .AsNoTracking()
                .Join(
                    dbContext.StoreCategories.AsNoTracking(),
                    tab => tab.IdCategory,
                    category => category.Id,
                    (tab, category) => new { tab, category })
                .Where(tabCategory => tabCategory.tab.IsActive && tabCategory.category.IsActive)
                .OrderBy(tabCategory => tabCategory.tab.SortOrder)
                .ThenBy(tabCategory => tabCategory.tab.Id)
                .Select(tabCategory => new { tabCategory.category.CategoryKey, tabCategory.category.NameFa })
                .ToListAsync(cancellationToken)
            : new[] { "ابزار برقی", "ابزار اندازه‌گیری", "لحیم‌کاری و تعمیر برد", "روشنایی و لامپ" }
                .Select(name => categories.FirstOrDefault(category => category.IsActive && category.NameFa == name))
                .Where(category => category is not null)
                .Select(category => new { category!.CategoryKey, category.NameFa }).ToList();
        var collectionIds = collections.Select(x => x.Id).ToArray();
        var collectionItems = await dbContext.StoreProductCollectionItems
            .AsNoTracking()
            .Join(
                dbContext.Products.AsNoTracking(),
                item => item.IdProduct,
                product => product.Id,
                (item, product) => new { item, product })
            .Where(itemProduct => collectionIds.Contains(itemProduct.item.IdProductCollection) &&
                                  itemProduct.product.IsStoreEnabled)
            .OrderBy(itemProduct => itemProduct.item.SortOrder)
            .ThenBy(itemProduct => itemProduct.item.Id)
            .Select(itemProduct => new { itemProduct.item.IdProductCollection, itemProduct.item.IdProduct })
            .ToListAsync(cancellationToken);
        var countryCodeById = countries.ToDictionary(x => x.Id, x => x.CountryCode);
        var brandKeyById = brands.ToDictionary(x => x.Id, x => x.BrandKey);
        var categoryById = categories.ToDictionary(x => x.Id);
        var colorKeyById = colors.ToDictionary(x => x.Id, x => x.ColorKey);
        var groupKeyById = groups.ToDictionary(x => x.Id, x => x.GroupKey);
        var specCategoryById = specCategories.ToDictionary(x => x.Id);

        var root = new JsonObject { ["schemaVersion"] = 2 };
        root["homeCategoryTabs"] = new JsonArray(homeCategoryTabs
            .Select(tab => (JsonNode?)new JsonObject { ["id"] = tab.CategoryKey, ["name"] = tab.NameFa }).ToArray());
        root["catalogFeatureOptions"] = new JsonArray(catalogFeatureOptions.Select(x => (JsonNode?)new JsonObject
        {
            ["featureId"] = x.FeatureId, ["featureName"] = x.FeatureName, ["valueId"] = x.ValueId,
            ["valueName"] = x.ValueName, ["kind"] = x.Kind
        }).ToArray());
        root["specificationGroups"] = new JsonArray(groups.Select(x =>
            (JsonNode?)new JsonObject { ["id"] = x.GroupKey, ["name"] = x.Name, ["order"] = x.SortOrder }).ToArray());
        root["specificationCategories"] = new JsonArray(specCategories.Select(x => (JsonNode?)new JsonObject
        {
            ["id"] = x.CategoryKey, ["groupId"] = groupKeyById.GetValueOrDefault(x.IdSpecificationGroup),
            ["name"] = x.Name, ["order"] = x.SortOrder
        }).ToArray());
        root["specificationFeatures"] = new JsonArray(features.Select(x => (JsonNode?)new JsonObject
        {
            ["id"] = x.FeatureKey,
            ["categoryId"] = specCategoryById.GetValueOrDefault(x.IdSpecificationCategory)?.CategoryKey,
            ["label"] = x.Label, ["order"] = x.SortOrder
        }).ToArray());
        root["attributeDefinitions"] = new JsonObject(attributes.Select(x =>
        {
            var definition = new JsonObject
            {
                ["label"] = x.Label, ["icon"] = x.Icon ?? "", ["source"] = x.SourceKey ?? "",
                ["is_filterable"] = x.IsFilterable, ["is_highlight"] = x.IsHighlight
            };
            AddOptional(definition, "filterKey", x.FilterKey);

            return KeyValuePair.Create<string, JsonNode?>(x.AttributeKey, definition);
        }));
        root["brands"] = new JsonArray(brands.Select(x => (JsonNode?)new JsonObject
        {
            ["id"] = x.BrandKey, ["name"] = x.NameEn, ["fa"] = x.NameFa, ["logo"] = x.LogoPath,
            ["showInStore"] = x.ShowInStore,
            ["countryCode"] = x.IdCountry.HasValue ? countryCodeById.GetValueOrDefault(x.IdCountry.Value) : null
        }).ToArray());
        var activeCategories = categories.Where(x => x.IsActive).ToArray();
        var categoryKeyById = categories.ToDictionary(x => x.Id, x => x.CategoryKey);
        root["categories"] = new JsonArray(activeCategories.Select(x => (JsonNode?)new JsonObject
        {
            ["id"] = x.CategoryKey,
            ["parentId"] = x.IdParentCategory.HasValue
                ? categoryKeyById.GetValueOrDefault(x.IdParentCategory.Value)
                : null,
            ["en"] = x.NameEn, ["fa"] = x.NameFa, ["slug"] = x.CategoryKey, ["path"] = ParseJsonValue(x.PathJson)
        }).ToArray());
        root["colors"] = new JsonArray(colors.Select(x => (JsonNode?)new JsonObject
            { ["id"] = x.ColorKey, ["en"] = x.NameEn, ["fa"] = x.NameFa, ["hex"] = x.HexCode }).ToArray());
        root["countries"] = new JsonArray(countries.Select(x => (JsonNode?)new JsonObject
        {
            ["id"] = x.CountryCode, ["name"] = x.NameFa, ["fa"] = x.NameFa, ["en"] = x.NameEn, ["flag"] = x.FlagPath
        }).ToArray());
        root["details"] = new JsonArray(activeCategories.Select(x =>
        {
            var o = new JsonObject { ["id"] = x.CategoryKey };
            AddOptional(o, "intro", x.ProductIntro);
            if (x.ProductHighlightsJson is not null) o["highlights"] = ParseJsonValue(x.ProductHighlightsJson);
            return (JsonNode?)o;
        }).ToArray());
        root["media"] = new JsonArray(products.Select(product =>
        {
            var images = assets.Where(x => x.IdProduct == product.Id && x.IdMediaType == ProductMediaTypes.Image)
                .ToArray();
            var main = images.FirstOrDefault(x => x.IsDefault);
            return (JsonNode?)new JsonObject
            {
                ["id"] = "product-" + product.ProductKey, ["main"] = main?.Path, ["card"] = main?.CardPath,
                ["thumbnail"] = main?.ThumbnailPath,
                ["images"] = new JsonArray(images.Select(i =>
                    (JsonNode?)new JsonObject { ["src"] = i.Path, ["thumbnail"] = i.ThumbnailPath }).ToArray()),
                ["videos"] = new JsonArray(assets
                    .Where(v => v.IdProduct == product.Id && v.IdMediaType == ProductMediaTypes.Video)
                    .Select(v => (JsonNode?)JsonValue.Create(v.Path)).ToArray())
            };
        }).ToArray());
        root["products"] = new JsonArray(products.Select(product =>
        {
            brandKeyById.TryGetValue(product.IdBrand ?? 0, out var brandKey);
            categoryKeyById.TryGetValue(product.IdCategory ?? 0, out var categoryKey);
            decimal stock = inventoryTotals.GetValueOrDefault(product.Id);
            int stockQty = stock >= int.MaxValue ? int.MaxValue : Math.Max(0, (int)Math.Floor(stock));
            var badge = product.IdBadge is decimal badgeId ? badges.GetValueOrDefault(badgeId) : null;
            var regularPrice = regularPriceByProduct.GetValueOrDefault(product.Id);
            var o = new JsonObject
            {
                ["id"] = checked((int)product.Id), ["name"] = product.Name,
                ["englishName"] = product.EnglishName ?? product.Name, ["code"] = product.ProductKey,
                ["slug"] = product.Slug, ["brandId"] = brandKey, ["categoryId"] = categoryKey,
                ["detailId"] = categoryKey, ["mediaId"] = "product-" + product.ProductKey,
                ["priceBeforeDiscountAmount"] = regularPrice?.PriceBeforeDiscountAmount ?? 0m,
                ["price"] = regularPrice is null ? 0m : regularPrice.FinalPriceAmount / 1_000_000m,
                ["finalPriceAmount"] = regularPrice?.FinalPriceAmount ?? 0m, ["stockQuantity"] = stockQty,
                ["maxCartQuantity"] = product.MaxPurchaseQuantity ?? 99,
                ["discountPercent"] = regularPrice?.DiscountPercent ?? 0m, ["badge"] = badge?.Title,
                ["badgeColor"] = badge?.BackgroundColor, ["badgeTextColor"] = badge?.TextColor,
                ["badgeBorderColor"] = badge?.BorderColor
            };
            o["colorIds"] = new JsonArray(productColors.Where(x => x.IdProduct == product.Id)
                .Select(x => (JsonNode?)JsonValue.Create(colorKeyById.GetValueOrDefault(x.IdColor))).ToArray());
            o["specificationValues"] = new JsonArray();
            o["catalogFeatures"] = new JsonArray(catalogFeatureValues.Where(x => x.IdProduct == product.Id)
                .Select(x => (JsonNode?)new JsonObject
                {
                    ["featureId"] = x.FeatureId, ["featureName"] = x.FeatureName, ["valueId"] = x.ValueId,
                    ["valueName"] = x.ValueName, ["kind"] = 2
                }).Concat(
                    variantFeatureValues.Where(x => x.IdProduct == product.Id).Select(x => (JsonNode?)new JsonObject
                    {
                        ["featureId"] = x.FeatureId, ["featureName"] = x.FeatureName, ["valueId"] = x.ValueId,
                        ["valueName"] = x.ValueName, ["kind"] = 1
                    })).ToArray());
            o["includedBenefits"] = new JsonArray(productBenefits.Where(x => x.IdProduct == product.Id).Select(x =>
                (JsonNode?)new JsonObject
                {
                    ["id"] = x.Id,
                    ["title"] = x.Title,
                    ["detail"] = x.Detail,
                    ["icon"] = x.Icon
                }).ToArray());
            o["accessories"] = new JsonArray(productAccessories.Where(x => x.IdProduct == product.Id).Select(x =>
                (JsonNode?)new JsonObject
                {
                    ["id"] = x.AccessoryId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["productAccessoryId"] = x.Id, ["name"] = x.Name, ["required"] = x.IsRequired,
                    ["displayDiscountPercent"] = x.DisplayDiscountPercent,
                    ["price"] = regularAccessoryPriceByTarget.GetValueOrDefault((x.IdProduct, x.AccessoryId)) /
                                1_000_000m
                }).ToArray());
            o["documents"] = new JsonArray(assets
                .Where(x => x.IdProduct == product.Id && x.IdMediaType == ProductMediaTypes.Document)
                .Select(x => (JsonNode?)new JsonObject { ["title"] = x.Title, ["path"] = x.Path }).ToArray());
            o["variants"] = new JsonObject();
            return (JsonNode?)o;
        }).ToArray());
        root["collections"] = new JsonArray(collections.Select(collection => (JsonNode?)new JsonObject
        {
            ["id"] = collection.CollectionKey,
            ["items"] = new JsonArray(collectionItems.Where(x => x.IdProductCollection == collection.Id)
                .Select(x => (JsonNode?)JsonValue.Create(checked((int)x.IdProduct))).ToArray())
        }).ToArray());

        var activeBrandKeys = await dbContext.StoreBrands.AsNoTracking().Where(x => x.IsActive).Select(x => x.BrandKey)
            .ToHashSetAsync(cancellationToken);
        var activeAttributeKeys = await dbContext.StoreAttributeDefinitions.AsNoTracking().Where(x => x.IsActive)
            .Select(x => x.AttributeKey).ToHashSetAsync(cancellationToken);
        FilterArray(root, "brands", item => activeBrandKeys.Contains(item?["id"]?.GetValue<string>() ?? ""));
        if (root["attributeDefinitions"] is JsonObject attributeDefinitions)
            foreach (string key in attributeDefinitions.Select(pair => pair.Key)
                         .Where(key => !activeAttributeKeys.Contains(key)).ToArray())
                attributeDefinitions.Remove(key);
        FilterArray(root, "products",
            item => activeBrandKeys.Contains(item?["brandId"]?.GetValue<string>() ?? "") &&
                    (item?["categoryId"] is null || root["categories"]?.AsArray().Any(category =>
                        string.Equals(category?["id"]?.GetValue<string>(), item["categoryId"]?.GetValue<string>(),
                            StringComparison.Ordinal)) == true));
        var visibleProductIds = (root["products"]?.AsArray() ?? []).Select(item => item?["id"]?.ToJsonString())
            .ToHashSet(StringComparer.Ordinal);
        if (root["collections"] is JsonArray collectionArray)
            foreach (var collection in collectionArray)
                if (collection?["items"] is JsonArray items)
                    for (int index = items.Count - 1; index >= 0; index--)
                        if (!visibleProductIds.Contains(items[index]?.ToJsonString()))
                            items.RemoveAt(index);

        return ContentCdn.RewriteJson(root.ToJsonString(), await mediaCdn.GetBaseUrlAsync(cancellationToken));
    }

    private static void AddOptional(JsonObject target, string name, string? value)
    {
        if (value is not null) target[name] = value;
    }

    private static JsonNode? ParseJsonValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        try
        {
            return JsonNode.Parse(value);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static JsonNode ParseJsonArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new JsonArray();

        try
        {
            return JsonNode.Parse(value) ?? new JsonArray();
        }
        catch (System.Text.Json.JsonException)
        {
            return new JsonArray();
        }
    }

    private static void FilterArray(JsonObject root, string name, Func<JsonNode?, bool> keep)
    {
        if (root[name] is not JsonArray items) return;

        for (int index = items.Count - 1; index >= 0; index--)
            if (!keep(items[index]))
                items.RemoveAt(index);
    }

    public async Task<string?> ReadBrandEditorialJsonAsync(CancellationToken cancellationToken)
    {
        var rows = await dbContext.StoreBrands
            .AsNoTracking()
            .Join(
                dbContext.StoreBrandEditorials.AsNoTracking(),
                brand => brand.Id,
                editorial => editorial.IdBrand,
                (brand, editorial) => new { brand, editorial })
            .Where(brandEditorial => brandEditorial.brand.IsActive)
            .OrderBy(brandEditorial => brandEditorial.brand.Id)
            .Select(brandEditorial => new
            {
                brandEditorial.brand.BrandKey,
                brandEditorial.editorial.Established,
                brandEditorial.editorial.OfficialUrl,
                brandEditorial.editorial.Specialization,
                brandEditorial.editorial.FeaturedProducts,
                brandEditorial.editorial.Audience,
                brandEditorial.editorial.ShoppingAdvice,
                brandEditorial.editorial.CompanyIntroductionsJson,
                brandEditorial.editorial.ProductFamiliesJson,
                brandEditorial.editorial.StrengthsJson
            })
            .ToListAsync(cancellationToken);
        var items = new JsonArray();
        foreach (var row in rows)
        {
            var item = new JsonObject { ["slug"] = row.BrandKey };
            AddOptional(item, "established", row.Established);
            AddOptional(item, "officialUrl", row.OfficialUrl);
            AddOptional(item, "specialization", row.Specialization);
            AddOptional(item, "featuredProducts", row.FeaturedProducts);
            AddOptional(item, "audience", row.Audience);
            AddOptional(item, "shoppingAdvice", row.ShoppingAdvice);
            item["companyIntroductions"] = ParseJsonArray(row.CompanyIntroductionsJson);
            item["productFamilies"] = ParseJsonArray(row.ProductFamiliesJson);
            item["strengths"] = ParseJsonArray(row.StrengthsJson);
            items.Add(item);
        }

        return ContentCdn.RewriteJson(items.ToJsonString(), await mediaCdn.GetBaseUrlAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<StoreFaqRow>> ReadFaqAsync(CancellationToken cancellationToken)
    {
        return await dbContext.StoreFaqItems
            .AsNoTracking()
            .Join(
                dbContext.StoreContentPages.AsNoTracking(),
                faq => faq.IdContentPage,
                page => page.Id,
                (faq, page) => new { faq, page })
            .Where(faqPage => faqPage.page.PageKey == "faq" && faqPage.page.IsPublished && faqPage.faq.IsPublished)
            .OrderBy(faqPage => faqPage.faq.SortOrder)
            .ThenBy(faqPage => faqPage.faq.Id)
            .Select(faqPage => new StoreFaqRow(
                faqPage.faq.Question,
                faqPage.faq.Answer,
                faqPage.faq.LinkText,
                faqPage.faq.LinkUrl,
                faqPage.faq.SortOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> ReadAccountJsonAsync(CancellationToken cancellationToken)
    {
        var coupons = await dbContext.AccountCoupons.AsNoTracking().Where(x => x.IdProfile == null).OrderBy(x => x.Id)
            .Select(x => new
            {
                id = x.CouponKey, x.Code, x.DiscountPercent, x.UsageLimit, enabled = x.Enabled, x.ExpiresOn,
                orderIds = Array.Empty<string>()
            })
            .ToListAsync(cancellationToken);

        return System.Text.Json.JsonSerializer.Serialize(
            new { schemaVersion = 1, currency = "IRT", orders = Array.Empty<object>(), coupons },
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
    }

    public async Task<string?> ReadMagazineJsonAsync(CancellationToken cancellationToken)
    {
        var articles = await dbContext.MagazineArticles
            .AsNoTracking()
            .Join(
                dbContext.MagazineArticleCategories.AsNoTracking(),
                article => article.IdCategory,
                category => category.Id,
                (article, category) => new { article, category })
            .Join(
                dbContext.MagazineArticleBranches.AsNoTracking(),
                articleCategory => articleCategory.article.IdBranch,
                branch => branch.Id,
                (articleCategory, branch) => new { articleCategory.article, articleCategory.category, branch })
            .Join(
                dbContext.MagazineArticleTopics.AsNoTracking(),
                articleCategoryBranch => articleCategoryBranch.article.IdTopic,
                topic => topic.Id,
                (articleCategoryBranch, topic) => new
                {
                    articleCategoryBranch.article,
                    articleCategoryBranch.category,
                    articleCategoryBranch.branch,
                    topic
                })
            .Join(
                dbContext.MagazineArticleAreas.AsNoTracking(),
                articleCategoryBranchTopic => articleCategoryBranchTopic.article.IdArea,
                area => area.Id,
                (articleCategoryBranchTopic, area) => new
                {
                    articleCategoryBranchTopic.article,
                    articleCategoryBranchTopic.category,
                    articleCategoryBranchTopic.branch,
                    articleCategoryBranchTopic.topic,
                    area
                })
            .Where(articleDetails => articleDetails.article.IsPublished)
            .OrderBy(articleDetails => articleDetails.article.Id)
            .Select(articleDetails => new
            {
                articleDetails.article.Id,
                articleDetails.article.ArticleKey,
                Category = articleDetails.category.CategoryKey,
                Branch = articleDetails.branch.BranchKey,
                Topic = articleDetails.topic.TopicKey,
                Area = articleDetails.area.AreaKey,
                articleDetails.article.ReadTimeMinutes,
                articleDetails.article.Kicker,
                articleDetails.article.Title,
                articleDetails.article.Summary,
                articleDetails.article.Author,
                articleDetails.article.ReadTimeText,
                articleDetails.article.HeroImagePath,
                articleDetails.article.ContentImagePath,
                articleDetails.article.Lead,
                articleDetails.article.Conclusion
            })
            .ToListAsync(cancellationToken);
        var articleIds = articles.Select(x => x.Id).ToArray();
        var sections = await dbContext.MagazineArticleSections.AsNoTracking()
            .Where(x => articleIds.Contains(x.IdMagazineArticle)).OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var sources = await dbContext.MagazineArticleSources.AsNoTracking()
            .Where(x => articleIds.Contains(x.IdMagazineArticle)).OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var tags = await dbContext.MagazineArticleTags.AsNoTracking()
            .Where(x => articleIds.Contains(x.IdMagazineArticle)).OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var related = await dbContext.MagazineArticleRelated
            .AsNoTracking()
            .Join(
                dbContext.MagazineArticles.AsNoTracking(),
                link => link.IdRelatedArticle,
                relatedArticle => relatedArticle.Id,
                (link, relatedArticle) => new { link, relatedArticle })
            .Join(
                dbContext.MagazineArticleCategories.AsNoTracking(),
                linkArticle => linkArticle.relatedArticle.IdCategory,
                relatedCategory => relatedCategory.Id,
                (linkArticle, relatedCategory) => new { linkArticle.link, linkArticle.relatedArticle, relatedCategory })
            .Where(relatedArticle => articleIds.Contains(relatedArticle.link.IdMagazineArticle))
            .OrderBy(relatedArticle => relatedArticle.link.SortOrder)
            .ThenBy(relatedArticle => relatedArticle.link.Id)
            .Select(relatedArticle => new
                {
                    relatedArticle.link.IdMagazineArticle,
                    Slug = relatedArticle.relatedArticle.ArticleKey,
                    Category = relatedArticle.relatedCategory.CategoryKey,
                    relatedArticle.relatedArticle.Title,
                    Image = relatedArticle.relatedArticle.HeroImagePath
                })
            .ToListAsync(cancellationToken);
        var metrics = await dbContext.MagazineArticleMetrics.AsNoTracking()
            .Where(x => articleIds.Contains(x.IdMagazineArticle))
            .ToDictionaryAsync(x => x.IdMagazineArticle, cancellationToken);
        var payload = articles.Select(article => new
        {
            article.Branch,
            article.Topic,
            article.Area,
            readMinutes = article.ReadTimeMinutes ?? 0,
            article = new
            {
                slug = article.ArticleKey,
                category = article.Category,
                article.Kicker,
                article.Title,
                article.Summary,
                author = article.Author ?? "",
                publishedAt = "",
                readTime = article.ReadTimeText ?? "",
                heroImage = article.HeroImagePath,
                contentImage = article.ContentImagePath,
                article.Lead,
                sections = sections.Where(x => x.IdMagazineArticle == article.Id)
                    .Select(x => new { id = x.Id.ToString(), title = x.Heading, content = x.Body }).ToArray(),
                article.Conclusion,
                sources = sources.Where(x => x.IdMagazineArticle == article.Id).Select(x => new { x.Title, x.Url })
                    .ToArray(),
                tags = tags.Where(x => x.IdMagazineArticle == article.Id).Select(x => new { x.Label, x.Filter })
                    .ToArray(),
                relatedArticles = related.Where(x => x.IdMagazineArticle == article.Id)
                    .Select(x => new { slug = x.Slug, category = x.Category, x.Title, image = x.Image }).ToArray(),
                metrics = metrics.TryGetValue(article.Id, out var metric)
                    ? new
                    {
                        views = metric.Views, likes = metric.Likes, averageRating = metric.AverageRating,
                        comments = metric.Comments
                    }
                    : new { views = 0, likes = 0, averageRating = 0m, comments = 0 }
            }
        }).ToArray();
        string json = System.Text.Json.JsonSerializer.Serialize(new { articles = payload },
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

        return ContentCdn.RewriteJson(json, await mediaCdn.GetBaseUrlAsync(cancellationToken));
    }

    public async Task<string> ReadGeographyJsonAsync(CancellationToken cancellationToken)
    {
        var countries = await dbContext.GeoCountries
            .AsNoTracking()
            .Where(country => dbContext.GeoProvinces.Any(province => province.IdCountry == country.Id))
            .OrderBy(country => country.Id)
            .Select(country => new
            {
                id = country.Id,
                name = country.NameFa,
                nameEn = country.NameEn,
                countryCode = country.CountryCode
            })
            .ToListAsync(cancellationToken);
        var provinces = await dbContext.GeoProvinces.AsNoTracking().OrderBy(x => x.NameFa)
            .Select(x => new
            {
                id = x.Id, countryId = x.IdCountry, name = x.NameFa, nameEn = x.NameEn, latitude = x.Latitude,
                longitude = x.Longitude
            }).ToListAsync(cancellationToken);
        var cities = await dbContext.GeoCities.AsNoTracking().OrderBy(x => x.IdProvince).ThenBy(x => x.NameFa)
            .Select(x => new
            {
                id = x.Id, countryId = x.IdCountry, provinceId = x.IdProvince, sourceCountryName = x.SourceCountryName,
                name = x.NameFa, nameEn = x.NameEn, latitude = x.Latitude, longitude = x.Longitude,
                isProvinceCenter = x.IsProvinceCenter
            }).ToListAsync(cancellationToken);
        string json = System.Text.Json.JsonSerializer.Serialize(new { countries, provinces, cities },
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        return ContentCdn.RewriteJson(json, await mediaCdn.GetBaseUrlAsync(cancellationToken))!;
    }
}
