SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'101-product-required-fields')
    BEGIN
        -- Do not replace missing business data with invented values.
        IF EXISTS (SELECT 1 FROM [Warehouse].[Product] WHERE [EnglishName] IS NULL OR [PriceAmount] IS NULL OR [DiscountPercent] IS NULL OR [MaxPurchaseQuantity] IS NULL)
            THROW 51101, 'Products with missing required values must be reviewed before migration 101.', 1;

        ALTER TABLE [Warehouse].[Product] ALTER COLUMN [EnglishName] nvarchar(300) NOT NULL;
        ALTER TABLE [Warehouse].[Product] ALTER COLUMN [PriceAmount] decimal(19, 0) NOT NULL;
        ALTER TABLE [Warehouse].[Product] ALTER COLUMN [DiscountPercent] decimal(5, 2) NOT NULL;
        ALTER TABLE [Warehouse].[Product] ALTER COLUMN [MaxPurchaseQuantity] int NOT NULL;

        DECLARE @constraint sysname, @sql nvarchar(max);
        SELECT @constraint = dc.[name]
        FROM sys.default_constraints dc
        JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
        WHERE dc.[parent_object_id] = OBJECT_ID(N'[Warehouse].[Product]') AND c.[name] = N'IsStoreEnabled';
        IF @constraint IS NOT NULL
        BEGIN
            SET @sql = N'ALTER TABLE [Warehouse].[Product] DROP CONSTRAINT ' + QUOTENAME(@constraint);
            EXEC sys.sp_executesql @sql;
        END;
        ALTER TABLE [Warehouse].[Product] ADD CONSTRAINT [DF_Product_IsStoreEnabled] DEFAULT (0) FOR [IsStoreEnabled];
        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'101-product-required-fields');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
