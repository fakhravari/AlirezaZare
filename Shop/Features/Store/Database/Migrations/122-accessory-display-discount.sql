SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'122-accessory-display-discount')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'121-option-prices-by-sales-plan')
            THROW 51290, 'Migration 121 must be applied first.', 1;
        IF COL_LENGTH(N'Store.ProductAccessory', N'DisplayDiscountPercent') IS NOT NULL
            THROW 51291, 'DisplayDiscountPercent already exists without its migration marker.', 1;
        ALTER TABLE Store.ProductAccessory ADD DisplayDiscountPercent decimal(5,2) NULL;
        EXEC(N'ALTER TABLE Store.ProductAccessory WITH CHECK ADD CONSTRAINT CK_Store_ProductAccessory_DisplayDiscountPercent
            CHECK (DisplayDiscountPercent IS NULL OR (DisplayDiscountPercent >= 0 AND DisplayDiscountPercent < 100))');
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'122-accessory-display-discount');
    END
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
