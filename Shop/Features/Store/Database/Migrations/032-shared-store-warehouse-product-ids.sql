SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'032-shared-store-warehouse-product-ids')
BEGIN
    COMMIT TRANSACTION;
    RETURN;
END;

IF COL_LENGTH(N'Warehouse.Product', N'IdStoreProduct') IS NULL
   OR COL_LENGTH(N'Warehouse.ProductVariant', N'IdStoreProduct') IS NULL
    THROW 51032, 'Warehouse product relations are not in the expected pre-migration state.', 1;

IF COL_LENGTH(N'Store.Product', N'IsStoreEnabled') IS NULL
    ALTER TABLE [Store].[Product] ADD [IsStoreEnabled] BIT NOT NULL
        CONSTRAINT [DF_Store_Product_IsStoreEnabled] DEFAULT (1) WITH VALUES;

CREATE TABLE #ProductIdMap
(
    [OldId] BIGINT NOT NULL PRIMARY KEY,
    [SharedId] BIGINT NOT NULL UNIQUE
);
INSERT INTO #ProductIdMap ([OldId], [SharedId])
SELECT [Id], [IdStoreProduct] FROM [Warehouse].[Product];

IF EXISTS (
    SELECT 1
    FROM [Warehouse].[ProductVariant] variant
    INNER JOIN #ProductIdMap map ON map.[OldId] = variant.[IdProduct]
    WHERE variant.[IdStoreProduct] <> map.[SharedId]
)
    THROW 51032, 'A variant has inconsistent product mappings; no IDs were changed.', 1;

CREATE TABLE #WarehouseProductData
(
    [Id] BIGINT NOT NULL PRIMARY KEY,
    [IdBaseUnit] BIGINT NOT NULL,
    [Name] NVARCHAR(300) NOT NULL,
    [IsActive] BIT NOT NULL,
    [CreatedAtUtc] DATETIME2(3) NOT NULL,
    [UpdatedAtUtc] DATETIME2(3) NULL
);
INSERT INTO #WarehouseProductData ([Id], [IdBaseUnit], [Name], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
SELECT map.[SharedId], product.[IdBaseUnit], product.[Name], product.[IsActive], product.[CreatedAtUtc], product.[UpdatedAtUtc]
FROM [Warehouse].[Product] product
INNER JOIN #ProductIdMap map ON map.[OldId] = product.[Id];

ALTER TABLE [Account].[CartItem] DROP CONSTRAINT [FK_Account_CartItem_WarehouseProductVariant];
ALTER TABLE [Account].[CustomerOrderItem] DROP CONSTRAINT [FK_Account_CustomerOrderItem_WarehouseProductVariant];
ALTER TABLE [Warehouse].[InventoryItem] DROP CONSTRAINT [FK_Warehouse_InventoryItem_ProductVariant];
ALTER TABLE [Warehouse].[InventoryItem] DROP CONSTRAINT [FK_Warehouse_InventoryItem_Product];
ALTER TABLE [Warehouse].[ProductUnit] DROP CONSTRAINT [FK_Warehouse_ProductUnit_Product];
ALTER TABLE [Warehouse].[ProductVariant] DROP CONSTRAINT [FK_Warehouse_ProductVariant_Product_StoreProduct];
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [parent_object_id] = OBJECT_ID(N'[Warehouse].[ProductVariant]') AND [name] = N'FK_Warehouse_ProductVariant_Product')
    ALTER TABLE [Warehouse].[ProductVariant] DROP CONSTRAINT [FK_Warehouse_ProductVariant_Product];
ALTER TABLE [Warehouse].[Product] DROP CONSTRAINT [FK_Warehouse_Product_StoreProduct];
ALTER TABLE [Warehouse].[Product] DROP CONSTRAINT [FK_Warehouse_Product_BaseUnit];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[Warehouse].[ProductVariant]') AND [name] = N'UX_Warehouse_ProductVariant_StoreProduct_Id')
    DROP INDEX [UX_Warehouse_ProductVariant_StoreProduct_Id] ON [Warehouse].[ProductVariant];

