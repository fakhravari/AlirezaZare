SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'102-product-media-relations')
    BEGIN
        IF EXISTS (SELECT 1 FROM [Store].[ProductMedia] WHERE (NULLIF(LTRIM(RTRIM(ImagesJson)), '') IS NOT NULL AND (ISJSON(ImagesJson) <> 1 OR LEFT(LTRIM(ImagesJson), 1) <> '[')) OR (NULLIF(LTRIM(RTRIM(VideosJson)), '') IS NOT NULL AND (ISJSON(VideosJson) <> 1 OR LEFT(LTRIM(VideosJson), 1) <> '[')))
            THROW 51000, 'Invalid product media JSON; repair it before migration.', 1;
        CREATE TABLE [Store].[ProductImage] (
            [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ProductImage] PRIMARY KEY,
            [IdProductMedia] bigint NOT NULL,
            [Path] nvarchar(1000) NOT NULL,
            [ThumbnailPath] nvarchar(1000) NULL,
            [SortOrder] int NOT NULL,
            CONSTRAINT [FK_ProductImage_ProductMedia] FOREIGN KEY ([IdProductMedia]) REFERENCES [Store].[ProductMedia]([Id])
        );
        CREATE TABLE [Store].[ProductVideo] (
            [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ProductVideo] PRIMARY KEY,
            [IdProductMedia] bigint NOT NULL,
            [Path] nvarchar(1000) NOT NULL,
            [SortOrder] int NOT NULL,
            CONSTRAINT [FK_ProductVideo_ProductMedia] FOREIGN KEY ([IdProductMedia]) REFERENCES [Store].[ProductMedia]([Id])
        );
        IF EXISTS (SELECT 1 FROM [Store].[ProductMedia] m CROSS APPLY OPENJSON(COALESCE(NULLIF(LTRIM(RTRIM(m.ImagesJson)), ''), '[]')) j WHERE j.[type] <> 5 OR NULLIF(JSON_VALUE(j.[value], '$.src'), '') IS NULL OR TRY_CONVERT(int, j.[key]) IS NULL)
            THROW 51001, 'Invalid product image entry; repair it before migration.', 1;
        IF EXISTS (SELECT 1 FROM [Store].[ProductMedia] m CROSS APPLY OPENJSON(COALESCE(NULLIF(LTRIM(RTRIM(m.VideosJson)), ''), '[]')) j WHERE j.[type] <> 1 OR NULLIF(j.[value], '') IS NULL OR TRY_CONVERT(int, j.[key]) IS NULL)
            THROW 51002, 'Invalid product video entry; repair it before migration.', 1;
        INSERT [Store].[ProductImage] ([IdProductMedia], [Path], [ThumbnailPath], [SortOrder])
        SELECT m.Id, JSON_VALUE(j.[value], '$.src'), JSON_VALUE(j.[value], '$.thumbnail'), CONVERT(int, j.[key]) + 1
        FROM [Store].[ProductMedia] m CROSS APPLY OPENJSON(COALESCE(NULLIF(LTRIM(RTRIM(m.ImagesJson)), ''), '[]')) j;
        INSERT [Store].[ProductImage] ([IdProductMedia], [Path], [ThumbnailPath], [SortOrder])
        SELECT m.Id, m.MainPath, m.ThumbnailPath, 0 FROM [Store].[ProductMedia] m
        WHERE NULLIF(m.MainPath, '') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Store].[ProductImage] i WHERE i.IdProductMedia = m.Id AND i.[Path] = m.MainPath);
        INSERT [Store].[ProductVideo] ([IdProductMedia], [Path], [SortOrder])
        SELECT m.Id, j.[value], CONVERT(int, j.[key]) + 1 FROM [Store].[ProductMedia] m CROSS APPLY OPENJSON(COALESCE(NULLIF(LTRIM(RTRIM(m.VideosJson)), ''), '[]')) j;
        CREATE INDEX [IX_ProductImage_Media_Order] ON [Store].[ProductImage] ([IdProductMedia], [SortOrder], [Id]);
        CREATE INDEX [IX_ProductVideo_Media_Order] ON [Store].[ProductVideo] ([IdProductMedia], [SortOrder], [Id]);
        IF (SELECT COUNT_BIG(*) FROM [Store].[ProductImage]) <>
           (SELECT COUNT_BIG(*) FROM [Store].[ProductMedia] m CROSS APPLY OPENJSON(COALESCE(NULLIF(LTRIM(RTRIM(m.ImagesJson)), ''), '[]')) j) +
           (SELECT COUNT_BIG(*) FROM [Store].[ProductMedia] m WHERE NULLIF(m.MainPath, '') IS NOT NULL AND NOT EXISTS
             (SELECT 1 FROM OPENJSON(COALESCE(NULLIF(LTRIM(RTRIM(m.ImagesJson)), ''), '[]')) j WHERE JSON_VALUE(j.[value], '$.src') = m.MainPath))
            THROW 51003, 'Image transfer count mismatch.', 1;
        IF (SELECT COUNT_BIG(*) FROM [Store].[ProductVideo]) <>
           (SELECT COUNT_BIG(*) FROM [Store].[ProductMedia] m CROSS APPLY OPENJSON(COALESCE(NULLIF(LTRIM(RTRIM(m.VideosJson)), ''), '[]')) j)
            THROW 51004, 'Video transfer count mismatch.', 1;
        IF OBJECT_ID(N'[Store].[CK_Store_ProductMedia_Json]', N'C') IS NOT NULL
            ALTER TABLE [Store].[ProductMedia] DROP CONSTRAINT [CK_Store_ProductMedia_Json];
        DECLARE @constraint sysname, @dropSql nvarchar(max);
        DECLARE media_defaults CURSOR LOCAL FAST_FORWARD FOR
            SELECT dc.name FROM sys.default_constraints dc
            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'[Store].[ProductMedia]') AND c.name IN (N'ImagesJson', N'VideosJson');
        OPEN media_defaults;
        FETCH NEXT FROM media_defaults INTO @constraint;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @dropSql = N'ALTER TABLE [Store].[ProductMedia] DROP CONSTRAINT ' + QUOTENAME(@constraint);
            EXEC sys.sp_executesql @dropSql;
            FETCH NEXT FROM media_defaults INTO @constraint;
        END;
        CLOSE media_defaults;
        DEALLOCATE media_defaults;
        ALTER TABLE [Store].[ProductMedia] DROP COLUMN [ImagesJson], [VideosJson];
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'102-product-media-relations');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
