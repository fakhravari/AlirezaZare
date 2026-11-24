SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51710, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'171-remove-order-redundant-columns')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'170-order-address-required')
            THROW 51711, 'Migration 170 is required.', 1;
        IF OBJECT_ID(N'[Store].[Order]', N'U') IS NULL
           OR COL_LENGTH(N'Store.Order', N'CreatedAtUtc') IS NULL
           OR COL_LENGTH(N'Store.Order', N'OrderedAtUtc') IS NULL
           OR COL_LENGTH(N'Store.Order', N'ExternalOrderKey') IS NULL
            THROW 51712, 'Store.Order does not have the expected date columns.', 1;

        IF EXISTS (SELECT 1 FROM sys.indexes
                   WHERE object_id = OBJECT_ID(N'[Store].[Order]')
                     AND name = N'UX_Account_CustomerOrder_ExternalOrderKey')
            DROP INDEX [UX_Account_CustomerOrder_ExternalOrderKey] ON [Store].[Order];

        DECLARE @constraint sysname, @dropSql nvarchar(500);
        DECLARE drop_defaults CURSOR LOCAL FAST_FORWARD FOR
            SELECT dc.name
            FROM sys.default_constraints dc
            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'[Store].[Order]')
              AND c.name IN (N'OrderedAtUtc', N'ExternalOrderKey');
        OPEN drop_defaults;
        FETCH NEXT FROM drop_defaults INTO @constraint;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @dropSql = N'ALTER TABLE [Store].[Order] DROP CONSTRAINT ' + QUOTENAME(@constraint) + N';';
            EXEC sys.sp_executesql @dropSql;
            FETCH NEXT FROM drop_defaults INTO @constraint;
        END;
        CLOSE drop_defaults;
        DEALLOCATE drop_defaults;

        ALTER TABLE [Store].[Order] DROP COLUMN [OrderedAtUtc], [ExternalOrderKey];
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'171-remove-order-redundant-columns');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
