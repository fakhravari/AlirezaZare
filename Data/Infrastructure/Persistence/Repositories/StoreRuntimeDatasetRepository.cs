using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Data.Infrastructure.Persistence.Repositories;

public sealed class StoreRuntimeDatasetRepository(ShopDbContext dbContext, IMediaCdnBaseUrlProvider mediaCdn) : IStoreRuntimeDatasetRepository
{
    public async Task<string?> ReadCatalogJsonAsync(CancellationToken cancellationToken)
    {
        var groups = await dbContext.StoreSpecificationGroups.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var specCategories = await dbContext.StoreSpecificationCategories.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var features = await dbContext.StoreSpecificationFeatures.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var attributes = await dbContext.StoreAttributeDefinitions.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var brands = await dbContext.StoreBrands.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var countries = await dbContext.GeoCountries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var categories = await dbContext.StoreCategories.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var colors = await dbContext.StoreColors.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var media = await dbContext.StoreProductMedia.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var products = await dbContext.Products.AsNoTracking().Where(x => x.IsCatalogActive && x.IsStoreEnabled).OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var productIds = products.Select(x => x.Id).ToArray();
        var inventoryTotals = await (from inventory in dbContext.InventoryItems.AsNoTracking()
                                     join balance in dbContext.StockBalances.AsNoTracking() on inventory.Id equals balance.IdInventoryItem
                                     where productIds.Contains(inventory.IdProduct)
                                     group balance.Quantity by inventory.IdProduct into grouped
                                     select new { IdProduct = grouped.Key, Quantity = grouped.Sum() }).ToDictionaryAsync(x => x.IdProduct, x => x.Quantity, cancellationToken);
        var productColors = await dbContext.StoreProductColors.AsNoTracking().Where(x => productIds.Contains(x.IdProduct)).ToListAsync(cancellationToken);
        var productSpecs = await (from value in dbContext.StoreProductSpecificationValues.AsNoTracking()
                                  join feature in dbContext.StoreSpecificationFeatures.AsNoTracking() on value.IdSpecificationFeature equals feature.Id
                                  where productIds.Contains(value.IdProduct)
                                  select new { value.IdProduct, feature.FeatureKey, value.Value }).ToListAsync(cancellationToken);
        var productAccessories = await (from link in dbContext.StoreProductAccessories.AsNoTracking()
                                        join accessory in dbContext.StoreAccessories.AsNoTracking() on link.IdAccessory equals accessory.Id
                                        where productIds.Contains(link.IdProduct)
                                        orderby link.SortOrder, link.Id
                                        select new { link.IdProduct, link.Id, accessory.AccessoryKey, accessory.Name, link.IsRequired, link.PriceAmount }).ToListAsync(cancellationToken);
        var documents = await dbContext.StoreProductDocuments.AsNoTracking().Where(x => productIds.Contains(x.IdProduct)).OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var collections = await dbContext.StoreProductCollections.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var collectionIds = collections.Select(x => x.Id).ToArray();
        var collectionItems = await (from item in dbContext.StoreProductCollectionItems.AsNoTracking()
                                     join product in dbContext.Products.AsNoTracking() on item.IdProduct equals product.Id
                                     where collectionIds.Contains(item.IdProductCollection) && product.IsCatalogActive && product.IsStoreEnabled
                                     orderby item.SortOrder, item.Id
                                     select new { item.IdProductCollection, item.IdProduct }).ToListAsync(cancellationToken);
        var countryCodeById = countries.ToDictionary(x => x.Id, x => x.CountryCode);
        var brandKeyById = brands.ToDictionary(x => x.Id, x => x.BrandKey);
        var categoryById = categories.ToDictionary(x => x.Id);
        var colorKeyById = colors.ToDictionary(x => x.Id, x => x.ColorKey);
        var groupKeyById = groups.ToDictionary(x => x.Id, x => x.GroupKey);
        var specCategoryById = specCategories.ToDictionary(x => x.Id);

        var root = new JsonObject { ["schemaVersion"] = 2 };
        root["specificationGroups"] = new JsonArray(groups.Select(x => (JsonNode?)new JsonObject { ["id"] = x.GroupKey, ["name"] = x.Name, ["order"] = x.SortOrder }).ToArray());
        root["specificationCategories"] = new JsonArray(specCategories.Select(x => (JsonNode?)new JsonObject { ["id"] = x.CategoryKey, ["groupId"] = groupKeyById.GetValueOrDefault(x.IdSpecificationGroup), ["name"] = x.Name, ["order"] = x.SortOrder }).ToArray());
        root["specificationFeatures"] = new JsonArray(features.Select(x => (JsonNode?)new JsonObject { ["id"] = x.FeatureKey, ["categoryId"] = specCategoryById.GetValueOrDefault(x.IdSpecificationCategory)?.CategoryKey, ["label"] = x.Label, ["order"] = x.SortOrder }).ToArray());
        root["attributeDefinitions"] = new JsonObject(attributes.Select(x =>
        {
            var definition = new JsonObject { ["label"] = x.Label, ["icon"] = x.Icon ?? "", ["source"] = x.SourceKey ?? "", ["is_filterable"] = x.IsFilterable, ["is_highlight"] = x.IsHighlight };
            AddOptional(definition, "filterKey", x.FilterKey);
            return KeyValuePair.Create<string, JsonNode?>(x.AttributeKey, definition);
        }));
        root["brands"] = new JsonArray(brands.Select(x => (JsonNode?)new JsonObject { ["id"] = x.BrandKey, ["name"] = x.NameEn, ["fa"] = x.NameFa, ["logo"] = x.LogoPath, ["countryCode"] = x.IdCountry.HasValue ? countryCodeById.GetValueOrDefault(x.IdCountry.Value) : null }).ToArray());
        var activeCategories = categories.Where(x => x.IsActive).ToArray();
        var categoryKeyById = categories.ToDictionary(x => x.Id, x => x.CategoryKey);
        root["categories"] = new JsonArray(activeCategories.Select(x => (JsonNode?)new JsonObject { ["id"] = x.CategoryKey, ["parentId"] = x.IdParentCategory.HasValue ? categoryKeyById.GetValueOrDefault(x.IdParentCategory.Value) : null, ["en"] = x.NameEn, ["fa"] = x.NameFa, ["slug"] = x.CategoryKey, ["path"] = ParseJsonValue(x.PathJson) }).ToArray());
        root["colors"] = new JsonArray(colors.Select(x => (JsonNode?)new JsonObject { ["id"] = x.ColorKey, ["en"] = x.NameEn, ["fa"] = x.NameFa, ["hex"] = x.HexCode }).ToArray());
        root["countries"] = new JsonArray(countries.Select(x => (JsonNode?)new JsonObject { ["id"] = x.CountryCode, ["name"] = x.NameFa, ["fa"] = x.NameFa, ["en"] = x.NameEn, ["flag"] = x.FlagPath }).ToArray());
        root["details"] = new JsonArray(activeCategories.Select(x => { var o = new JsonObject { ["id"] = x.CategoryKey }; AddOptional(o, "intro", x.ProductIntro); if (x.ProductHighlightsJson is not null) o["highlights"] = ParseJsonValue(x.ProductHighlightsJson); if (x.ProductSpecsJson is not null) o["specs"] = ParseJsonValue(x.ProductSpecsJson); return (JsonNode?)o; }).ToArray());
        root["media"] = new JsonArray(media.Select(x => (JsonNode?)new JsonObject { ["id"] = x.MediaKey, ["main"] = x.MainPath, ["card"] = x.CardPath, ["thumbnail"] = x.ThumbnailPath, ["images"] = ParseJsonArray(x.ImagesJson), ["videos"] = ParseJsonArray(x.VideosJson) }).ToArray());
        var mediaByKey = media.ToDictionary(x => (x.IdProduct, x.MediaKey));
        root["products"] = new JsonArray(products.Select(product =>
        {
            brandKeyById.TryGetValue(product.IdBrand ?? 0, out var brandKey); categoryKeyById.TryGetValue(product.IdCategory ?? 0, out var categoryKey);
            mediaByKey.TryGetValue((product.Id, "product-" + product.ProductKey), out var productMedia);
            decimal stock = inventoryTotals.GetValueOrDefault(product.Id);
            int stockQty = stock >= int.MaxValue ? int.MaxValue : Math.Max(0, (int)Math.Floor(stock));
            var o = new JsonObject { ["id"] = checked((int)product.Id), ["name"] = product.Name, ["englishName"] = product.EnglishName ?? product.Name, ["code"] = product.Code, ["slug"] = product.Slug, ["brandId"] = brandKey, ["categoryId"] = categoryKey, ["detailId"] = categoryKey, ["mediaId"] = productMedia?.MediaKey, ["powerSource"] = product.PowerSource ?? "", ["useCase"] = product.UseCase ?? "", ["price"] = (product.PriceAmount ?? 0m) / 1_000_000m, ["stockQuantity"] = stockQty, ["maxCartQuantity"] = product.MaxPurchaseQuantity ?? 99, ["discountPercent"] = product.DiscountPercent ?? 0m, ["badge"] = product.Badge, ["isNewest"] = product.IsNewest, ["newestOrder"] = product.NewestOrder };
            o["colorIds"] = new JsonArray(productColors.Where(x => x.IdProduct == product.Id).Select(x => (JsonNode?)JsonValue.Create(colorKeyById.GetValueOrDefault(x.IdColor))).ToArray());
            o["specificationValues"] = new JsonArray(productSpecs.Where(x => x.IdProduct == product.Id).Select(x => (JsonNode?)new JsonObject { ["featureId"] = x.FeatureKey, ["value"] = x.Value }).ToArray());
            o["accessories"] = new JsonArray(productAccessories.Where(x => x.IdProduct == product.Id).Select(x => (JsonNode?)new JsonObject { ["id"] = x.AccessoryKey, ["productAccessoryId"] = x.Id, ["name"] = x.Name, ["required"] = x.IsRequired, ["price"] = x.PriceAmount / 1_000_000m }).ToArray());
            o["documents"] = new JsonArray(documents.Where(x => x.IdProduct == product.Id).Select(x => (JsonNode?)new JsonObject { ["title"] = x.Title, ["path"] = x.Path }).ToArray()); o["variants"] = new JsonObject(); return (JsonNode?)o;
        }).ToArray());
        root["collections"] = new JsonArray(collections.Select(collection => (JsonNode?)new JsonObject { ["id"] = collection.CollectionKey, ["items"] = new JsonArray(collectionItems.Where(x => x.IdProductCollection == collection.Id).Select(x => (JsonNode?)JsonValue.Create(checked((int)x.IdProduct))).ToArray()) }).ToArray());

        var activeBrandKeys = await dbContext.StoreBrands.AsNoTracking().Where(x => x.IsActive).Select(x => x.BrandKey).ToHashSetAsync(cancellationToken);
        var activeAttributeKeys = await dbContext.StoreAttributeDefinitions.AsNoTracking().Where(x => x.IsActive).Select(x => x.AttributeKey).ToHashSetAsync(cancellationToken);
        FilterArray(root, "brands", item => activeBrandKeys.Contains(item?["id"]?.GetValue<string>() ?? ""));
        if (root["attributeDefinitions"] is JsonObject attributeDefinitions)
            foreach (string key in attributeDefinitions.Select(pair => pair.Key).Where(key => !activeAttributeKeys.Contains(key)).ToArray())
                attributeDefinitions.Remove(key);
        FilterArray(root, "products", item => activeBrandKeys.Contains(item?["brandId"]?.GetValue<string>() ?? "") && (item?["categoryId"] is null || root["categories"]?.AsArray().Any(category => string.Equals(category?["id"]?.GetValue<string>(), item["categoryId"]?.GetValue<string>(), StringComparison.Ordinal)) == true));
        var visibleProductIds = (root["products"]?.AsArray() ?? []).Select(item => item?["id"]?.ToJsonString()).ToHashSet(StringComparer.Ordinal);
        if (root["collections"] is JsonArray collectionArray) foreach (var collection in collectionArray) if (collection?["items"] is JsonArray items) for (int index = items.Count - 1; index >= 0; index--) if (!visibleProductIds.Contains(items[index]?.ToJsonString())) items.RemoveAt(index);
        return ContentCdn.RewriteJson(root.ToJsonString(), await mediaCdn.GetBaseUrlAsync(cancellationToken));
    }

