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
    THROW 51380, 'Store.SchemaMigration is required before migration 138.', 1;

IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'138-remove-purchase-option-order-snapshots')
BEGIN
    IF COL_LENGTH(N'Store.OrderItem', N'OptionsJson') IS NULL
        THROW 51381, 'Store.OrderItem.OptionsJson is required before migration 138.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'137-remove-purchase-option-price-history')
        THROW 51382, 'Migration 137 must complete before migration 138.', 1;

    UPDATE [Store].[OrderItem]
    SET [OptionsJson] = JSON_MODIFY([OptionsJson], N'$.priceOptions', NULL)
    WHERE EXISTS (
        SELECT 1
        FROM OPENJSON(CASE WHEN ISJSON([Store].[OrderItem].[OptionsJson]) = 1
                           THEN [Store].[OrderItem].[OptionsJson] ELSE N'{}' END) property
        WHERE property.[key] = N'priceOptions');

    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'138-remove-purchase-option-order-snapshots');
END;

COMMIT TRANSACTION;