UPDATE productUnit SET productUnit.[IdProduct] = map.[SharedId]
FROM [Warehouse].[ProductUnit] productUnit
INNER JOIN #ProductIdMap map ON map.[OldId] = productUnit.[IdProduct];
UPDATE inventoryItem SET inventoryItem.[IdProduct] = map.[SharedId]
FROM [Warehouse].[InventoryItem] inventoryItem
INNER JOIN #ProductIdMap map ON map.[OldId] = inventoryItem.[IdProduct];
UPDATE variant SET variant.[IdProduct] = map.[SharedId]
FROM [Warehouse].[ProductVariant] variant
INNER JOIN #ProductIdMap map ON map.[OldId] = variant.[IdProduct];

ALTER TABLE [Warehouse].[ProductVariant] DROP COLUMN [IdStoreProduct];
DROP TABLE [Warehouse].[Product];

CREATE TABLE [Warehouse].[Product]
(
    [Id] BIGINT NOT NULL,
    [IdBaseUnit] BIGINT NOT NULL,
    [Name] NVARCHAR(300) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_Product_IsActive] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_Product_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
    [UpdatedAtUtc] DATETIME2(3) NULL,
    CONSTRAINT [PK_Warehouse_Product_SharedId] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Warehouse_Product_StoreProduct_SharedId] FOREIGN KEY ([Id]) REFERENCES [Store].[Product] ([Id]),
    CONSTRAINT [FK_Warehouse_Product_BaseUnit] FOREIGN KEY ([IdBaseUnit]) REFERENCES [Warehouse].[Unit] ([Id]),
    CONSTRAINT [CK_Warehouse_Product_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
);

INSERT INTO [Warehouse].[Product] ([Id], [IdBaseUnit], [Name], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
SELECT [Id], [IdBaseUnit], [Name], [IsActive], [CreatedAtUtc], [UpdatedAtUtc]
FROM #WarehouseProductData;

ALTER TABLE [Warehouse].[ProductUnit] ADD CONSTRAINT [FK_Warehouse_ProductUnit_Product]
    FOREIGN KEY ([IdProduct]) REFERENCES [Warehouse].[Product] ([Id]);
ALTER TABLE [Warehouse].[ProductVariant] ADD CONSTRAINT [FK_Warehouse_ProductVariant_Product]
    FOREIGN KEY ([IdProduct]) REFERENCES [Warehouse].[Product] ([Id]);
CREATE UNIQUE INDEX [UX_Warehouse_ProductVariant_Product_Id]
    ON [Warehouse].[ProductVariant] ([IdProduct], [Id]);
ALTER TABLE [Warehouse].[InventoryItem] ADD CONSTRAINT [FK_Warehouse_InventoryItem_Product]
    FOREIGN KEY ([IdProduct]) REFERENCES [Warehouse].[Product] ([Id]);
ALTER TABLE [Warehouse].[InventoryItem] ADD CONSTRAINT [FK_Warehouse_InventoryItem_ProductVariant]
    FOREIGN KEY ([IdProductVariant], [IdProduct]) REFERENCES [Warehouse].[ProductVariant] ([Id], [IdProduct]);
ALTER TABLE [Account].[CartItem] ADD CONSTRAINT [FK_Account_CartItem_WarehouseProductVariant]
    FOREIGN KEY ([IdProduct], [IdProductVariant]) REFERENCES [Warehouse].[ProductVariant] ([IdProduct], [Id]);
ALTER TABLE [Account].[CustomerOrderItem] ADD CONSTRAINT [FK_Account_CustomerOrderItem_WarehouseProductVariant]
    FOREIGN KEY ([IdProduct], [IdProductVariant]) REFERENCES [Warehouse].[ProductVariant] ([IdProduct], [Id]);

INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'032-shared-store-warehouse-product-ids');
COMMIT TRANSACTION;
