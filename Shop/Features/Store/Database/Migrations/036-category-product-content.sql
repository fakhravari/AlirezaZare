SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'036-category-product-content')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'035-unify-product-entity')
        THROW 51036, 'Product consolidation migration 035 must run first.', 1;

    CREATE TABLE #CategoryDetail ([IdCategory] BIGINT NOT NULL, [DetailKey] NVARCHAR(100) NOT NULL);
    IF COL_LENGTH(N'Warehouse.Product', N'DetailKey') IS NULL
        THROW 51036, 'The product detail relation was not found.', 1;
    EXEC(N'INSERT #CategoryDetail SELECT DISTINCT [IdCategory], [DetailKey] FROM [Warehouse].[Product] WHERE [DetailKey] IS NOT NULL AND [IdCategory] IS NOT NULL;');

    DECLARE @missingCategory INT = 0;
    EXEC sp_executesql N'SELECT @n = COUNT(*) FROM [Warehouse].[Product] WHERE [DetailKey] IS NOT NULL AND [IdCategory] IS NULL', N'@n int OUTPUT', @n = @missingCategory OUTPUT;
    IF @missingCategory > 0 THROW 51036, 'A product with details has no category.', 1;

    IF EXISTS (SELECT [IdCategory] FROM #CategoryDetail GROUP BY [IdCategory] HAVING COUNT(*) > 1)
        THROW 51036, 'A category references multiple detail groups.', 1;
    IF EXISTS (SELECT 1 FROM [Store].[ProductDetail] d WHERE NOT EXISTS (SELECT 1 FROM #CategoryDetail m WHERE m.[DetailKey] = d.[DetailKey]))
        THROW 51036, 'A detail group has no category mapping.', 1;

    ALTER TABLE [Store].[Category] ADD [ProductIntro] NVARCHAR(MAX) NULL, [ProductHighlightsJson] NVARCHAR(MAX) NULL, [ProductSpecsJson] NVARCHAR(MAX) NULL;
    EXEC(N'UPDATE c SET [ProductIntro] = d.[Intro], [ProductHighlightsJson] = d.[HighlightsJson], [ProductSpecsJson] = d.[SpecsJson]
        FROM [Store].[Category] c JOIN #CategoryDetail m ON m.[IdCategory] = c.[Id]
        JOIN [Store].[ProductDetail] d ON d.[DetailKey] = m.[DetailKey];');
    EXEC(N'ALTER TABLE [Store].[Category] ADD CONSTRAINT [CK_Store_Category_ProductContentJson]
        CHECK (([ProductHighlightsJson] IS NULL OR ISJSON([ProductHighlightsJson]) = 1) AND ([ProductSpecsJson] IS NULL OR ISJSON([ProductSpecsJson]) = 1));');

    ALTER TABLE [Warehouse].[Product] DROP CONSTRAINT [FK_Warehouse_Product_Detail];
    ALTER TABLE [Warehouse].[Product] DROP COLUMN [DetailKey];
    DROP TABLE [Store].[ProductDetail];
    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'036-category-product-content');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
