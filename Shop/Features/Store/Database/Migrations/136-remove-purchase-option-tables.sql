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
    THROW 51360, 'Store.SchemaMigration is required before migration 136.', 1;

IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'136-remove-purchase-option-tables')
BEGIN
    IF OBJECT_ID(N'[Store].[ProductPurchaseOption]', N'U') IS NULL
       OR OBJECT_ID(N'[Store].[ProductPurchaseOptionValue]', N'U') IS NULL
       OR OBJECT_ID(N'[Account].[CartItemPurchaseOptionValue]', N'U') IS NULL
        THROW 51361, 'One or more purchase-option tables are missing before migration 136.', 1;

    IF COL_LENGTH(N'Store.PriceVersion', N'IdOptionValue') IS NULL
        THROW 51362, 'Store.PriceVersion.IdOptionValue is required to retain legacy price history before migration 136.', 1;

    IF EXISTS (
        SELECT 1
        FROM sys.foreign_keys fk
        WHERE fk.referenced_object_id IN (
            OBJECT_ID(N'[Store].[ProductPurchaseOption]'),
            OBJECT_ID(N'[Store].[ProductPurchaseOptionValue]'),
            OBJECT_ID(N'[Account].[CartItemPurchaseOptionValue]'))
          AND fk.parent_object_id NOT IN (
            OBJECT_ID(N'[Store].[ProductPurchaseOptionValue]'),
            OBJECT_ID(N'[Account].[CartItemPurchaseOptionValue]'))
          AND NOT (fk.parent_object_id = OBJECT_ID(N'[Store].[PriceVersion]')
                   AND fk.referenced_object_id = OBJECT_ID(N'[Store].[ProductPurchaseOptionValue]'))
    )
        THROW 51363, 'An unexpected foreign key references purchase-option tables; migration 136 made no changes.', 1;

    IF EXISTS (
        SELECT 1
        FROM sys.sql_expression_dependencies dependency
        WHERE dependency.referenced_id IN (
            OBJECT_ID(N'[Store].[ProductPurchaseOption]'),
            OBJECT_ID(N'[Store].[ProductPurchaseOptionValue]'),
            OBJECT_ID(N'[Account].[CartItemPurchaseOptionValue]'))
          AND dependency.referencing_id NOT IN (
            OBJECT_ID(N'[Store].[ProductPurchaseOption]'),
            OBJECT_ID(N'[Store].[ProductPurchaseOptionValue]'),
            OBJECT_ID(N'[Account].[CartItemPurchaseOptionValue]'))
    )
        THROW 51364, 'A SQL module depends on purchase-option tables; migration 136 made no changes.', 1;

    DECLARE @priceOptionForeignKey sysname;
    SELECT @priceOptionForeignKey = fk.[name]
    FROM sys.foreign_keys fk
    WHERE fk.parent_object_id = OBJECT_ID(N'[Store].[PriceVersion]')
      AND fk.referenced_object_id = OBJECT_ID(N'[Store].[ProductPurchaseOptionValue]');

    IF @priceOptionForeignKey IS NOT NULL
    BEGIN
        DECLARE @dropPriceOptionForeignKey nvarchar(max) =
            N'ALTER TABLE [Store].[PriceVersion] DROP CONSTRAINT ' + QUOTENAME(@priceOptionForeignKey) + N';';
        EXEC sys.sp_executesql @dropPriceOptionForeignKey;
    END;

    -- Retire active option prices while retaining their rows for historical order references.
    UPDATE [Store].[PriceVersion]
    SET [IsActive] = 0
    WHERE [IdOptionValue] IS NOT NULL AND [IsActive] = 1;

    DROP TABLE [Account].[CartItemPurchaseOptionValue];
    DROP TABLE [Store].[ProductPurchaseOptionValue];
    DROP TABLE [Store].[ProductPurchaseOption];

    -- IdOptionValue remains as a historical discriminator for old PriceVersion and order references.
    -- Current pricing no longer writes or resolves purchase-option IDs.
    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'136-remove-purchase-option-tables');
END;

COMMIT TRANSACTION;
