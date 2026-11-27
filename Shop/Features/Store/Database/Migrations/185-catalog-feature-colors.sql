SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51850, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'185-catalog-feature-colors')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'183-catalog-feature-tree')
            THROW 51854, 'Migration 183 must be applied first.', 1;
        IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'184-remove-catalog-feature-parent-key')
            THROW 51855, 'Migration 184 must be applied first.', 1;
        IF OBJECT_ID(N'[Store].[Color]', N'U') IS NULL OR OBJECT_ID(N'[Store].[ProductColor]', N'U') IS NULL
            THROW 51851, 'Legacy color tables are required before migration 185.', 1;

        ALTER TABLE [Store].[CatalogFeature] ADD [ValueKey] nvarchar(80) NULL;
        ALTER TABLE [Store].[CatalogFeature] ADD [NameEn] nvarchar(300) NULL;
        ALTER TABLE [Store].[CatalogFeature] ADD [HexCode] char(7) NULL;
        DECLARE @MigrationSql nvarchar(max) = N'';
        SET @MigrationSql += N'ALTER TABLE [Store].[CatalogFeature] ADD CONSTRAINT [CK_Store_CatalogFeature_HexCode]
';
        SET @MigrationSql += N'CHECK ([HexCode] IS NULL OR [HexCode] like ''#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'');
';
        SET @MigrationSql += N'CREATE UNIQUE INDEX [UQ_Store_CatalogFeature_Parent_ValueKey]
';
        SET @MigrationSql += N'ON [Store].[CatalogFeature] ([IdParent], [ValueKey])
';
        SET @MigrationSql += N'WHERE [IdParent] IS NOT NULL AND [ValueKey] IS NOT NULL;
';
        SET @MigrationSql += N'
';
        SET @MigrationSql += N'DECLARE @FeatureId bigint;
';
        SET @MigrationSql += N'SELECT @FeatureId = [Id] FROM [Store].[CatalogFeature]
';
        SET @MigrationSql += N'WHERE [IdParent] IS NULL AND [Kind] = 2 AND [Name] = N''رنگ'';
';
        SET @MigrationSql += N'
';
        SET @MigrationSql += N'IF @FeatureId IS NULL
';
        SET @MigrationSql += N'BEGIN
';
        SET @MigrationSql += N'INSERT [Store].[CatalogFeature] ([Name], [Kind], [IsActive], [TreePathIds], [TreePathNames])
