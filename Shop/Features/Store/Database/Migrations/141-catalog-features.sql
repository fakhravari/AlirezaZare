SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
    THROW 51410, 'Store.SchemaMigration is required.', 1;
IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'141-catalog-features')
BEGIN
    CREATE TABLE [Store].[CatalogFeature] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Store_CatalogFeature] PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Kind] TINYINT NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Store_CatalogFeature_IsActive] DEFAULT (1),
        CONSTRAINT [CK_Store_CatalogFeature_Name] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
        CONSTRAINT [CK_Store_CatalogFeature_Kind] CHECK ([Kind] IN (1, 2)),
        CONSTRAINT [UQ_Store_CatalogFeature_Kind_Name] UNIQUE ([Kind], [Name]),
        CONSTRAINT [UQ_Store_CatalogFeature_Id_Kind] UNIQUE ([Id], [Kind])
    );
    CREATE TABLE [Store].[CatalogFeatureValue] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Store_CatalogFeatureValue] PRIMARY KEY,
        [IdFeature] BIGINT NOT NULL,
        [Name] NVARCHAR(300) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Store_CatalogFeatureValue_IsActive] DEFAULT (1),
        CONSTRAINT [FK_Store_CatalogFeatureValue_Feature] FOREIGN KEY ([IdFeature]) REFERENCES [Store].[CatalogFeature] ([Id]),
        CONSTRAINT [CK_Store_CatalogFeatureValue_Name] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
        CONSTRAINT [UQ_Store_CatalogFeatureValue_Feature_Name] UNIQUE ([IdFeature], [Name]),
        CONSTRAINT [UQ_Store_CatalogFeatureValue_Id_Feature] UNIQUE ([Id], [IdFeature])
    );
    CREATE TABLE [Store].[CategoryCatalogFeature] (
        [IdCategory] BIGINT NOT NULL,
        [IdFeature] BIGINT NOT NULL,
        CONSTRAINT [PK_Store_CategoryCatalogFeature] PRIMARY KEY ([IdCategory], [IdFeature]),
        CONSTRAINT [FK_Store_CategoryCatalogFeature_Category] FOREIGN KEY ([IdCategory]) REFERENCES [Store].[Category] ([Id]),
        CONSTRAINT [FK_Store_CategoryCatalogFeature_Feature] FOREIGN KEY ([IdFeature]) REFERENCES [Store].[CatalogFeature] ([Id])
    );
    CREATE TABLE [Store].[ProductCatalogFeatureValue] (
        [IdProduct] BIGINT NOT NULL,
        [IdFeature] BIGINT NOT NULL,
        [IdFeatureValue] BIGINT NOT NULL,
        [Kind] TINYINT NOT NULL CONSTRAINT [DF_Store_ProductCatalogFeatureValue_Kind] DEFAULT (2),
        CONSTRAINT [PK_Store_ProductCatalogFeatureValue] PRIMARY KEY ([IdProduct], [IdFeature], [IdFeatureValue]),
        CONSTRAINT [CK_Store_ProductCatalogFeatureValue_Kind] CHECK ([Kind] = 2),
        CONSTRAINT [FK_Store_ProductCatalogFeatureValue_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Warehouse].[Product] ([Id]),
        CONSTRAINT [FK_Store_ProductCatalogFeatureValue_Feature] FOREIGN KEY ([IdFeature], [Kind]) REFERENCES [Store].[CatalogFeature] ([Id], [Kind]),
        CONSTRAINT [FK_Store_ProductCatalogFeatureValue_Value] FOREIGN KEY ([IdFeatureValue], [IdFeature]) REFERENCES [Store].[CatalogFeatureValue] ([Id], [IdFeature])
    );
    CREATE TABLE [Store].[VariantCatalogFeatureValue] (
        [IdProductVariant] BIGINT NOT NULL,
        [IdFeature] BIGINT NOT NULL,
        [IdFeatureValue] BIGINT NOT NULL,
        [Kind] TINYINT NOT NULL CONSTRAINT [DF_Store_VariantCatalogFeatureValue_Kind] DEFAULT (1),
        CONSTRAINT [PK_Store_VariantCatalogFeatureValue] PRIMARY KEY ([IdProductVariant], [IdFeature]),
        CONSTRAINT [CK_Store_VariantCatalogFeatureValue_Kind] CHECK ([Kind] = 1),
        CONSTRAINT [FK_Store_VariantCatalogFeatureValue_Variant] FOREIGN KEY ([IdProductVariant]) REFERENCES [Warehouse].[ProductVariant] ([Id]),
        CONSTRAINT [FK_Store_VariantCatalogFeatureValue_Feature] FOREIGN KEY ([IdFeature], [Kind]) REFERENCES [Store].[CatalogFeature] ([Id], [Kind]),
        CONSTRAINT [FK_Store_VariantCatalogFeatureValue_Value] FOREIGN KEY ([IdFeatureValue], [IdFeature]) REFERENCES [Store].[CatalogFeatureValue] ([Id], [IdFeature])
    );
    -- Keep the old data in place while copying it.  The price/inventory links
    -- remain authoritative until the variant editor has been migrated.
    IF EXISTS (SELECT 1 FROM [Store].[Category] WHERE [ProductSpecsJson] IS NOT NULL AND (ISJSON([ProductSpecsJson]) <> 1 OR LEFT(LTRIM([ProductSpecsJson]), 1) <> N'['))
        THROW 51411, 'A category has invalid legacy specification JSON.', 1;
    IF EXISTS (SELECT 1 FROM [Store].[Category] c CROSS APPLY OPENJSON(COALESCE(c.[ProductSpecsJson], N'[]')) pair
               WHERE pair.[type] <> 4 OR JSON_VALUE(CASE WHEN pair.[type] = 4 THEN pair.[value] ELSE N'[]' END, '$[0]') IS NULL
                                      OR JSON_VALUE(CASE WHEN pair.[type] = 4 THEN pair.[value] ELSE N'[]' END, '$[1]') IS NULL)
        THROW 51414, 'A category contains an invalid legacy specification pair.', 1;
    CREATE TABLE #LegacyFeature (
        [Kind] TINYINT NOT NULL, [FeatureName] NVARCHAR(MAX) NOT NULL, [ValueName] NVARCHAR(MAX) NOT NULL,
        [IdCategory] BIGINT NULL, [IdProduct] BIGINT NULL, [IdVariant] BIGINT NULL
    );
    INSERT #LegacyFeature ([Kind], [FeatureName], [ValueName], [IdCategory], [IdProduct], [IdVariant])
    SELECT 2, LTRIM(RTRIM(JSON_VALUE(pair.[value], '$[0]'))), LTRIM(RTRIM(JSON_VALUE(pair.[value], '$[1]'))), c.[Id], NULL, NULL
    FROM [Store].[Category] c CROSS APPLY OPENJSON(COALESCE(c.[ProductSpecsJson], N'[]')) pair
    WHERE JSON_VALUE(pair.[value], '$[0]') IS NOT NULL AND JSON_VALUE(pair.[value], '$[1]') IS NOT NULL;
    INSERT #LegacyFeature ([Kind], [FeatureName], [ValueName], [IdCategory], [IdProduct], [IdVariant])
    SELECT 2, LTRIM(RTRIM(f.[Label])), LTRIM(RTRIM(v.[Value])), p.[IdCategory], p.[Id], NULL
    FROM [Store].[ProductSpecificationValue] v
    JOIN [Store].[SpecificationFeature] f ON f.[Id] = v.[IdSpecificationFeature]
    JOIN [Warehouse].[Product] p ON p.[Id] = v.[IdProduct];
    INSERT #LegacyFeature ([Kind], [FeatureName], [ValueName], [IdCategory], [IdProduct], [IdVariant])
    SELECT 1, LTRIM(RTRIM(a.[Label])), LTRIM(RTRIM(av.[DisplayValue])), p.[IdCategory], p.[Id], link.[IdProductVariant]
    FROM [Warehouse].[ProductAttribute] a
    JOIN [Warehouse].[ProductAttributeValue] av ON av.[IdProductAttribute] = a.[Id]
    JOIN [Warehouse].[Product] p ON p.[Id] = a.[IdProduct]
    LEFT JOIN [Warehouse].[ProductVariantAttributeValue] link ON link.[IdProductAttributeValue] = av.[Id];
    IF EXISTS (SELECT 1 FROM #LegacyFeature WHERE LEN([FeatureName]) NOT BETWEEN 1 AND 200 OR LEN([ValueName]) NOT BETWEEN 1 AND 300)
        THROW 51412, 'A legacy feature or value is empty or too long; reconcile it before migrating.', 1;
    IF EXISTS (SELECT 1 FROM #LegacyFeature WHERE [IdProduct] IS NOT NULL AND [IdCategory] IS NULL)
        THROW 51415, 'A product with legacy feature values has no category; assign its category before migrating.', 1;
    IF EXISTS (SELECT [IdCategory], CONVERT(NVARCHAR(200), [FeatureName]) FROM #LegacyFeature WHERE [Kind] = 2 AND [IdProduct] IS NULL
               GROUP BY [IdCategory], CONVERT(NVARCHAR(200), [FeatureName]) HAVING COUNT(DISTINCT CONVERT(NVARCHAR(300), [ValueName])) > 1)
        THROW 51416, 'A category has conflicting legacy values for the same feature.', 1;
    IF EXISTS (SELECT [IdVariant], CONVERT(NVARCHAR(200), [FeatureName]) FROM #LegacyFeature WHERE [IdVariant] IS NOT NULL
               GROUP BY [IdVariant], CONVERT(NVARCHAR(200), [FeatureName]) HAVING COUNT(DISTINCT CONVERT(NVARCHAR(300), [ValueName])) > 1)
        THROW 51413, 'A variant has conflicting legacy values for the same feature.', 1;
    INSERT [Store].[CatalogFeature] ([Kind], [Name])
        SELECT DISTINCT [Kind], CONVERT(NVARCHAR(200), [FeatureName]) FROM #LegacyFeature;
    INSERT [Store].[CatalogFeatureValue] ([IdFeature], [Name])
        SELECT DISTINCT f.[Id], CONVERT(NVARCHAR(300), l.[ValueName]) FROM #LegacyFeature l
        JOIN [Store].[CatalogFeature] f ON f.[Kind] = l.[Kind] AND f.[Name] = l.[FeatureName];
    INSERT [Store].[CategoryCatalogFeature] ([IdCategory], [IdFeature])
        SELECT DISTINCT l.[IdCategory], f.[Id] FROM #LegacyFeature l
        JOIN [Store].[CatalogFeature] f ON f.[Kind] = l.[Kind] AND f.[Name] = l.[FeatureName]
        WHERE l.[IdCategory] IS NOT NULL;
    INSERT [Store].[ProductCatalogFeatureValue] ([IdProduct], [IdFeature], [IdFeatureValue], [Kind])
        SELECT DISTINCT p.[Id], f.[Id], v.[Id], 2 FROM #LegacyFeature l
        JOIN [Warehouse].[Product] p ON p.[Id] = l.[IdProduct]
        JOIN [Store].[CatalogFeature] f ON f.[Kind] = l.[Kind] AND f.[Name] = l.[FeatureName]
        JOIN [Store].[CatalogFeatureValue] v ON v.[IdFeature] = f.[Id] AND v.[Name] = l.[ValueName]
        WHERE l.[Kind] = 2;
    INSERT [Store].[ProductCatalogFeatureValue] ([IdProduct], [IdFeature], [IdFeatureValue], [Kind])
        SELECT DISTINCT p.[Id], f.[Id], v.[Id], 2 FROM #LegacyFeature l
        JOIN [Warehouse].[Product] p ON p.[IdCategory] = l.[IdCategory]
        JOIN [Store].[CatalogFeature] f ON f.[Kind] = 2 AND f.[Name] = l.[FeatureName]
        JOIN [Store].[CatalogFeatureValue] v ON v.[IdFeature] = f.[Id] AND v.[Name] = l.[ValueName]
        WHERE l.[Kind] = 2 AND l.[IdProduct] IS NULL AND NOT EXISTS (
            SELECT 1 FROM [Store].[ProductCatalogFeatureValue] specific WHERE specific.[IdProduct] = p.[Id] AND specific.[IdFeature] = f.[Id]);
    -- Preserve legacy specification values and category defaults for products
    -- that may be added to a category after this migration.
    INSERT [Store].[CategoryCatalogFeature] ([IdCategory], [IdFeature])
        SELECT DISTINCT c.[Id], f.[Id] FROM [Store].[Category] c
        CROSS APPLY OPENJSON(COALESCE(c.[ProductSpecsJson], N'[]')) pair
        JOIN [Store].[CatalogFeature] f ON f.[Kind] = 2 AND f.[Name] = LTRIM(RTRIM(JSON_VALUE(pair.[value], '$[0]')))
        WHERE NOT EXISTS (SELECT 1 FROM [Store].[CategoryCatalogFeature] existing WHERE existing.[IdCategory] = c.[Id] AND existing.[IdFeature] = f.[Id]);
    ;WITH CategoryPath AS
    (
        SELECT p.[Id] AS [IdProduct], c.[Id] AS [IdCategory], 0 AS [Depth]
        FROM [Warehouse].[Product] p JOIN [Store].[Category] c ON c.[Id] = p.[IdCategory]
        UNION ALL
        SELECT path.[IdProduct], parent.[Id], path.[Depth] + 1
        FROM CategoryPath path
        JOIN [Store].[Category] child ON child.[Id] = path.[IdCategory]
        JOIN [Store].[Category] parent ON parent.[Id] = child.[IdParentCategory]
        WHERE path.[Depth] < 100
    ), CategoryDefaults AS
    (
        SELECT path.[IdProduct], path.[IdCategory], LTRIM(RTRIM(JSON_VALUE(pair.[value], '$[0]'))) AS [FeatureName],
               LTRIM(RTRIM(JSON_VALUE(pair.[value], '$[1]'))) AS [ValueName], path.[Depth]
        FROM CategoryPath path JOIN [Store].[Category] c ON c.[Id] = path.[IdCategory]
        CROSS APPLY OPENJSON(COALESCE(c.[ProductSpecsJson], N'[]')) pair
    ), RankedDefaults AS
    (
        SELECT defaults.*, f.[Id] AS [IdFeature], v.[Id] AS [IdFeatureValue],
               ROW_NUMBER() OVER (PARTITION BY defaults.[IdProduct], f.[Id] ORDER BY defaults.[Depth], defaults.[IdCategory]) AS [Rank]
        FROM CategoryDefaults defaults
        JOIN [Store].[CatalogFeature] f ON f.[Kind] = 2 AND f.[Name] = defaults.[FeatureName]
        JOIN [Store].[CatalogFeatureValue] v ON v.[IdFeature] = f.[Id] AND v.[Name] = defaults.[ValueName]
    )
    INSERT [Store].[ProductCatalogFeatureValue] ([IdProduct], [IdFeature], [IdFeatureValue], [Kind])
        SELECT defaults.[IdProduct], defaults.[IdFeature], defaults.[IdFeatureValue], 2
        FROM RankedDefaults defaults
        WHERE defaults.[Rank] = 1 AND NOT EXISTS (
            SELECT 1 FROM [Store].[ProductCatalogFeatureValue] existing
            WHERE existing.[IdProduct] = defaults.[IdProduct] AND existing.[IdFeature] = defaults.[IdFeature])
    OPTION (MAXRECURSION 100);    INSERT [Store].[VariantCatalogFeatureValue] ([IdProductVariant], [IdFeature], [IdFeatureValue], [Kind])
        SELECT DISTINCT l.[IdVariant], f.[Id], v.[Id], 1 FROM #LegacyFeature l
        JOIN [Store].[CatalogFeature] f ON f.[Kind] = l.[Kind] AND f.[Name] = l.[FeatureName]
        JOIN [Store].[CatalogFeatureValue] v ON v.[IdFeature] = f.[Id] AND v.[Name] = l.[ValueName]
        WHERE l.[Kind] = 1 AND l.[IdVariant] IS NOT NULL;
    IF EXISTS (SELECT 1 FROM [Store].[ProductSalesPlanOfferSpecificationOverride] WHERE LEN(LTRIM(RTRIM([SpecificationKey]))) NOT BETWEEN 1 AND 200 OR LEN(LTRIM(RTRIM([Value]))) NOT BETWEEN 1 AND 300)
        THROW 51417, 'A plan specification is empty or too long to migrate.', 1;
    INSERT [Store].[CatalogFeature] ([Kind], [Name])
        SELECT DISTINCT 2, LTRIM(RTRIM(o.[SpecificationKey])) FROM [Store].[ProductSalesPlanOfferSpecificationOverride] o
        WHERE NOT EXISTS (SELECT 1 FROM [Store].[CatalogFeature] f WHERE f.[Kind] = 2 AND f.[Name] = LTRIM(RTRIM(o.[SpecificationKey])));
    INSERT [Store].[CatalogFeatureValue] ([IdFeature], [Name])
        SELECT DISTINCT f.[Id], LTRIM(RTRIM(o.[Value])) FROM [Store].[ProductSalesPlanOfferSpecificationOverride] o
        JOIN [Store].[CatalogFeature] f ON f.[Kind] = 2 AND f.[Name] = LTRIM(RTRIM(o.[SpecificationKey]))
        WHERE NOT EXISTS (SELECT 1 FROM [Store].[CatalogFeatureValue] v WHERE v.[IdFeature] = f.[Id] AND v.[Name] = LTRIM(RTRIM(o.[Value])));
    INSERT [Store].[CategoryCatalogFeature] ([IdCategory], [IdFeature])
        SELECT DISTINCT p.[IdCategory], f.[Id] FROM [Store].[ProductSalesPlanOfferSpecificationOverride] o
        JOIN [Store].[ProductSalesPlanOffer] offer ON offer.[Id] = o.[IdOffer]
        JOIN [Warehouse].[Product] p ON p.[Id] = offer.[IdProduct]
        JOIN [Store].[CatalogFeature] f ON f.[Kind] = 2 AND f.[Name] = LTRIM(RTRIM(o.[SpecificationKey]))
        WHERE p.[IdCategory] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Store].[CategoryCatalogFeature] existing WHERE existing.[IdCategory] = p.[IdCategory] AND existing.[IdFeature] = f.[Id]);
    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'141-catalog-features');
END;
COMMIT TRANSACTION;


