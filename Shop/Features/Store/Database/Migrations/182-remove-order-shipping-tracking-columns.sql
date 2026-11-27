SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51820, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                   WHERE [MigrationKey] = N'182-remove-order-shipping-tracking-columns')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                       WHERE [MigrationKey] = N'181-remove-legacy-stock-rebuild-procedures')
            THROW 51821, 'Migration 181 must be applied first.', 1;

        IF COL_LENGTH(N'Store.Order', N'ShippingCarrier') IS NOT NULL
            ALTER TABLE [Store].[Order] DROP COLUMN [ShippingCarrier];

        IF COL_LENGTH(N'Store.Order', N'ShippingTrackingCode') IS NOT NULL
            ALTER TABLE [Store].[Order] DROP COLUMN [ShippingTrackingCode];

        INSERT [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'182-remove-order-shipping-tracking-columns');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
