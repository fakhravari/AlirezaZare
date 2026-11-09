SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'031-remove-warehouse-store-product-key')
BEGIN
    COMMIT TRANSACTION;
    RETURN;
END;

IF OBJECT_ID(N'[Warehouse].[Product]', N'U') IS NULL
    THROW 51031, 'Warehouse.Product does not exist.', 1;
IF COL_LENGTH(N'Warehouse.Product', N'IdStoreProduct') IS NULL
    THROW 51031, 'Warehouse.Product.IdStoreProduct is required.', 1;
IF COL_LENGTH(N'Warehouse.Product', N'StoreProductKey') IS NULL
    THROW 51031, 'Warehouse.Product.StoreProductKey is missing while migration is unrecorded.', 1;

IF EXISTS (
    SELECT 1
    FROM [Warehouse].[Product] wp
    LEFT JOIN [Store].[Product] sp ON sp.[Id] = wp.[IdStoreProduct]
    WHERE wp.[IdStoreProduct] IS NULL
       OR sp.[Id] IS NULL
       OR wp.[StoreProductKey] <> sp.[StoreProductKey]
)
    THROW 51031, 'Warehouse product references are incomplete or their legacy keys disagree; no data was removed.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE [parent_object_id] = OBJECT_ID(N'[Warehouse].[Product]')
      AND [referenced_object_id] = OBJECT_ID(N'[Store].[Product]')
)
    THROW 51031, 'The direct Warehouse.Product.IdStoreProduct foreign key is required.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON ic.[object_id] = i.[object_id] AND ic.[index_id] = i.[index_id]
    INNER JOIN sys.columns c ON c.[object_id] = ic.[object_id] AND c.[column_id] = ic.[column_id]
    WHERE i.[object_id] = OBJECT_ID(N'[Warehouse].[Product]')
      AND i.[is_unique] = 1
      AND c.[name] = N'IdStoreProduct'
      AND ic.[key_ordinal] = 1
      AND (SELECT COUNT(*) FROM sys.index_columns allKeys
           WHERE allKeys.[object_id] = i.[object_id]
             AND allKeys.[index_id] = i.[index_id]
             AND allKeys.[key_ordinal] > 0) = 1
)
    THROW 51031, 'A unique index on Warehouse.Product.IdStoreProduct is required.', 1;

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE [parent_object_id] = OBJECT_ID(N'[Warehouse].[Product]') AND [name] = N'UQ_Warehouse_Product_StoreProductKey')
    ALTER TABLE [Warehouse].[Product] DROP CONSTRAINT [UQ_Warehouse_Product_StoreProductKey];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'[Warehouse].[Product]') AND [name] = N'CK_Warehouse_Product_StoreKey_NotEmpty')
    ALTER TABLE [Warehouse].[Product] DROP CONSTRAINT [CK_Warehouse_Product_StoreKey_NotEmpty];

ALTER TABLE [Warehouse].[Product] DROP COLUMN [StoreProductKey];
INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'031-remove-warehouse-store-product-key');

COMMIT TRANSACTION;
