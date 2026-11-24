SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51700, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'170-order-address-required')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'169-warehouse-voucher-names')
            THROW 51701, 'Migration 169 is required.', 1;
        IF EXISTS (SELECT 1 FROM Store.[Order] WHERE IdAddress IS NULL)
            THROW 51702, 'Orders without an address must be resolved before this migration.', 1;
        IF EXISTS (SELECT 1 FROM Store.[Order] o LEFT JOIN Account.Address a ON a.Id = o.IdAddress WHERE a.Id IS NULL)
            THROW 51703, 'An order references a missing address.', 1;
        ALTER TABLE Store.[Order] ALTER COLUMN IdAddress bigint NOT NULL;
        ALTER TABLE Store.[Order] WITH CHECK CHECK CONSTRAINT FK_Account_CustomerOrder_Address;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'170-order-address-required');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