';
        SET @MigrationSql += N'VALUES (N''رنگ'', 2, 1, N'''', N''رنگ'');
';
        SET @MigrationSql += N'SET @FeatureId = SCOPE_IDENTITY();
';
        SET @MigrationSql += N'UPDATE [Store].[CatalogFeature] SET [TreePathIds] = CONVERT(nvarchar(30), @FeatureId)
';
        SET @MigrationSql += N'WHERE [Id] = @FeatureId;
';
        SET @MigrationSql += N'END;
';
        SET @MigrationSql += N'
';
        SET @MigrationSql += N'DECLARE @ColorMap TABLE ([IdColor] bigint NOT NULL PRIMARY KEY, [IdFeatureValue] bigint NOT NULL);
';
        SET @MigrationSql += N'INSERT [Store].[CatalogFeature] ([IdParent], [Name], [NameEn], [ValueKey], [HexCode], [Kind], [IsActive], [TreePathIds], [TreePathNames])
';
        SET @MigrationSql += N'SELECT @FeatureId,
';
        SET @MigrationSql += N'CASE WHEN EXISTS (
';
        SET @MigrationSql += N'SELECT 1 FROM [Store].[Color] duplicate
';
        SET @MigrationSql += N'WHERE duplicate.[Id] <> c.[Id] AND duplicate.[NameFa] = c.[NameFa])
';
        SET @MigrationSql += N'OR EXISTS (
';
        SET @MigrationSql += N'SELECT 1 FROM [Store].[CatalogFeature] existing
';
        SET @MigrationSql += N'WHERE existing.[IdParent] = @FeatureId AND existing.[Name] = c.[NameFa])
';
        SET @MigrationSql += N'THEN CONCAT(LEFT(c.[NameFa], 210), N'' ('', c.[ColorKey], N'')'')
';
        SET @MigrationSql += N'ELSE c.[NameFa] END,
';
        SET @MigrationSql += N'c.[NameEn], c.[ColorKey], c.[HexCode], 2, 1, N'''', N''''
';
        SET @MigrationSql += N'FROM [Store].[Color] c
';
        SET @MigrationSql += N'WHERE NOT EXISTS (
';
        SET @MigrationSql += N'SELECT 1 FROM [Store].[CatalogFeature] v
';
        SET @MigrationSql += N'WHERE v.[IdParent] = @FeatureId AND v.[ValueKey] = c.[ColorKey]);
';
        SET @MigrationSql += N'
';
        SET @MigrationSql += N'INSERT @ColorMap ([IdColor], [IdFeatureValue])
';
        SET @MigrationSql += N'SELECT c.[Id], v.[Id]
';
        SET @MigrationSql += N'FROM [Store].[Color] c
';
        SET @MigrationSql += N'JOIN [Store].[CatalogFeature] v ON v.[IdParent] = @FeatureId AND v.[ValueKey] = c.[ColorKey];
';
        SET @MigrationSql += N'
';
        SET @MigrationSql += N'IF (SELECT COUNT_BIG(*) FROM [Store].[Color]) <> (SELECT COUNT_BIG(*) FROM @ColorMap)
';
        SET @MigrationSql += N'THROW 51852, ''Not every legacy color has a catalog feature value.'', 1;
';
        SET @MigrationSql += N'
';
        SET @MigrationSql += N'UPDATE valueRow
';
        SET @MigrationSql += N'SET [TreePathIds] = CONCAT(CONVERT(nvarchar(30), @FeatureId), N''/'', CONVERT(nvarchar(30), valueRow.[Id])),
';
        SET @MigrationSql += N'[TreePathNames] = CONCAT(N''رنگ/'', valueRow.[Name])
';
        SET @MigrationSql += N'FROM [Store].[CatalogFeature] valueRow
';
        SET @MigrationSql += N'WHERE valueRow.[IdParent] = @FeatureId;
';
        SET @MigrationSql += N'
';
        SET @MigrationSql += N'INSERT [Store].[ProductCatalogFeatureValue] ([IdProduct], [IdFeature], [IdFeatureValue], [Kind])
';
        SET @MigrationSql += N'SELECT pc.[IdProduct], @FeatureId, cm.[IdFeatureValue], 2
';
        SET @MigrationSql += N'FROM [Store].[ProductColor] pc
';
        SET @MigrationSql += N'JOIN @ColorMap cm ON cm.[IdColor] = pc.[IdColor]
';
        SET @MigrationSql += N'WHERE NOT EXISTS (
';
        SET @MigrationSql += N'SELECT 1 FROM [Store].[ProductCatalogFeatureValue] existing
';
        SET @MigrationSql += N'WHERE existing.[IdProduct] = pc.[IdProduct] AND existing.[IdFeature] = @FeatureId
';
        SET @MigrationSql += N'AND existing.[IdFeatureValue] = cm.[IdFeatureValue]);
';
        SET @MigrationSql += N'
';
        SET @MigrationSql += N'IF EXISTS (
';
        SET @MigrationSql += N'SELECT 1 FROM [Store].[ProductColor] pc
';
        SET @MigrationSql += N'JOIN @ColorMap cm ON cm.[IdColor] = pc.[IdColor]
';
        SET @MigrationSql += N'LEFT JOIN [Store].[ProductCatalogFeatureValue] migrated
';
        SET @MigrationSql += N'ON migrated.[IdProduct] = pc.[IdProduct] AND migrated.[IdFeature] = @FeatureId
';
        SET @MigrationSql += N'AND migrated.[IdFeatureValue] = cm.[IdFeatureValue]
';
        SET @MigrationSql += N'WHERE migrated.[IdProduct] IS NULL)
';
        SET @MigrationSql += N'THROW 51853, ''A product color assignment was not transferred.'', 1;
';
        SET @MigrationSql += N'
';
        SET @MigrationSql += N'INSERT [Store].[CategoryCatalogFeature] ([IdCategory], [IdFeature])
';
        SET @MigrationSql += N'SELECT DISTINCT p.[IdCategory], @FeatureId
';
        SET @MigrationSql += N'FROM [Store].[ProductColor] pc
';
        SET @MigrationSql += N'JOIN [Warehouse].[Product] p ON p.[Id] = pc.[IdProduct]
';
        SET @MigrationSql += N'WHERE p.[IdCategory] IS NOT NULL
';
        SET @MigrationSql += N'AND NOT EXISTS (SELECT 1 FROM [Store].[CategoryCatalogFeature] cf
';
        SET @MigrationSql += N'WHERE cf.[IdCategory] = p.[IdCategory] AND cf.[IdFeature] = @FeatureId);
';
        SET @MigrationSql += N'
';
        SET @MigrationSql += N'DECLARE @DropSql nvarchar(max) = N'''';
';
        SET @MigrationSql += N'SELECT @DropSql = @DropSql + N''ALTER TABLE [Store].[ProductColor] DROP CONSTRAINT '' + QUOTENAME(fk.[name]) + N'';''
';
        SET @MigrationSql += N'FROM sys.foreign_keys fk WHERE fk.[parent_object_id] = OBJECT_ID(N''[Store].[ProductColor]'');
';
        SET @MigrationSql += N'EXEC sys.sp_executesql @DropSql;
';
        SET @MigrationSql += N'DROP TABLE [Store].[ProductColor];
';
        SET @MigrationSql += N'DROP TABLE [Store].[Color];
';
        SET @MigrationSql += N'
';
        SET @MigrationSql += N'INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N''185-catalog-feature-colors'');
';
        EXEC sys.sp_executesql @MigrationSql;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
