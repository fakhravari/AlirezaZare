SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51720, 'Store.SchemaMigration is required.', 1;

    IF OBJECT_ID(N'[Warehouse].[WarehouseStockOwner]', N'U') IS NULL
       OR COL_LENGTH(N'Warehouse.WarehouseStockOwner', N'Id') IS NULL
       OR OBJECT_ID(N'[Store].[VariantCatalogFeatureValue]', N'U') IS NULL
       OR COL_LENGTH(N'Store.VariantCatalogFeatureValue', N'Id') IS NULL
       OR OBJECT_ID(N'[Store].[CouponUsage]', N'U') IS NULL
       OR COL_LENGTH(N'Store.CouponUsage', N'Id') IS NULL
        THROW 51721, 'The three association tables must already have their surrogate Id columns.', 1;

    IF EXISTS (
        SELECT 1
        FROM (VALUES (N'Warehouse', N'WarehouseStockOwner'), (N'Store', N'VariantCatalogFeatureValue'), (N'Store', N'CouponUsage')) AS target([SchemaName], [TableName])
        JOIN sys.tables t ON t.[name] = target.[TableName]
        JOIN sys.schemas s ON s.[schema_id] = t.[schema_id] AND s.[name] = target.[SchemaName]
        JOIN sys.columns c ON c.[object_id] = t.[object_id] AND c.[name] = N'Id'
        JOIN sys.types ty ON ty.[user_type_id] = c.[user_type_id]
        WHERE ty.[name] <> N'numeric' OR c.[precision] <> 30 OR c.[scale] <> 0 OR c.[is_identity] <> 1
    )
        THROW 51724, 'Each association Id must be numeric(30,0) IDENTITY.', 1;

    IF EXISTS (
        SELECT 1
        FROM (VALUES (N'Warehouse', N'WarehouseStockOwner'), (N'Store', N'VariantCatalogFeatureValue'), (N'Store', N'CouponUsage')) AS target([SchemaName], [TableName])
        JOIN sys.tables t ON t.[name] = target.[TableName]
        JOIN sys.schemas s ON s.[schema_id] = t.[schema_id] AND s.[name] = target.[SchemaName]
        WHERE NOT EXISTS (
            SELECT 1
            FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.[object_id] = i.[object_id] AND ic.[index_id] = i.[index_id]
            JOIN sys.columns c ON c.[object_id] = ic.[object_id] AND c.[column_id] = ic.[column_id]
            WHERE i.[object_id] = t.[object_id]
              AND i.[is_primary_key] = 1
              AND c.[name] = N'Id'
              AND ic.[key_ordinal] = 1
              AND (SELECT COUNT(*) FROM sys.index_columns x WHERE x.[object_id] = i.[object_id] AND x.[index_id] = i.[index_id] AND x.[key_ordinal] > 0) = 1
        )
    )
        THROW 51725, 'Each association table must already use Id as its only primary-key column.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'172-surrogate-keys-for-association-tables')
    BEGIN
        IF EXISTS (
            SELECT 1 FROM [Warehouse].[WarehouseStockOwner]
            GROUP BY [IdWarehouse], [IdStockOwner] HAVING COUNT_BIG(*) > 1
        ) OR EXISTS (
            SELECT 1 FROM [Store].[VariantCatalogFeatureValue]
            GROUP BY [IdProductVariant], [IdFeature] HAVING COUNT_BIG(*) > 1
        ) OR EXISTS (
            SELECT 1 FROM [Store].[CouponUsage]
            GROUP BY [IdCoupon], [IdCustomerOrder] HAVING COUNT_BIG(*) > 1
        )
            THROW 51722, 'Duplicate association rows must be resolved before adding unique constraints.', 1;

        IF NOT EXISTS (
            SELECT 1 FROM sys.key_constraints
            WHERE [parent_object_id] = OBJECT_ID(N'[Warehouse].[WarehouseStockOwner]')
              AND [name] = N'UQ_WarehouseStockOwner_Warehouse_StockOwner'
              AND [type] = N'UQ'
        )
            ALTER TABLE [Warehouse].[WarehouseStockOwner]
                ADD CONSTRAINT [UQ_WarehouseStockOwner_Warehouse_StockOwner] UNIQUE ([IdWarehouse], [IdStockOwner]);

        IF NOT EXISTS (
            SELECT 1 FROM sys.key_constraints
            WHERE [parent_object_id] = OBJECT_ID(N'[Store].[VariantCatalogFeatureValue]')
              AND [name] = N'UQ_VariantCatalogFeatureValue_Variant_Feature'
              AND [type] = N'UQ'
        )
            ALTER TABLE [Store].[VariantCatalogFeatureValue]
                ADD CONSTRAINT [UQ_VariantCatalogFeatureValue_Variant_Feature] UNIQUE ([IdProductVariant], [IdFeature]);

        IF NOT EXISTS (
            SELECT 1 FROM sys.key_constraints
            WHERE [parent_object_id] = OBJECT_ID(N'[Store].[CouponUsage]')
              AND [name] = N'UQ_CouponUsage_Coupon_Order'
              AND [type] = N'UQ'
        )
            ALTER TABLE [Store].[CouponUsage]
                ADD CONSTRAINT [UQ_CouponUsage_Coupon_Order] UNIQUE ([IdCoupon], [IdCustomerOrder]);

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE [object_id] = OBJECT_ID(N'[Warehouse].[WarehouseStockOwner]')
              AND [name] = N'UX_WarehouseStockOwner_OneDefaultPerWarehouse'
              AND [is_unique] = 1
              AND REPLACE(REPLACE(REPLACE(LOWER([filter_definition]), N' ', N''), N'[', N''), N']', N'') = N'(isdefault=(1))'
        )
            THROW 51723, 'The filtered unique default-owner index is missing or has an unexpected definition.', 1;

        INSERT [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'172-surrogate-keys-for-association-tables');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
