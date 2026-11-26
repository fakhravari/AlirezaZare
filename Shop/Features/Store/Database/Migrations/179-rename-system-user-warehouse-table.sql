SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51790, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                   WHERE [MigrationKey] = N'179-rename-system-user-warehouse-table')
    BEGIN
        IF OBJECT_ID(N'[Admin].[SystemUserWarehouseAccess]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[Admin].[SystemUserWarehouse]', N'U') IS NOT NULL
            THROW 51791, 'Both old and new warehouse access tables exist.', 1;

        IF OBJECT_ID(N'[Admin].[SystemUserWarehouseAccess]', N'U') IS NOT NULL
            EXEC sys.sp_rename N'Admin.SystemUserWarehouseAccess', N'SystemUserWarehouse', N'OBJECT';
        ELSE IF OBJECT_ID(N'[Admin].[SystemUserWarehouse]', N'U') IS NULL
            THROW 51792, 'Neither warehouse table name exists.', 1;

        INSERT [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'179-rename-system-user-warehouse-table');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
