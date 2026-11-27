SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51850, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'185-catalog-feature-colors')
    BEGIN
        IF OBJECT_ID(N'[Store].[Color]', N'U') IS NULL OR OBJECT_ID(N'[Store].[ProductColor]', N'U') IS NULL
            THROW 51851, 'Legacy color tables are required before migration 185.', 1;

        ALTER TABLE [Store].[CatalogFeature] ADD [ValueKey] nvarchar(80) NULL;
        ALTER TABLE [Store].[CatalogFeature] ADD [NameEn] nvarchar(300) NULL;
        ALTER TABLE [Store].[CatalogFeature] ADD [HexCode] char(7) NULL;
        ALTER TABLE [Store].[CatalogFeature] ADD CONSTRAINT [CK_Store_CatalogFeature_HexCode]
            CHECK ([HexCode] IS NULL OR [HexCode] like '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]');
        CREATE UNIQUE INDEX [UQ_Store_CatalogFeature_Parent_ValueKey]
            ON [Store].[CatalogFeature] ([IdParent], [ValueKey])
            WHERE [IdParent] IS NOT NULL AND [ValueKey] IS NOT NULL;

        DECLARE @FeatureId bigint;
        SELECT @FeatureId = [Id] FROM [Store].[CatalogFeature]
        WHERE [IdParent] IS NULL AND [Kind] = 2 AND [Name] = N'رنگ';

        IF @FeatureId IS NULL
        BEGIN
            INSERT [Store].[CatalogFeature] ([Name], [Kind], [IsActive], [TreePathIds], [TreePathNames])
            VALUES (N'رنگ', 2, 1, N'', N'رنگ');
            SET @FeatureId = SCOPE_IDENTITY();
            UPDATE [Store].[CatalogFeature] SET [TreePathIds] = CONVERT(nvarchar(30), @FeatureId)
            WHERE [Id] = @FeatureId;
        END;

        DECLARE @ColorMap TABLE ([IdColor] bigint NOT NULL PRIMARY KEY, [IdFeatureValue] bigint NOT NULL);
        INSERT [Store].[CatalogFeature] ([IdParent], [Name], [NameEn], [ValueKey], [HexCode], [Kind], [IsActive], [TreePathIds], [TreePathNames])
        SELECT @FeatureId,
            CASE WHEN EXISTS (
                SELECT 1 FROM [Store].[Color] duplicate
                WHERE duplicate.[Id] <> c.[Id] AND duplicate.[NameFa] = c.[NameFa])
                OR EXISTS (
                    SELECT 1 FROM [Store].[CatalogFeature] existing
                    WHERE existing.[IdParent] = @FeatureId AND existing.[Name] = c.[NameFa])
                THEN CONCAT(LEFT(c.[NameFa], 210), N' (', c.[ColorKey], N')')
                ELSE c.[NameFa] END,
            c.[NameEn], c.[ColorKey], c.[HexCode], 2, 1, N'', N''
        FROM [Store].[Color] c
        WHERE NOT EXISTS (
            SELECT 1 FROM [Store].[CatalogFeature] v
            WHERE v.[IdParent] = @FeatureId AND v.[ValueKey] = c.[ColorKey]);

        INSERT @ColorMap ([IdColor], [IdFeatureValue])
        SELECT c.[Id], v.[Id]
        FROM [Store].[Color] c
        JOIN [Store].[CatalogFeature] v ON v.[IdParent] = @FeatureId AND v.[ValueKey] = c.[ColorKey];

        IF (SELECT COUNT_BIG(*) FROM [Store].[Color]) <> (SELECT COUNT_BIG(*) FROM @ColorMap)
            THROW 51852, 'Not every legacy color has a catalog feature value.', 1;

        UPDATE valueRow
        SET [TreePathIds] = CONCAT(CONVERT(nvarchar(30), @FeatureId), N'/', CONVERT(nvarchar(30), valueRow.[Id])),
            [TreePathNames] = CONCAT(N'رنگ/', valueRow.[Name])
        FROM [Store].[CatalogFeature] valueRow
        WHERE valueRow.[IdParent] = @FeatureId;

        INSERT [Store].[ProductCatalogFeatureValue] ([IdProduct], [IdFeature], [IdFeatureValue], [Kind])
        SELECT pc.[IdProduct], @FeatureId, cm.[IdFeatureValue], 2
        FROM [Store].[ProductColor] pc
        JOIN @ColorMap cm ON cm.[IdColor] = pc.[IdColor]
        WHERE NOT EXISTS (
            SELECT 1 FROM [Store].[ProductCatalogFeatureValue] existing
            WHERE existing.[IdProduct] = pc.[IdProduct] AND existing.[IdFeature] = @FeatureId
              AND existing.[IdFeatureValue] = cm.[IdFeatureValue]);

        IF EXISTS (
            SELECT 1 FROM [Store].[ProductColor] pc
            JOIN @ColorMap cm ON cm.[IdColor] = pc.[IdColor]
            LEFT JOIN [Store].[ProductCatalogFeatureValue] migrated
                ON migrated.[IdProduct] = pc.[IdProduct] AND migrated.[IdFeature] = @FeatureId
                AND migrated.[IdFeatureValue] = cm.[IdFeatureValue]
            WHERE migrated.[IdProduct] IS NULL)
            THROW 51853, 'A product color assignment was not transferred.', 1;

        INSERT [Store].[CategoryCatalogFeature] ([IdCategory], [IdFeature])
        SELECT DISTINCT p.[IdCategory], @FeatureId
        FROM [Store].[ProductColor] pc
        JOIN [Warehouse].[Product] p ON p.[Id] = pc.[IdProduct]
        WHERE p.[IdCategory] IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM [Store].[CategoryCatalogFeature] cf
                          WHERE cf.[IdCategory] = p.[IdCategory] AND cf.[IdFeature] = @FeatureId);

        DECLARE @DropSql nvarchar(max) = N'';
        SELECT @DropSql = @DropSql + N'ALTER TABLE [Store].[ProductColor] DROP CONSTRAINT ' + QUOTENAME(fk.[name]) + N';'
        FROM sys.foreign_keys fk WHERE fk.[parent_object_id] = OBJECT_ID(N'[Store].[ProductColor]');
        EXEC sys.sp_executesql @DropSql;
        DROP TABLE [Store].[ProductColor];
        DROP TABLE [Store].[Color];

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'185-catalog-feature-colors');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
