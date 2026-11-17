SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'113-product-badge-text-color')
    BEGIN
        IF COL_LENGTH(N'Store.ProductBadge', N'BackgroundColor') IS NULL
            THROW 51211, 'Product badge background color migration is required.', 1;
        IF COL_LENGTH(N'Store.ProductBadge', N'TextColor') IS NOT NULL
            THROW 51212, 'Product badge text color already exists; review database state.', 1;
        ALTER TABLE Store.ProductBadge ADD TextColor varchar(7) NOT NULL CONSTRAINT DF_ProductBadge_TextColor DEFAULT ('#FFFFFF');
        EXEC sys.sp_executesql N'
            UPDATE b SET TextColor = CASE WHEN 0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b > 0.179 THEN ''#000000'' ELSE ''#FFFFFF'' END
            FROM Store.ProductBadge b
            CROSS APPLY (SELECT CONVERT(float, CONVERT(int, CONVERT(varbinary(1), SUBSTRING(b.BackgroundColor, 2, 2), 2))) / 255.0 AS r,
                                CONVERT(float, CONVERT(int, CONVERT(varbinary(1), SUBSTRING(b.BackgroundColor, 4, 2), 2))) / 255.0 AS g,
                                CONVERT(float, CONVERT(int, CONVERT(varbinary(1), SUBSTRING(b.BackgroundColor, 6, 2), 2))) / 255.0 AS b) v
            CROSS APPLY (SELECT CASE WHEN v.r <= 0.04045 THEN v.r / 12.92 ELSE POWER((v.r + 0.055) / 1.055, 2.4) END AS r,
                                CASE WHEN v.g <= 0.04045 THEN v.g / 12.92 ELSE POWER((v.g + 0.055) / 1.055, 2.4) END AS g,
                                CASE WHEN v.b <= 0.04045 THEN v.b / 12.92 ELSE POWER((v.b + 0.055) / 1.055, 2.4) END AS b) c;
            ALTER TABLE Store.ProductBadge ADD CONSTRAINT CK_ProductBadge_TextColor CHECK (LEN(TextColor) = 7 AND LEFT(TextColor, 1) = ''#'' AND SUBSTRING(TextColor, 2, 6) COLLATE Latin1_General_100_BIN2 NOT LIKE ''%[^0-9A-Fa-f]%'');';
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'113-product-badge-text-color');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