    private static void AddOptional(JsonObject target, string name, string? value)
    {
        if (value is not null) target[name] = value;
    }

    private static JsonNode? ParseJsonValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return JsonNode.Parse(value); } catch (System.Text.Json.JsonException) { return null; }
    }

    private static JsonNode ParseJsonArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new JsonArray();
        try { return JsonNode.Parse(value) ?? new JsonArray(); }
        catch (System.Text.Json.JsonException) { return new JsonArray(); }
    }

    private static void FilterArray(JsonObject root, string name, Func<JsonNode?, bool> keep)
    {
        if (root[name] is not JsonArray items) return;
        for (int index = items.Count - 1; index >= 0; index--)
            if (!keep(items[index])) items.RemoveAt(index);
    }

    public async Task<string?> ReadBrandEditorialJsonAsync(CancellationToken cancellationToken)
    {
        var rows = await (from brand in dbContext.StoreBrands.AsNoTracking()
                          join editorial in dbContext.StoreBrandEditorials.AsNoTracking() on brand.Id equals editorial.IdBrand
                          orderby brand.Id
                          select new { brand.BrandKey, editorial.Established, editorial.OfficialUrl, editorial.Specialization, editorial.FeaturedProducts, editorial.Audience, editorial.ShoppingAdvice, editorial.CompanyIntroductionsJson, editorial.ProductFamiliesJson, editorial.StrengthsJson }).ToListAsync(cancellationToken);
        var items = new JsonArray();
        foreach (var row in rows)
        {
            var item = new JsonObject { ["slug"] = row.BrandKey };
            AddOptional(item, "established", row.Established); AddOptional(item, "officialUrl", row.OfficialUrl);
            AddOptional(item, "specialization", row.Specialization); AddOptional(item, "featuredProducts", row.FeaturedProducts);
            AddOptional(item, "audience", row.Audience); AddOptional(item, "shoppingAdvice", row.ShoppingAdvice);
            item["companyIntroductions"] = ParseJsonArray(row.CompanyIntroductionsJson);
            item["productFamilies"] = ParseJsonArray(row.ProductFamiliesJson); item["strengths"] = ParseJsonArray(row.StrengthsJson);
            items.Add(item);
        }
        return ContentCdn.RewriteJson(items.ToJsonString(), await mediaCdn.GetBaseUrlAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<StoreFaqRow>> ReadFaqAsync(CancellationToken cancellationToken)
    {
        return await (
            from faq in dbContext.StoreFaqItems.AsNoTracking()
            join page in dbContext.StoreContentPages.AsNoTracking()
                on faq.IdContentPage equals page.Id
            where page.PageKey == "faq" && page.IsPublished && faq.IsPublished
            orderby faq.SortOrder, faq.Id
            select new StoreFaqRow(faq.Question, faq.Answer, faq.LinkText, faq.LinkUrl, faq.SortOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> ReadAccountJsonAsync(CancellationToken cancellationToken)
    {
        var coupons = await dbContext.AccountCoupons.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new { id = x.CouponKey, x.Code, x.DiscountPercent, x.UsageLimit, enabled = x.Enabled, x.ExpiresOn, orderIds = Array.Empty<string>() })
            .ToListAsync(cancellationToken);
        return System.Text.Json.JsonSerializer.Serialize(new { schemaVersion = 1, currency = "IRT", orders = Array.Empty<object>(), coupons }, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
    }

    public async Task<string?> ReadMagazineJsonAsync(CancellationToken cancellationToken)
    {
        var articles = await (from article in dbContext.MagazineArticles.AsNoTracking()
                              join category in dbContext.MagazineArticleCategories.AsNoTracking() on article.IdCategory equals category.Id
                              join branch in dbContext.MagazineArticleBranches.AsNoTracking() on article.IdBranch equals branch.Id
                              join topic in dbContext.MagazineArticleTopics.AsNoTracking() on article.IdTopic equals topic.Id
                              join area in dbContext.MagazineArticleAreas.AsNoTracking() on article.IdArea equals area.Id
                              where article.IsPublished
                              orderby article.Id
                              select new { article.Id, article.ArticleKey, Category = category.CategoryKey, Branch = branch.BranchKey, Topic = topic.TopicKey, Area = area.AreaKey, article.ReadTimeMinutes, article.Kicker, article.Title, article.Summary, article.Author, article.ReadTimeText, article.HeroImagePath, article.ContentImagePath, article.Lead, article.Conclusion }).ToListAsync(cancellationToken);
        var articleIds = articles.Select(x => x.Id).ToArray();
        var sections = await dbContext.MagazineArticleSections.AsNoTracking().Where(x => articleIds.Contains(x.IdMagazineArticle)).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var sources = await dbContext.MagazineArticleSources.AsNoTracking().Where(x => articleIds.Contains(x.IdMagazineArticle)).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var tags = await dbContext.MagazineArticleTags.AsNoTracking().Where(x => articleIds.Contains(x.IdMagazineArticle)).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var related = await (from link in dbContext.MagazineArticleRelated.AsNoTracking()
                             join relatedArticle in dbContext.MagazineArticles.AsNoTracking() on link.IdRelatedArticle equals relatedArticle.Id
                             join relatedCategory in dbContext.MagazineArticleCategories.AsNoTracking() on relatedArticle.IdCategory equals relatedCategory.Id
                             where articleIds.Contains(link.IdMagazineArticle)
                             orderby link.SortOrder, link.Id
                             select new { link.IdMagazineArticle, Slug = relatedArticle.ArticleKey, Category = relatedCategory.CategoryKey, relatedArticle.Title, Image = relatedArticle.HeroImagePath })
            .ToListAsync(cancellationToken);
        var metrics = await dbContext.MagazineArticleMetrics.AsNoTracking().Where(x => articleIds.Contains(x.IdMagazineArticle)).ToDictionaryAsync(x => x.IdMagazineArticle, cancellationToken);
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
                sections = sections.Where(x => x.IdMagazineArticle == article.Id).Select(x => new { id = x.Id.ToString(), title = x.Heading, content = x.Body }).ToArray(),
                article.Conclusion,
                sources = sources.Where(x => x.IdMagazineArticle == article.Id).Select(x => new { x.Title, x.Url }).ToArray(),
                tags = tags.Where(x => x.IdMagazineArticle == article.Id).Select(x => new { x.Label, x.Filter }).ToArray(),
                relatedArticles = related.Where(x => x.IdMagazineArticle == article.Id).Select(x => new { slug = x.Slug, category = x.Category, x.Title, image = x.Image }).ToArray(),
                metrics = metrics.TryGetValue(article.Id, out var metric) ? new { views = metric.Views, likes = metric.Likes, averageRating = metric.AverageRating, comments = metric.Comments } : new { views = 0, likes = 0, averageRating = 0m, comments = 0 }
            }
        }).ToArray();
        string json = System.Text.Json.JsonSerializer.Serialize(new { articles = payload }, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        return ContentCdn.RewriteJson(json, await mediaCdn.GetBaseUrlAsync(cancellationToken));
    }

    public async Task<string> ReadGeographyJsonAsync(CancellationToken cancellationToken)
    {
        var countries = await (from country in dbContext.GeoCountries.AsNoTracking()
                               where dbContext.GeoProvinces.Any(province => province.IdCountry == country.Id)
                               orderby country.Id
                               select new { id = country.Id, name = country.NameFa, nameEn = country.NameEn, countryCode = country.CountryCode }).ToListAsync(cancellationToken);
        var provinces = await dbContext.GeoProvinces.AsNoTracking().OrderBy(x => x.NameFa)
            .Select(x => new { id = x.Id, countryId = x.IdCountry, name = x.NameFa, nameEn = x.NameEn, latitude = x.Latitude, longitude = x.Longitude }).ToListAsync(cancellationToken);
        var cities = await dbContext.GeoCities.AsNoTracking().OrderBy(x => x.IdProvince).ThenBy(x => x.NameFa)
            .Select(x => new { id = x.Id, countryId = x.IdCountry, provinceId = x.IdProvince, sourceCountryName = x.SourceCountryName, name = x.NameFa, nameEn = x.NameEn, latitude = x.Latitude, longitude = x.Longitude, isProvinceCenter = x.IsProvinceCenter }).ToListAsync(cancellationToken);
        string json = System.Text.Json.JsonSerializer.Serialize(new { countries, provinces, cities }, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        return ContentCdn.RewriteJson(json, await mediaCdn.GetBaseUrlAsync(cancellationToken))!;
    }

}
