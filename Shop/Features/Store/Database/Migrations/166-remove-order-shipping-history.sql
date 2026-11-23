SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51660, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'166-remove-order-shipping-history')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'164-order-status-identity-keys')
            THROW 51661, 'Migration 164 is required first.', 1;
        IF OBJECT_ID(N'Store.OrderShippingHistory', N'U') IS NULL
            THROW 51662, 'Store.OrderShippingHistory is missing; inspect before migration.', 1;
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID(N'Store.OrderShippingHistory'))
            THROW 51663, 'A foreign key references order shipping history.', 1;
        IF EXISTS (SELECT 1 FROM sys.sql_modules WHERE definition LIKE N'%OrderShippingHistory%')
            THROW 51664, 'A database module references order shipping history.', 1;
        EXEC sys.sp_executesql N'DROP TABLE Store.OrderShippingHistory;';
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'166-remove-order-shipping-history');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
