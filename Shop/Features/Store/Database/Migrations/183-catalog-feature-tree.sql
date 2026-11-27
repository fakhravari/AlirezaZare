SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51830, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                   WHERE [MigrationKey] = N'183-catalog-feature-tree')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                       WHERE [MigrationKey] = N'182-remove-order-shipping-tracking-columns')
            THROW 51831, 'Migration 182 must be applied first.', 1;
        IF OBJECT_ID(N'[Store].[CatalogFeature]', N'U') IS NULL OR
           OBJECT_ID(N'[Store].[CatalogFeatureValue]', N'U') IS NULL
            THROW 51832, 'Catalog feature source tables are required.', 1;

        IF EXISTS (SELECT 1 FROM [Store].[CatalogFeatureValue] v
                   LEFT JOIN [Store].[CatalogFeature] f ON f.Id = v.IdFeature
                   WHERE f.Id IS NULL)
            THROW 51833, 'An orphan catalog feature value exists.', 1;
        IF EXISTS (SELECT 1 FROM [Store].[ProductCatalogFeatureValue] p
                   LEFT JOIN [Store].[CatalogFeatureValue] v ON v.Id = p.IdFeatureValue AND v.IdFeature = p.IdFeature
                   WHERE v.Id IS NULL)
            THROW 51834, 'A product feature assignment has an invalid value.', 1;
        IF EXISTS (SELECT 1 FROM [Store].[VariantCatalogFeatureValue] p
                   LEFT JOIN [Store].[CatalogFeatureValue] v ON v.Id = p.IdFeatureValue AND v.IdFeature = p.IdFeature
                   WHERE v.Id IS NULL)
            THROW 51835, 'A variant feature assignment has an invalid value.', 1;

        ALTER TABLE [Store].[ProductCatalogFeatureValue] DROP CONSTRAINT [FK_Store_ProductCatalogFeatureValue_Value];
        ALTER TABLE [Store].[VariantCatalogFeatureValue] DROP CONSTRAINT [FK_Store_VariantCatalogFeatureValue_Value];
        ALTER TABLE [Store].[CatalogFeatureValue] DROP CONSTRAINT [FK_Store_CatalogFeatureValue_Feature];

        ALTER TABLE [Store].[CatalogFeature] ADD [IdParent] bigint NULL;
        EXEC sys.sp_executesql N'ALTER TABLE [Store].[CatalogFeature] ADD [IdParentKey] AS ISNULL([IdParent], CONVERT(bigint, 0)) PERSISTED;';
        ALTER TABLE [Store].[CatalogFeature] ADD [TreePathIds] nvarchar(4000) NOT NULL
            CONSTRAINT [DF_Store_CatalogFeature_TreePathIds] DEFAULT (N'');
        ALTER TABLE [Store].[CatalogFeature] ADD [TreePathNames] nvarchar(4000) NOT NULL
            CONSTRAINT [DF_Store_CatalogFeature_TreePathNames] DEFAULT (N'');
        ALTER TABLE [Store].[CatalogFeature] ALTER COLUMN [Name] nvarchar(300) NOT NULL;
        ALTER TABLE [Store].[CatalogFeature] DROP CONSTRAINT [UQ_Store_CatalogFeature_Kind_Name];

        DECLARE @lastId bigint = ISNULL((SELECT MAX([Id]) FROM [Store].[CatalogFeature]), 0);
        CREATE TABLE #CatalogFeatureValueMap ([OldId] bigint NOT NULL PRIMARY KEY, [NewId] bigint NOT NULL UNIQUE);
        INSERT #CatalogFeatureValueMap ([OldId], [NewId])
        SELECT v.[Id], @lastId + ROW_NUMBER() OVER (ORDER BY v.[Id])
        FROM [Store].[CatalogFeatureValue] v;

        EXEC sys.sp_executesql N'
            SET IDENTITY_INSERT [Store].[CatalogFeature] ON;
            INSERT [Store].[CatalogFeature] ([Id], [IdParent], [Name], [Kind], [IsActive], [TreePathIds], [TreePathNames])
            SELECT m.[NewId], v.[IdFeature], v.[Name], f.[Kind], v.[IsActive], N'''', N''''
            FROM [Store].[CatalogFeatureValue] v
            JOIN #CatalogFeatureValueMap m ON m.[OldId] = v.[Id]
            JOIN [Store].[CatalogFeature] f ON f.[Id] = v.[IdFeature];
            SET IDENTITY_INSERT [Store].[CatalogFeature] OFF;';

        DECLARE @newLastId bigint = ISNULL((SELECT MAX([Id]) FROM [Store].[CatalogFeature]), 0);
        DECLARE @reseedSql nvarchar(200) = N'DBCC CHECKIDENT (''Store.CatalogFeature'', RESEED, '
            + CONVERT(nvarchar(30), @newLastId) + N')';
        EXEC(@reseedSql);

        UPDATE p SET p.[IdFeatureValue] = m.[NewId]
        FROM [Store].[ProductCatalogFeatureValue] p
        JOIN #CatalogFeatureValueMap m ON m.[OldId] = p.[IdFeatureValue];
        UPDATE p SET p.[IdFeatureValue] = m.[NewId]
        FROM [Store].[VariantCatalogFeatureValue] p
        JOIN #CatalogFeatureValueMap m ON m.[OldId] = p.[IdFeatureValue];

        EXEC sys.sp_executesql N'
            UPDATE [Store].[CatalogFeature]
            SET [TreePathIds] = CONVERT(nvarchar(30), [Id]), [TreePathNames] = [Name]
            WHERE [IdParent] IS NULL;
            UPDATE child
            SET child.[TreePathIds] = CONCAT(parent.[TreePathIds], N''/'', child.[Id]),
                child.[TreePathNames] = CONCAT(parent.[TreePathNames], N''/'', child.[Name])
            FROM [Store].[CatalogFeature] child
            JOIN [Store].[CatalogFeature] parent ON parent.[Id] = child.[IdParent];

            CREATE UNIQUE INDEX [UQ_Store_CatalogFeature_Kind_Name]
                ON [Store].[CatalogFeature] ([Kind], [Name]) WHERE [IdParent] IS NULL;
            CREATE UNIQUE INDEX [UQ_Store_CatalogFeature_Parent_Name]
                ON [Store].[CatalogFeature] ([IdParent], [Name]) WHERE [IdParent] IS NOT NULL;
            ALTER TABLE [Store].[CatalogFeature] ADD CONSTRAINT [AK_Store_CatalogFeature_Id_Parent]
                UNIQUE ([Id], [IdParentKey]);
            ALTER TABLE [Store].[CatalogFeature] ADD CONSTRAINT [FK_Store_CatalogFeature_Parent]
                FOREIGN KEY ([IdParent]) REFERENCES [Store].[CatalogFeature] ([Id]);
            ALTER TABLE [Store].[ProductCatalogFeatureValue] ADD CONSTRAINT [FK_Store_ProductCatalogFeatureValue_Value]
                FOREIGN KEY ([IdFeatureValue], [IdFeature]) REFERENCES [Store].[CatalogFeature] ([Id], [IdParentKey]);
            ALTER TABLE [Store].[VariantCatalogFeatureValue] ADD CONSTRAINT [FK_Store_VariantCatalogFeatureValue_Value]
                FOREIGN KEY ([IdFeatureValue], [IdFeature]) REFERENCES [Store].[CatalogFeature] ([Id], [IdParentKey]);';

        DROP TABLE [Store].[CatalogFeatureValue];
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'183-catalog-feature-tree');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
