SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'067-product-variant-default')
    BEGIN
        IF COL_LENGTH(N'Warehouse.ProductVariant', N'IsDefault') IS NULL
            ALTER TABLE [Warehouse].[ProductVariant] ADD [IsDefault] BIT NULL;

        EXEC sys.sp_executesql N'
            ;WITH ranked AS
            (
                SELECT [Id], ROW_NUMBER() OVER
                    (PARTITION BY [IdProduct] ORDER BY CASE WHEN [IsActive] = 1 THEN 0 ELSE 1 END, [Id]) AS [Position]
                FROM [Warehouse].[ProductVariant]
            )
            UPDATE variant
            SET [IsDefault] = CASE WHEN ranked.[Position] = 1 THEN 1 ELSE 0 END
            FROM [Warehouse].[ProductVariant] AS variant
            JOIN ranked ON ranked.[Id] = variant.[Id];';

        IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE [name] = N'DF_Warehouse_ProductVariant_IsDefault')
            EXEC sys.sp_executesql N'ALTER TABLE [Warehouse].[ProductVariant] ADD CONSTRAINT [DF_Warehouse_ProductVariant_IsDefault] DEFAULT (0) FOR [IsDefault];';

        EXEC sys.sp_executesql N'ALTER TABLE [Warehouse].[ProductVariant] ALTER COLUMN [IsDefault] BIT NOT NULL;';

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[Warehouse].[ProductVariant]') AND [name] = N'UX_Warehouse_ProductVariant_DefaultPerProduct')
            EXEC sys.sp_executesql N'CREATE UNIQUE INDEX [UX_Warehouse_ProductVariant_DefaultPerProduct]
                ON [Warehouse].[ProductVariant] ([IdProduct]) WHERE [IsDefault] = 1;
            ';

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'067-product-variant-default');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
