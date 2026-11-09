SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'035-unify-product-entity')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF OBJECT_ID(N'[Store].[Product]', N'U') IS NULL OR OBJECT_ID(N'[Warehouse].[Product]', N'U') IS NULL
        THROW 51035, 'Both product tables must exist before product consolidation.', 1;

    EXEC sys.sp_rename N'Warehouse.Product.IsActive', N'IsWarehouseEnabled', N'COLUMN';

    ALTER TABLE [Warehouse].[Product] ADD
        [ProductKey] NVARCHAR(128) NULL,
        [Code] NVARCHAR(100) NULL,
        [EnglishName] NVARCHAR(300) NULL,
        [IdBrand] BIGINT NULL,
        [IdCategory] BIGINT NULL,
        [DetailKey] NVARCHAR(100) NULL,
        [PowerSource] NVARCHAR(200) NULL,
        [UseCase] NVARCHAR(500) NULL,
        [PriceAmount] DECIMAL(19,0) NULL,
        [DiscountPercent] DECIMAL(5,2) NULL,
        [MaxPurchaseQuantity] INT NULL,
        [Slug] NVARCHAR(200) NULL,
        [Badge] NVARCHAR(100) NULL,
        [IsNewest] BIT NOT NULL CONSTRAINT [DF_Warehouse_Product_IsNewest] DEFAULT (0),
        [NewestOrder] INT NULL,
        [IsStoreEnabled] BIT NOT NULL CONSTRAINT [DF_Warehouse_Product_IsStoreEnabled] DEFAULT (1),
        [IsCatalogActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_Product_IsCatalogActive] DEFAULT (1);

    EXEC(N'    UPDATE product
       SET product.[Name] = storeProduct.[Name],
           product.[ProductKey] = storeProduct.[StoreProductKey],
           product.[Code] = storeProduct.[Code],
           product.[EnglishName] = storeProduct.[EnglishName],
           product.[IdBrand] = storeProduct.[IdBrand],
           product.[IdCategory] = storeProduct.[IdCategory],
           product.[DetailKey] = storeProduct.[DetailKey],
           product.[PowerSource] = storeProduct.[PowerSource],
           product.[UseCase] = storeProduct.[UseCase],
           product.[PriceAmount] = storeProduct.[PriceAmount],
           product.[DiscountPercent] = storeProduct.[DiscountPercent],
           product.[MaxPurchaseQuantity] = storeProduct.[MaxPurchaseQuantity],
           product.[Slug] = storeProduct.[Slug],
           product.[Badge] = storeProduct.[Badge],
           product.[IsNewest] = storeProduct.[IsNewest],
           product.[NewestOrder] = storeProduct.[NewestOrder],
           product.[IsStoreEnabled] = storeProduct.[IsStoreEnabled],
           product.[IsCatalogActive] = storeProduct.[IsActive],
           product.[CreatedAtUtc] = storeProduct.[CreatedAtUtc],
           product.[UpdatedAtUtc] = storeProduct.[UpdatedAtUtc]
      FROM [Warehouse].[Product] product
      JOIN [Store].[Product] storeProduct ON storeProduct.[Id] = product.[Id];

    IF EXISTS (SELECT 1 FROM [Store].[Product] storeProduct
               LEFT JOIN [Warehouse].[Product] product ON product.[Id] = storeProduct.[Id]
               WHERE product.[Id] IS NULL OR product.[ProductKey] IS NULL OR product.[Code] IS NULL)
        THROW 51035, ''Product merge validation failed; no source product was deleted.'', 1;

    ALTER TABLE [Warehouse].[Product] ALTER COLUMN [ProductKey] NVARCHAR(128) NOT NULL;
    ALTER TABLE [Warehouse].[Product] ALTER COLUMN [Code] NVARCHAR(100) NOT NULL;

    DECLARE @drop nvarchar(max) = N'''';
    SELECT @drop += N''ALTER TABLE '' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N''.'' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
        + N'' DROP CONSTRAINT '' + QUOTENAME(fk.name) + N'';''
    FROM sys.foreign_keys fk
    WHERE fk.referenced_object_id = OBJECT_ID(N''[Store].[Product]'');

    DECLARE @add nvarchar(max) = N'''';
    SELECT @add += N''ALTER TABLE '' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N''.'' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
        + N'' ADD CONSTRAINT '' + QUOTENAME(fk.name) + N'' FOREIGN KEY (''
        + QUOTENAME(parentColumn.name) + N'') REFERENCES [Warehouse].[Product] ('' + QUOTENAME(referenceColumn.name) + N'')''
        + CASE fk.delete_referential_action WHEN 1 THEN N'' ON DELETE CASCADE'' WHEN 2 THEN N'' ON DELETE SET NULL'' WHEN 3 THEN N'' ON DELETE SET DEFAULT'' ELSE N'''' END
        + CASE fk.update_referential_action WHEN 1 THEN N'' ON UPDATE CASCADE'' WHEN 2 THEN N'' ON UPDATE SET NULL'' WHEN 3 THEN N'' ON UPDATE SET DEFAULT'' ELSE N'''' END + N'';''
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    JOIN sys.columns parentColumn ON parentColumn.object_id = fk.parent_object_id AND parentColumn.column_id = fkc.parent_column_id
    JOIN sys.columns referenceColumn ON referenceColumn.object_id = fk.referenced_object_id AND referenceColumn.column_id = fkc.referenced_column_id
    WHERE fk.referenced_object_id = OBJECT_ID(N''[Store].[Product]'')
      AND fk.parent_object_id <> OBJECT_ID(N''[Warehouse].[Product]'');

    EXEC sys.sp_executesql @drop;

    DROP TABLE [Store].[Product];
    EXEC sys.sp_executesql @add;

    ALTER TABLE [Warehouse].[Product] ADD CONSTRAINT [FK_Warehouse_Product_Brand]
        FOREIGN KEY ([IdBrand]) REFERENCES [Store].[Brand] ([Id]);
    ALTER TABLE [Warehouse].[Product] ADD CONSTRAINT [FK_Warehouse_Product_Category]
        FOREIGN KEY ([IdCategory]) REFERENCES [Store].[Category] ([Id]);
    ALTER TABLE [Warehouse].[Product] ADD CONSTRAINT [FK_Warehouse_Product_Detail]
        FOREIGN KEY ([DetailKey]) REFERENCES [Store].[ProductDetail] ([DetailKey]);
    CREATE UNIQUE INDEX [UX_Warehouse_Product_ProductKey] ON [Warehouse].[Product] ([ProductKey]);
    CREATE UNIQUE INDEX [UX_Warehouse_Product_Slug] ON [Warehouse].[Product] ([Slug]) WHERE [Slug] IS NOT NULL;
    CREATE INDEX [IX_Warehouse_Product_Brand_Category] ON [Warehouse].[Product] ([IdBrand], [IdCategory]);

');

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'035-unify-product-entity');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
