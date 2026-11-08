SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'016-account-warehouse-variants')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF COL_LENGTH(N'Warehouse.Product', N'IdStoreProduct') IS NULL
       OR OBJECT_ID(N'[Warehouse].[ProductVariant]', N'U') IS NULL
        THROW 51040, 'Warehouse product and product-variant relations must be installed before this migration.', 1;

    IF COL_LENGTH(N'Warehouse.Product', N'Id') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[Warehouse].[Product]') AND [name] = N'UX_Warehouse_Product_Id_IdStoreProduct')
        CREATE UNIQUE INDEX [UX_Warehouse_Product_Id_IdStoreProduct]
            ON [Warehouse].[Product] ([Id], [IdStoreProduct]);

    IF COL_LENGTH(N'Warehouse.ProductVariant', N'IdStoreProduct') IS NULL
        ALTER TABLE [Warehouse].[ProductVariant] ADD [IdStoreProduct] BIGINT NULL;

    EXEC sys.sp_executesql N'
        UPDATE variant
        SET variant.[IdStoreProduct] = product.[IdStoreProduct]
        FROM [Warehouse].[ProductVariant] AS variant
        INNER JOIN [Warehouse].[Product] AS product ON product.[Id] = variant.[IdProduct]
        WHERE variant.[IdStoreProduct] IS NULL;';

    EXEC sys.sp_executesql N'
        IF EXISTS (SELECT 1 FROM [Warehouse].[ProductVariant] WHERE [IdStoreProduct] IS NULL)
            THROW 51041, ''A warehouse product variant is missing its store product relation.'', 1;';

    ALTER TABLE [Warehouse].[ProductVariant] ALTER COLUMN [IdStoreProduct] BIGINT NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Warehouse_ProductVariant_Product_StoreProduct')
        ALTER TABLE [Warehouse].[ProductVariant]
            ADD CONSTRAINT [FK_Warehouse_ProductVariant_Product_StoreProduct]
            FOREIGN KEY ([IdProduct], [IdStoreProduct]) REFERENCES [Warehouse].[Product] ([Id], [IdStoreProduct]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[Warehouse].[ProductVariant]') AND [name] = N'UX_Warehouse_ProductVariant_StoreProduct_Id')
        CREATE UNIQUE INDEX [UX_Warehouse_ProductVariant_StoreProduct_Id]
            ON [Warehouse].[ProductVariant] ([IdStoreProduct], [Id]);

    IF COL_LENGTH(N'Account.CartItem', N'IdProductVariant') IS NULL
        ALTER TABLE [Account].[CartItem] ADD [IdProductVariant] BIGINT NULL;
    IF COL_LENGTH(N'Account.CustomerOrderItem', N'IdProductVariant') IS NULL
        ALTER TABLE [Account].[CustomerOrderItem] ADD [IdProductVariant] BIGINT NULL;

    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE [name] = N'UQ_Account_CartItem_Configuration')
        ALTER TABLE [Account].[CartItem] DROP CONSTRAINT [UQ_Account_CartItem_Configuration];

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[Account].[CartItem]') AND [name] = N'UX_Account_CartItem_Configuration_Variant')
        CREATE UNIQUE INDEX [UX_Account_CartItem_Configuration_Variant]
            ON [Account].[CartItem] ([IdCart], [IdProduct], [SalesPlanKey], [IdProductVariant]);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Account_CartItem_WarehouseProductVariant')
        ALTER TABLE [Account].[CartItem]
            ADD CONSTRAINT [FK_Account_CartItem_WarehouseProductVariant]
            FOREIGN KEY ([IdProduct], [IdProductVariant])
            REFERENCES [Warehouse].[ProductVariant] ([IdStoreProduct], [Id]);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Account_CustomerOrderItem_WarehouseProductVariant')
        ALTER TABLE [Account].[CustomerOrderItem]
            ADD CONSTRAINT [FK_Account_CustomerOrderItem_WarehouseProductVariant]
            FOREIGN KEY ([IdProduct], [IdProductVariant])
            REFERENCES [Warehouse].[ProductVariant] ([IdStoreProduct], [Id]);

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'016-account-warehouse-variants');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
