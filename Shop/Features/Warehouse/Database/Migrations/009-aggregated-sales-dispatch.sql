SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Warehouse].[SchemaMigration] WHERE [MigrationKey] = N'009-aggregated-sales-dispatch')
    BEGIN COMMIT TRANSACTION; RETURN; END;
    IF NOT EXISTS (SELECT 1 FROM [Warehouse].[SchemaMigration] WHERE [MigrationKey] = N'008-document-line-sales-plan')
        THROW 51009, 'Warehouse migration 008 is required.', 1;
    IF OBJECT_ID(N'[Store].[PaymentAttempt]', N'U') IS NULL
        THROW 51009, 'Store payment migration 045 is required.', 1;
    IF COL_LENGTH(N'Store.Order', N'IdWarehouseDocument') IS NULL
        ALTER TABLE [Store].[Order] ADD [IdWarehouseDocument] BIGINT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Store_Order_WarehouseDocument')
        ALTER TABLE [Store].[Order] ADD CONSTRAINT [FK_Store_Order_WarehouseDocument]
            FOREIGN KEY ([IdWarehouseDocument]) REFERENCES [Warehouse].[WarehouseDocument] ([Id]);
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE [parent_object_id] = OBJECT_ID(N'[Warehouse].[PaidOrderInboxDocument]') AND [name] = N'UQ_Warehouse_PaidOrderInboxDocument_Document')
        ALTER TABLE [Warehouse].[PaidOrderInboxDocument] DROP CONSTRAINT [UQ_Warehouse_PaidOrderInboxDocument_Document];
    IF NOT EXISTS (SELECT 1 FROM [Warehouse].[DocumentType] WHERE [Code] = N'SALES-DISPATCH')
        INSERT [Warehouse].[DocumentType] ([Code], [Name], [Direction], [AffectsStock], [IsActive])
        VALUES (N'SALES-DISPATCH', N'حواله فروش', 'I', 1, 1);
    IF NOT EXISTS (SELECT 1 FROM [Warehouse].[Party] WHERE [ExternalPartyKey] = N'STORE-SALES')
        INSERT [Warehouse].[Party] ([PartyType], [ExternalPartyKey], [Name], [IsActive])
        VALUES (N'Other', N'STORE-SALES', N'سفارش‌های فروشگاه', 1);
    INSERT [Warehouse].[SchemaMigration] ([MigrationKey]) VALUES (N'009-aggregated-sales-dispatch');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
