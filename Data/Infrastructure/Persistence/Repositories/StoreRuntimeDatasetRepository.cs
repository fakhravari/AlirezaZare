using System.Data;
using System.Data.Common;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Data.Infrastructure.Persistence.Repositories;

public sealed class StoreRuntimeDatasetRepository(ShopDbContext dbContext, IMediaCdnBaseUrlProvider mediaCdn) : IStoreRuntimeDatasetRepository
{
    public async Task<string?> ReadCatalogJsonAsync(CancellationToken cancellationToken)
    {
        string? json = await ReadScalarJsonAsync("""
                                  SELECT 2 AS [schemaVersion],
                                         JSON_QUERY((SELECT [GroupKey] AS [id], [Name] AS [name], [SortOrder] AS [order] FROM [Store].[SpecificationGroup] ORDER BY [SortOrder], [Id] FOR JSON PATH)) AS [specificationGroups],
                                         JSON_QUERY((SELECT c.[CategoryKey] AS [id], g.[GroupKey] AS [groupId], c.[Name] AS [name], c.[SortOrder] AS [order] FROM [Store].[SpecificationCategory] c JOIN [Store].[SpecificationGroup] g ON g.[Id]=c.[IdSpecificationGroup] ORDER BY c.[SortOrder], c.[Id] FOR JSON PATH)) AS [specificationCategories],
                                         JSON_QUERY((SELECT f.[FeatureKey] AS [id], c.[CategoryKey] AS [categoryId], f.[Label] AS [label], f.[SortOrder] AS [order] FROM [Store].[SpecificationFeature] f JOIN [Store].[SpecificationCategory] c ON c.[Id]=f.[IdSpecificationCategory] ORDER BY f.[SortOrder], f.[Id] FOR JSON PATH)) AS [specificationFeatures],
                                         JSON_QUERY((SELECT [AttributeKey] AS [id], [Label] AS [label], [Icon] AS [icon], [SourceKey] AS [source], [FilterKey] AS [filterKey], [IsFilterable] AS [is_filterable], [IsHighlight] AS [is_highlight] FROM [Store].[AttributeDefinition] ORDER BY [Id] FOR JSON PATH)) AS [attributeDefinitions],
                                         JSON_QUERY((SELECT b.[BrandKey] AS [id], b.[NameEn] AS [name], b.[NameFa] AS [fa], b.[LogoPath] AS [logo], c.[CountryCode] AS [countryCode] FROM [Store].[Brand] b LEFT JOIN [Geo].[Country] c ON c.[Id] = b.[IdCountry] ORDER BY b.[Id] FOR JSON PATH, INCLUDE_NULL_VALUES)) AS [brands],
                                         JSON_QUERY((SELECT c.[CategoryKey] AS [id], parent.[CategoryKey] AS [parentId], c.[NameEn] AS [en], c.[NameFa] AS [fa], c.[CategoryKey] AS [slug], JSON_QUERY(c.[PathJson]) AS [path] FROM [Store].[Category] c LEFT JOIN [Store].[Category] parent ON parent.[Id] = c.[IdParentCategory] WHERE c.[IsActive] = 1 ORDER BY c.[SortOrder], c.[Id] FOR JSON PATH, INCLUDE_NULL_VALUES)) AS [categories],
                                         JSON_QUERY((SELECT [ColorKey] AS [id], [NameEn] AS [en], [NameFa] AS [fa], [HexCode] AS [hex] FROM [Store].[Color] ORDER BY [Id] FOR JSON PATH)) AS [colors],
                                         JSON_QUERY((SELECT [CountryCode] AS [id], [NameFa] AS [name], [NameFa] AS [fa], [NameEn] AS [en], [FlagPath] AS [flag] FROM [Geo].[Country] ORDER BY [Id] FOR JSON PATH)) AS [countries],
                                         JSON_QUERY((SELECT [CategoryKey] AS [id], [ProductIntro] AS [intro], JSON_QUERY([ProductHighlightsJson]) AS [highlights], JSON_QUERY([ProductSpecsJson]) AS [specs] FROM [Store].[Category] WHERE [IsActive] = 1 ORDER BY [Id] FOR JSON PATH)) AS [details],
                                         JSON_QUERY((SELECT [MediaKey] AS [id], [MainPath] AS [main], [CardPath] AS [card], [ThumbnailPath] AS [thumbnail], JSON_QUERY(COALESCE([ImagesJson], N'[]')) AS [images], JSON_QUERY(COALESCE([VideosJson], N'[]')) AS [videos] FROM [Store].[ProductMedia] ORDER BY [Id] FOR JSON PATH)) AS [media],
                                         JSON_QUERY((SELECT CONVERT(INT, p.[Id]) AS [id], p.[Name] AS [name], COALESCE(p.[EnglishName], p.[Name]) AS [englishName], p.[Code] AS [code], p.[Slug] AS [slug], b.[BrandKey] AS [brandId], c.[CategoryKey] AS [categoryId], c.[CategoryKey] AS [detailId], pm.[MediaKey] AS [mediaId], COALESCE(p.[PowerSource], N'') AS [powerSource], COALESCE(p.[UseCase], N'') AS [useCase], (p.[PriceAmount] / 1000000.0) AS [price], CONVERT(INT, CASE WHEN stock.[Quantity] > 2147483647 THEN 2147483647 ELSE FLOOR(ISNULL(stock.[Quantity], 0)) END) AS [stockQuantity], COALESCE(p.[MaxPurchaseQuantity], 99) AS [maxCartQuantity], COALESCE(p.[DiscountPercent], 0) AS [discountPercent], p.[Badge] AS [badge], p.[IsNewest] AS [isNewest], p.[NewestOrder] AS [newestOrder], JSON_QUERY(N'[' + COALESCE((SELECT STRING_AGG(CONVERT(nvarchar(max), N'"' + STRING_ESCAPE(c2.[ColorKey], 'json') + N'"'), N',') FROM [Store].[ProductColor] pc INNER JOIN [Store].[Color] c2 ON c2.[Id] = pc.[IdColor] WHERE pc.[IdProduct] = p.[Id]), N'') + N']') AS [colorIds], JSON_QUERY((SELECT sf.[FeatureKey] AS [featureId], psv.[Value] AS [value] FROM [Store].[ProductSpecificationValue] psv INNER JOIN [Store].[SpecificationFeature] sf ON sf.[Id] = psv.[IdSpecificationFeature] WHERE psv.[IdProduct] = p.[Id] FOR JSON PATH)) AS [specificationValues], JSON_QUERY((SELECT a.[AccessoryKey] AS [id], a.[Name] AS [name], pa.[IsRequired] AS [required], (pa.[PriceAmount] / 1000000.0) AS [price] FROM [Store].[ProductAccessory] pa INNER JOIN [Store].[Accessory] a ON a.[Id] = pa.[IdAccessory] WHERE pa.[IdProduct] = p.[Id] ORDER BY pa.[SortOrder], pa.[Id] FOR JSON PATH, INCLUDE_NULL_VALUES)) AS [accessories], JSON_QUERY((SELECT d.[Title] AS [title], d.[Path] AS [path] FROM [Store].[ProductDocument] d WHERE d.[IdProduct] = p.[Id] ORDER BY d.[Id] FOR JSON PATH)) AS [documents], JSON_QUERY(N'{}') AS [variants] FROM [Warehouse].[Product] p OUTER APPLY (SELECT SUM(sb.[Quantity]) AS [Quantity] FROM [Warehouse].[InventoryItem] ii INNER JOIN [Warehouse].[StockBalance] sb ON sb.[IdInventoryItem] = ii.[Id] WHERE ii.[IdProduct] = p.[Id]) stock LEFT JOIN [Store].[Brand] b ON b.[Id] = p.[IdBrand] LEFT JOIN [Store].[Category] c ON c.[Id] = p.[IdCategory] LEFT JOIN [Store].[ProductMedia] pm ON pm.[IdProduct] = p.[Id] AND pm.[MediaKey] = CONCAT(N'product-', p.[ProductKey]) WHERE p.[IsCatalogActive] = 1 AND p.[IsStoreEnabled] = 1 ORDER BY p.[Id] FOR JSON PATH, INCLUDE_NULL_VALUES)) AS [products],
                                         JSON_QUERY((SELECT pc.[CollectionKey] AS [id], JSON_QUERY(N'[' + COALESCE((SELECT STRING_AGG(CONVERT(nvarchar(max), CONVERT(INT, p.[Id])), N',') FROM [Store].[ProductCollectionItem] pci INNER JOIN [Warehouse].[Product] p ON p.[Id] = pci.[IdProduct] WHERE pci.[IdProductCollection] = pc.[Id] AND p.[IsCatalogActive] = 1 AND p.[IsStoreEnabled] = 1), N'') + N']') AS [items] FROM [Store].[ProductCollection] pc ORDER BY pc.[Id] FOR JSON PATH)) AS [collections]
                                  FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                                  OPTION (MAX_GRANT_PERCENT = 5);
                                  """, cancellationToken, commandTimeoutSeconds: 90);
        if (string.IsNullOrWhiteSpace(json)) return json;

        var activeBrandKeys = await dbContext.StoreBrands.AsNoTracking().Where(x => x.IsActive)
            .Select(x => x.BrandKey).ToHashSetAsync(cancellationToken);
        var activeAttributeKeys = await dbContext.StoreAttributeDefinitions.AsNoTracking().Where(x => x.IsActive)
            .Select(x => x.AttributeKey).ToHashSetAsync(cancellationToken);
        var catalog = JsonNode.Parse(json)?.AsObject();
        if (catalog is null) return json;

        FilterArray(catalog, "brands", item => activeBrandKeys.Contains(item?["id"]?.GetValue<string>() ?? ""));
        FilterArray(catalog, "attributeDefinitions", item => activeAttributeKeys.Contains(item?["id"]?.GetValue<string>() ?? ""));
        FilterArray(catalog, "products", item =>
            activeBrandKeys.Contains(item?["brandId"]?.GetValue<string>() ?? "") &&
            (item?["categoryId"] is null || catalog["categories"]?.AsArray().Any(category =>
                string.Equals(category?["id"]?.GetValue<string>(), item["categoryId"]?.GetValue<string>(), StringComparison.Ordinal)) == true));
        var visibleProductIds = (catalog["products"]?.AsArray() ?? []).Select(item => item?["id"]?.ToJsonString()).ToHashSet(StringComparer.Ordinal);
        if (catalog["collections"] is JsonArray collections)
            foreach (var collection in collections)
                if (collection?["items"] is JsonArray items)
                    for (int index = items.Count - 1; index >= 0; index--)
                        if (!visibleProductIds.Contains(items[index]?.ToJsonString())) items.RemoveAt(index);
        return ContentCdn.RewriteJson(catalog.ToJsonString(), await mediaCdn.GetBaseUrlAsync(cancellationToken));
    }

