SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'104-product-asset-required-fields')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'103-unified-product-assets')
            THROW 51000, 'Migration 103 is required first.', 1;

        UPDATE a
        SET Title = CASE WHEN NULLIF(LTRIM(RTRIM(a.Title)), N'') IS NULL THEN p.Name ELSE a.Title END,
            ThumbnailPath = COALESCE(a.ThumbnailPath, N''),
            CardPath = COALESCE(a.CardPath, N''),
            CreatedAtUtc = COALESCE(a.CreatedAtUtc, SYSUTCDATETIME())
        FROM Store.ProductAsset a JOIN Warehouse.Product p ON p.Id = a.IdProduct;

        ALTER TABLE Store.ProductAsset ALTER COLUMN Title nvarchar(300) NOT NULL;
        ALTER TABLE Store.ProductAsset ALTER COLUMN ThumbnailPath nvarchar(1000) NOT NULL;
        ALTER TABLE Store.ProductAsset ALTER COLUMN CardPath nvarchar(1000) NOT NULL;
        ALTER TABLE Store.ProductAsset ALTER COLUMN CreatedAtUtc datetime2(3) NOT NULL;
        ALTER TABLE Store.ProductAsset ADD CONSTRAINT DF_ProductAsset_CreatedAtUtc DEFAULT SYSUTCDATETIME() FOR CreatedAtUtc;
        ALTER TABLE Store.ProductAsset ADD CONSTRAINT DF_ProductAsset_ThumbnailPath DEFAULT N'' FOR ThumbnailPath;
        ALTER TABLE Store.ProductAsset ADD CONSTRAINT DF_ProductAsset_CardPath DEFAULT N'' FOR CardPath;

        EXEC sys.sp_rename N'Store.ProductMediaType.[Key]', N'Title', N'COLUMN';
        EXEC sys.sp_executesql N'UPDATE Store.ProductMediaType SET Title = CASE Id WHEN 1 THEN N''تصویر'' WHEN 2 THEN N''ویدئو'' WHEN 3 THEN N''فایل'' ELSE Title END';
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'104-product-asset-required-fields');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
