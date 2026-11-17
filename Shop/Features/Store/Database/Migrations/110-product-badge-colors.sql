SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'110-product-badge-colors')
    BEGIN
        IF COL_LENGTH(N'Store.ProductBadge', N'BackgroundColor') IS NOT NULL
            THROW 51200, 'Product badge color already exists; review database state.', 1;
        ALTER TABLE Store.ProductBadge ADD BackgroundColor varchar(7) NOT NULL CONSTRAINT DF_ProductBadge_BackgroundColor DEFAULT ('#102A43');
        EXEC sys.sp_executesql N'UPDATE Store.ProductBadge SET BackgroundColor = ''#11805F'' WHERE Title = N''جدید'';';
        EXEC sys.sp_executesql N'ALTER TABLE Store.ProductBadge ADD CONSTRAINT CK_ProductBadge_BackgroundColor CHECK (LEN(BackgroundColor) = 7 AND LEFT(BackgroundColor, 1) = ''#'' AND SUBSTRING(BackgroundColor, 2, 6) COLLATE Latin1_General_100_BIN2 NOT LIKE ''%[^0-9A-Fa-f]%'');';
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'110-product-badge-colors');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