    private static void FilterArray(JsonObject root, string name, Func<JsonNode?, bool> keep)
    {
        if (root[name] is not JsonArray items) return;
        for (int index = items.Count - 1; index >= 0; index--)
            if (!keep(items[index])) items.RemoveAt(index);
    }

    public async Task<string?> ReadBrandEditorialJsonAsync(CancellationToken cancellationToken)
    {
        string? json = await ReadScalarJsonAsync("""
                                  SELECT b.[BrandKey] AS [slug], e.[Established] AS [established], e.[OfficialUrl] AS [officialUrl], e.[Specialization] AS [specialization], e.[FeaturedProducts] AS [featuredProducts], e.[Audience] AS [audience], e.[ShoppingAdvice] AS [shoppingAdvice], JSON_QUERY(COALESCE(e.[CompanyIntroductionsJson], N'[]')) AS [companyIntroductions], JSON_QUERY(COALESCE(e.[ProductFamiliesJson], N'[]')) AS [productFamilies], JSON_QUERY(COALESCE(e.[StrengthsJson], N'[]')) AS [strengths]
                                  FROM [Store].[Brand] b INNER JOIN [Store].[BrandEditorial] e ON e.[IdBrand] = b.[Id]
                                  ORDER BY b.[Id]
                                  FOR JSON PATH;
                                  """, cancellationToken);
        return ContentCdn.RewriteJson(json, await mediaCdn.GetBaseUrlAsync(cancellationToken));
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

    public async Task<string?> ReadAccountJsonAsync(CancellationToken cancellationToken) =>
        await ReadScalarJsonAsync("""
                                  SELECT
                                  (
                                      SELECT
                                          1 AS [schemaVersion],
                                          N'IRT' AS [currency],
                                          JSON_QUERY(N'[]') AS [orders],
                                          JSON_QUERY((
                                              SELECT
                                                  c.[CouponKey] AS [id],
                                                  c.[Code] AS [code],
                                                  c.[DiscountPercent] AS [discountPercent],
                                                  c.[UsageLimit] AS [usageLimit],
                                                  c.[Enabled] AS [enabled],
                                                  c.[ExpiresOn] AS [expiresOn],
                                                  JSON_QUERY(N'[]') AS [orderIds]
                                              FROM [Store].[Coupon] c
                                              ORDER BY c.[Id]
                                              FOR JSON PATH
                                          )) AS [coupons]
                                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                                  ) AS [Value];
                                  """, cancellationToken);

    public async Task<string?> ReadMagazineJsonAsync(CancellationToken cancellationToken)
    {
        string? json = await ReadScalarJsonAsync("""
                                  SELECT JSON_QUERY((
                                      SELECT
                                          branch.[BranchKey] AS [branch], topic.[TopicKey] AS [topic], area.[AreaKey] AS [area],
                                          COALESCE(a.[ReadTimeMinutes], 0) AS [readMinutes],
                                          JSON_QUERY((
                                              SELECT a.[ArticleKey] AS [slug], category.[CategoryKey] AS [category], a.[Kicker] AS [kicker],
                                                     a.[Title] AS [title], a.[Summary] AS [summary], COALESCE(a.[Author], N'') AS [author],
                                                     N'' AS [publishedAt], COALESCE(a.[ReadTimeText], N'') AS [readTime],
                                                     a.[HeroImagePath] AS [heroImage], a.[ContentImagePath] AS [contentImage],
                                                     a.[Lead] AS [lead],
                                                     JSON_QUERY((SELECT CONVERT(nvarchar(50), s.[Id]) AS [id], s.[Heading] AS [title], s.[Body] AS [content]
                                                                 FROM [Mag].[MagazineArticleSection] s
                                                                 WHERE s.[IdMagazineArticle] = a.[Id]
                                                                 ORDER BY s.[SortOrder], s.[Id] FOR JSON PATH)) AS [sections],
                                                     a.[Conclusion] AS [conclusion],
                                                     JSON_QUERY((SELECT s.[Title] AS [title], s.[Url] AS [url]
                                                                 FROM [Mag].[MagazineArticleSource] s
                                                                 WHERE s.[IdMagazineArticle] = a.[Id]
                                                                 ORDER BY s.[SortOrder], s.[Id] FOR JSON PATH)) AS [sources],
                                                     JSON_QUERY((SELECT t.[Label] AS [label], t.[Filter] AS [filter]
                                                                 FROM [Mag].[MagazineArticleTag] t
                                                                 WHERE t.[IdMagazineArticle] = a.[Id]
                                                                 ORDER BY t.[SortOrder], t.[Id] FOR JSON PATH)) AS [tags],
                                                     JSON_QUERY((SELECT related.[ArticleKey] AS [slug], relatedCategory.[CategoryKey] AS [category], related.[Title] AS [title], related.[HeroImagePath] AS [image]
                                                                 FROM [Mag].[MagazineArticleRelated] r
                                                                 INNER JOIN [Mag].[MagazineArticle] related ON related.[Id] = r.[IdRelatedArticle]
                                                                 INNER JOIN [Mag].[ArticleCategory] relatedCategory ON relatedCategory.[Id] = related.[IdCategory]
                                                                 WHERE r.[IdMagazineArticle] = a.[Id]
                                                                 ORDER BY r.[SortOrder], r.[Id] FOR JSON PATH)) AS [relatedArticles],
                                                     JSON_QUERY((SELECT COALESCE(m.[Views], 0) AS [views], COALESCE(m.[Likes], 0) AS [likes],
                                                                        COALESCE(m.[AverageRating], 0) AS [averageRating], COALESCE(m.[Comments], 0) AS [comments]
                                                                 FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)) AS [metrics]
                                              FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                                          )) AS [article]
                                      FROM [Mag].[MagazineArticle] a
                                      INNER JOIN [Mag].[ArticleCategory] category ON category.[Id] = a.[IdCategory]
                                      INNER JOIN [Mag].[ArticleBranch] branch ON branch.[Id] = a.[IdBranch]
                                      INNER JOIN [Mag].[ArticleTopic] topic ON topic.[Id] = a.[IdTopic]
                                      INNER JOIN [Mag].[ArticleArea] area ON area.[Id] = a.[IdArea]
                                      LEFT JOIN [Mag].[MagazineArticleMetric] m ON m.[IdMagazineArticle] = a.[Id]
                                      WHERE a.[IsPublished] = 1
                                      ORDER BY a.[Id]
                                      FOR JSON PATH
                                  )) AS [articles]
                                  FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
                                  """, cancellationToken, commandTimeoutSeconds: 90);
        return ContentCdn.RewriteJson(json, await mediaCdn.GetBaseUrlAsync(cancellationToken));
    }

    public async Task<string> ReadGeographyJsonAsync(CancellationToken cancellationToken)
    {
        string json = await ReadScalarJsonAsync("""
                                  SELECT
                                      JSON_QUERY((SELECT c.[Id] AS [id], c.[NameFa] AS [name], c.[NameEn] AS [nameEn], c.[CountryCode] AS [countryCode]
                                                  FROM [Geo].[Country] c
                                                  WHERE EXISTS (SELECT 1 FROM [Geo].[Province] p WHERE p.[IdCountry] = c.[Id])
                                                  ORDER BY c.[Id] FOR JSON PATH)) AS [countries],
                                      JSON_QUERY((SELECT [Id] AS [id], [IdCountry] AS [countryId], [NameFa] AS [name],
                                                         [NameEn] AS [nameEn], [Latitude] AS [latitude], [Longitude] AS [longitude]
                                                  FROM [Geo].[Province] ORDER BY [NameFa] FOR JSON PATH)) AS [provinces],
                                      JSON_QUERY((SELECT [Id] AS [id], [IdCountry] AS [countryId], [IdProvince] AS [provinceId],
                                                         [SourceCountryName] AS [sourceCountryName], [NameFa] AS [name],
                                                         [NameEn] AS [nameEn], [Latitude] AS [latitude], [Longitude] AS [longitude],
                                                         [IsProvinceCenter] AS [isProvinceCenter]
                                                  FROM [Geo].[City] ORDER BY [IdProvince], [NameFa] FOR JSON PATH)) AS [cities]
                                  FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
                                  """, cancellationToken) ?? "{\"countries\":[],\"provinces\":[],\"cities\":[]}";
        return ContentCdn.RewriteJson(json, await mediaCdn.GetBaseUrlAsync(cancellationToken))!;
    }

    private async Task<string?> ReadScalarJsonAsync(string sql, CancellationToken cancellationToken,
        int? commandTimeoutSeconds = null)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        bool closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);
        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = sql;
            if (commandTimeoutSeconds.HasValue) command.CommandTimeout = commandTimeoutSeconds.Value;
            await using DbDataReader reader =
                await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(0)) return null;
            var json = new System.Text.StringBuilder();
            do
            {
                json.Append(reader.GetString(0));
            } while (await reader.ReadAsync(cancellationToken));

            return json.ToString();
        }
        finally
        {
            if (closeWhenDone) await connection.CloseAsync();
        }
    }
}
