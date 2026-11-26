SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51780, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                   WHERE [MigrationKey] = N'178-remove-order-address-snapshot')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                       WHERE [MigrationKey] = N'177-order-accessory-id-and-unique-reference')
            THROW 51781, 'Migration 177 must be applied first.', 1;
        IF COL_LENGTH(N'Store.Order', N'AddressSnapshotJson') IS NULL
            THROW 51782, 'Store.Order.AddressSnapshotJson is required.', 1;

        IF EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE parent_object_id=OBJECT_ID(N'[Store].[Order]')
                     AND [name]=N'CK_Account_CustomerOrder_Snapshot')
            ALTER TABLE [Store].[Order] DROP CONSTRAINT [CK_Account_CustomerOrder_Snapshot];

        ALTER TABLE [Store].[Order] DROP COLUMN [AddressSnapshotJson];

        INSERT [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'178-remove-order-address-snapshot');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
