SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51480, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'148-order-item-id-order')
    BEGIN
        IF OBJECT_ID(N'[Store].[OrderItem]', N'U') IS NULL
            THROW 51481, 'Store.OrderItem is missing.', 1;
        IF COL_LENGTH(N'Store.OrderItem', N'IdCustomerOrder') IS NULL OR COL_LENGTH(N'Store.OrderItem', N'IdOrder') IS NOT NULL
            THROW 51482, 'Unexpected Store.OrderItem order key column state.', 1;
        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.foreign_key_columns fkc
            JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
            WHERE fkc.parent_object_id = OBJECT_ID(N'[Store].[OrderItem]')
              AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = N'IdCustomerOrder'
              AND fkc.referenced_object_id = OBJECT_ID(N'[Store].[Order]')
              AND COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) = N'Id'
        )
            THROW 51483, 'Expected OrderItem to Order foreign key is missing.', 1;

        DECLARE @before BIGINT = (SELECT COUNT_BIG(*) FROM [Store].[OrderItem]);
        EXEC sys.sp_rename N'Store.OrderItem.IdCustomerOrder', N'IdOrder', N'COLUMN';

        IF COL_LENGTH(N'Store.OrderItem', N'IdOrder') IS NULL OR COL_LENGTH(N'Store.OrderItem', N'IdCustomerOrder') IS NOT NULL
            THROW 51484, 'OrderItem key column rename failed.', 1;
        IF (SELECT COUNT_BIG(*) FROM [Store].[OrderItem]) <> @before
            THROW 51485, 'OrderItem row count changed during column rename.', 1;

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'148-order-item-id-order');
    END;

    IF COL_LENGTH(N'Store.OrderItem', N'IdOrder') IS NULL OR COL_LENGTH(N'Store.OrderItem', N'IdCustomerOrder') IS NOT NULL
        THROW 51486, 'Migration marker exists but OrderItem schema is not current.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
