SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[OrderItemPriceReference]', N'U') IS NULL
        THROW 51062, 'Migration 061 must create Store.OrderItemPriceReference first.', 1;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Store].[OrderItemPriceReference]') AND name = N'UX_Store_OrderItemPriceReference_Component')
        DROP INDEX [UX_Store_OrderItemPriceReference_Component] ON [Store].[OrderItemPriceReference];

    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'[Store].[OrderItemPriceReference]') AND name = N'PK_Store_OrderItemPriceReference')
        ALTER TABLE [Store].[OrderItemPriceReference] DROP CONSTRAINT [PK_Store_OrderItemPriceReference];

    IF COL_LENGTH(N'Store.OrderItemPriceReference', N'AppliedUnitAmount') IS NOT NULL
        ALTER TABLE [Store].[OrderItemPriceReference] DROP COLUMN [AppliedUnitAmount];
    IF COL_LENGTH(N'Store.OrderItemPriceReference', N'ComponentType') IS NOT NULL
        ALTER TABLE [Store].[OrderItemPriceReference] DROP COLUMN [ComponentType];
    IF COL_LENGTH(N'Store.OrderItemPriceReference', N'Id') IS NOT NULL
        ALTER TABLE [Store].[OrderItemPriceReference] DROP COLUMN [Id];

    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'[Store].[OrderItemPriceReference]') AND name = N'PK_Store_OrderItemPriceReference')
        ALTER TABLE [Store].[OrderItemPriceReference] ADD CONSTRAINT [PK_Store_OrderItemPriceReference] PRIMARY KEY([IdOrderItem],[IdPriceVersion]);

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'062-order-price-reference-minimal')
        INSERT [Store].[SchemaMigration]([MigrationKey]) VALUES(N'062-order-price-reference-minimal');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
