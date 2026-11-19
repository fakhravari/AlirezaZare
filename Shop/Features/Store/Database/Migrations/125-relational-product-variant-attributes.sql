SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'125-relational-product-variant-attributes')
    BEGIN
        IF COL_LENGTH(N'Warehouse.ProductVariant', N'AttributesJson') IS NULL
            THROW 51250, 'ProductVariant.AttributesJson is missing; review the database state before migration.', 1;
        IF EXISTS
        (
            SELECT 1 FROM [Warehouse].[ProductVariant]
            WHERE [AttributesJson] IS NOT NULL AND LTRIM(RTRIM([AttributesJson])) <> N''
              AND (ISJSON([AttributesJson]) <> 1 OR LEFT(LTRIM([AttributesJson]), 1) <> N'[')
        )
            THROW 51251, 'Product variant attributes contain invalid JSON; repair them before migration.', 1;
        IF EXISTS
        (
            SELECT 1
            FROM [Warehouse].[ProductVariant] v
            CROSS APPLY OPENJSON(v.[AttributesJson]) j
            WHERE v.[AttributesJson] IS NOT NULL AND LTRIM(RTRIM(v.[AttributesJson])) <> N''
              AND
              (
                  j.[type] <> 5
                  OR EXISTS
                  (
                      SELECT required.[PropertyName]
                      FROM (VALUES (N'key'), (N'label'), (N'value'), (N'display')) required ([PropertyName])
                      WHERE NOT EXISTS
                      (
                          SELECT 1 FROM OPENJSON(j.[value]) property
                          WHERE property.[key] = required.[PropertyName] AND property.[type] = 1
                      )
                  )
                  OR EXISTS
                  (
                      SELECT 1 FROM OPENJSON(j.[value]) property
                      WHERE property.[key] = N'swatch' AND property.[type] NOT IN (0, 1)
                  )
              )
        )
            THROW 51257, 'Product variant attribute entries must be objects with string key, label, value, and display fields.', 1;

        CREATE TABLE [Warehouse].[ProductAttribute]
        (
            [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Warehouse_ProductAttribute] PRIMARY KEY,
            [IdProduct] bigint NOT NULL,
            [Key] varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
            [Label] nvarchar(80) NOT NULL,
            [SortOrder] int NOT NULL CONSTRAINT [DF_Warehouse_ProductAttribute_SortOrder] DEFAULT (0),
            [IsActive] bit NOT NULL CONSTRAINT [DF_Warehouse_ProductAttribute_IsActive] DEFAULT (1),
            CONSTRAINT [FK_Warehouse_ProductAttribute_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Warehouse].[Product] ([Id]) ON DELETE CASCADE,
            CONSTRAINT [UQ_Warehouse_ProductAttribute_Id_Product] UNIQUE ([Id], [IdProduct]),
            CONSTRAINT [UQ_Warehouse_ProductAttribute_Product_Key] UNIQUE ([IdProduct], [Key])
        );
        CREATE TABLE [Warehouse].[ProductAttributeValue]
        (
            [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Warehouse_ProductAttributeValue] PRIMARY KEY,
            [IdProduct] bigint NOT NULL,
            [IdProductAttribute] bigint NOT NULL,
            [ValueKey] nvarchar(100) COLLATE Latin1_General_100_BIN2 NOT NULL,
            [Label] nvarchar(100) NOT NULL,
            [DisplayValue] nvarchar(100) NOT NULL,
            [Swatch] varchar(7) NULL,
            [IsActive] bit NOT NULL CONSTRAINT [DF_Warehouse_ProductAttributeValue_IsActive] DEFAULT (1),
            CONSTRAINT [FK_Warehouse_ProductAttributeValue_Attribute] FOREIGN KEY ([IdProductAttribute], [IdProduct]) REFERENCES [Warehouse].[ProductAttribute] ([Id], [IdProduct]) ON DELETE CASCADE,
            CONSTRAINT [UQ_Warehouse_ProductAttributeValue_Id_Product_Attribute] UNIQUE ([Id], [IdProduct], [IdProductAttribute]),
            CONSTRAINT [UQ_Warehouse_ProductAttributeValue_Attribute_Value] UNIQUE ([IdProductAttribute], [ValueKey]),
            CONSTRAINT [CK_Warehouse_ProductAttributeValue_Swatch] CHECK ([Swatch] IS NULL OR (LEN([Swatch]) = 7 AND LEFT([Swatch], 1) = '#' AND SUBSTRING([Swatch], 2, 6) COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-Fa-f]%'))
        );
        CREATE TABLE [Warehouse].[ProductVariantAttributeValue]
        (
            [IdProductVariant] bigint NOT NULL,
            [IdProduct] bigint NOT NULL,
            [IdProductAttribute] bigint NOT NULL,
            [IdProductAttributeValue] bigint NOT NULL,
            CONSTRAINT [PK_Warehouse_ProductVariantAttributeValue] PRIMARY KEY ([IdProductVariant], [IdProductAttribute]),
            CONSTRAINT [FK_Warehouse_ProductVariantAttributeValue_Variant] FOREIGN KEY ([IdProductVariant], [IdProduct]) REFERENCES [Warehouse].[ProductVariant] ([Id], [IdProduct]) ON DELETE CASCADE,
            CONSTRAINT [FK_Warehouse_ProductVariantAttributeValue_Value] FOREIGN KEY ([IdProductAttributeValue], [IdProduct], [IdProductAttribute]) REFERENCES [Warehouse].[ProductAttributeValue] ([Id], [IdProduct], [IdProductAttribute]) ON DELETE NO ACTION
        );
        CREATE INDEX [IX_Warehouse_ProductVariantAttributeValue_Value] ON [Warehouse].[ProductVariantAttributeValue] ([IdProductAttributeValue]);

        CREATE TABLE #ParsedAttributes
        (
            [IdProductVariant] bigint NOT NULL,
            [IdProduct] bigint NOT NULL,
            [Ordinal] int NOT NULL,
            -- Keep the untruncated JSON values until the length checks below run.
            [AttributeKey] nvarchar(max) COLLATE Latin1_General_100_BIN2 NOT NULL,
            [AttributeLabel] nvarchar(max) NOT NULL,
            [ValueKey] nvarchar(max) COLLATE Latin1_General_100_BIN2 NOT NULL,
            [DisplayValue] nvarchar(max) NOT NULL,
            [Swatch] nvarchar(max) NULL
        );
        INSERT #ParsedAttributes ([IdProductVariant], [IdProduct], [Ordinal], [AttributeKey], [AttributeLabel], [ValueKey], [DisplayValue], [Swatch])
        SELECT v.[Id], v.[IdProduct], TRY_CONVERT(int, j.[key]),
               LTRIM(RTRIM(a.[AttributeKey])), LTRIM(RTRIM(a.[AttributeLabel])),
               LTRIM(RTRIM(a.[ValueKey])), LTRIM(RTRIM(a.[DisplayValue])), NULLIF(UPPER(LTRIM(RTRIM(a.[Swatch]))), '')
        FROM [Warehouse].[ProductVariant] v
        CROSS APPLY OPENJSON(CASE WHEN ISJSON(v.[AttributesJson]) = 1 THEN v.[AttributesJson] ELSE N'[]' END) j
        CROSS APPLY OPENJSON(j.[value]) WITH
        (
            [AttributeKey] nvarchar(max) '$.key', [AttributeLabel] nvarchar(max) '$.label',
            [ValueKey] nvarchar(max) '$.value', [DisplayValue] nvarchar(max) '$.display', [Swatch] nvarchar(max) '$.swatch'
        ) a
        WHERE v.[AttributesJson] IS NOT NULL AND LTRIM(RTRIM(v.[AttributesJson])) <> N'';

        IF EXISTS (SELECT 1 FROM #ParsedAttributes WHERE [AttributeKey] = '' OR [AttributeLabel] = N'' OR [ValueKey] = N'' OR [DisplayValue] = N'' OR LEN([AttributeKey]) > 64 OR LEN([AttributeLabel]) > 80 OR LEN([ValueKey]) > 100 OR LEN([DisplayValue]) > 100)
            THROW 51252, 'Product variant attribute entries do not match the relational schema limits.', 1;
        IF EXISTS (SELECT 1 FROM #ParsedAttributes WHERE [AttributeKey] COLLATE Latin1_General_100_BIN2 LIKE '%[^a-zA-Z0-9-]%' OR LEFT([AttributeKey], 1) COLLATE Latin1_General_100_BIN2 NOT LIKE '[a-zA-Z]')
            THROW 51253, 'Product variant attribute keys contain unsupported characters.', 1;
        IF EXISTS (SELECT 1 FROM #ParsedAttributes WHERE [Swatch] IS NOT NULL AND (LEN([Swatch]) <> 7 OR LEFT([Swatch], 1) <> '#' OR SUBSTRING([Swatch], 2, 6) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%' ))
            THROW 51254, 'Product variant attribute swatch is invalid.', 1;
        IF EXISTS (SELECT 1 FROM #ParsedAttributes GROUP BY [IdProductVariant], [AttributeKey] HAVING COUNT(*) > 1)
            THROW 51255, 'A product variant contains duplicate attribute keys.', 1;
        IF EXISTS
        (
            SELECT 1 FROM #ParsedAttributes
            GROUP BY [IdProduct], [AttributeKey]
            HAVING COUNT(DISTINCT [AttributeLabel] COLLATE Latin1_General_100_BIN2) > 1
        )
            THROW 51258, 'The same product attribute has conflicting labels across variants.', 1;
        IF EXISTS
        (
            SELECT 1 FROM #ParsedAttributes
            GROUP BY [IdProduct], [AttributeKey], [ValueKey]
            HAVING COUNT(DISTINCT [DisplayValue] COLLATE Latin1_General_100_BIN2) > 1
                OR MIN(COALESCE([Swatch], '')) <> MAX(COALESCE([Swatch], ''))
        )
            THROW 51259, 'The same attribute value has conflicting display text or swatches across variants.', 1;

        INSERT [Warehouse].[ProductAttribute] ([IdProduct], [Key], [Label], [SortOrder])
        SELECT p.[IdProduct], p.[AttributeKey], MAX(p.[AttributeLabel]), MIN(p.[Ordinal])
        FROM #ParsedAttributes p
        GROUP BY p.[IdProduct], p.[AttributeKey];
        INSERT [Warehouse].[ProductAttributeValue] ([IdProduct], [IdProductAttribute], [ValueKey], [Label], [DisplayValue], [Swatch])
        SELECT p.[IdProduct], a.[Id], p.[ValueKey], MAX(p.[DisplayValue]), MAX(p.[DisplayValue]), MAX(p.[Swatch])
        FROM #ParsedAttributes p
        JOIN [Warehouse].[ProductAttribute] a ON a.[IdProduct] = p.[IdProduct] AND a.[Key] = p.[AttributeKey]
        GROUP BY p.[IdProduct], a.[Id], p.[ValueKey];
        INSERT [Warehouse].[ProductVariantAttributeValue] ([IdProductVariant], [IdProduct], [IdProductAttribute], [IdProductAttributeValue])
        SELECT p.[IdProductVariant], p.[IdProduct], a.[Id], av.[Id]
        FROM #ParsedAttributes p
        JOIN [Warehouse].[ProductAttribute] a ON a.[IdProduct] = p.[IdProduct] AND a.[Key] = p.[AttributeKey]
        JOIN [Warehouse].[ProductAttributeValue] av ON av.[IdProductAttribute] = a.[Id] AND av.[ValueKey] = p.[ValueKey];

        IF EXISTS
        (
            SELECT 1 FROM sys.check_constraints
            WHERE [parent_object_id] = OBJECT_ID(N'Warehouse.ProductVariant')
              AND [name] = N'CK_Warehouse_ProductVariant_AttributesJson'
        )
            ALTER TABLE [Warehouse].[ProductVariant] DROP CONSTRAINT [CK_Warehouse_ProductVariant_AttributesJson];

        ALTER TABLE [Warehouse].[ProductVariant] DROP COLUMN [AttributesJson];

        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.structure.group' AND [IsActive] = 1)
            THROW 51256, 'Warehouse structure permission group is missing.', 1;
        IF EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.variants')
            UPDATE [Admin].[Permission]
            SET [Name] = N'مدیریت تنوع و ویژگی کالا', [ParentPermissionKey] = N'warehouse.structure.group',
                [Area] = N'Warehouse', [Controller] = N'Features', [Action] = N'Index', [FeatureKey] = N'variants',
                [SortOrder] = 65, [IsActive] = 1
            WHERE [PermissionKey] = N'warehouse.variants';
        ELSE
            INSERT [Admin].[Permission] ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
            VALUES (N'warehouse.variants', N'مدیریت تنوع و ویژگی کالا', N'warehouse.structure.group', N'Warehouse', N'Features', N'Index', N'variants', 65, 1);

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'125-relational-product-variant-attributes');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
