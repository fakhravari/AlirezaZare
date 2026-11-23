SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51620, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'162-remove-empty-order-status-archive')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'158-remove-simulated-order-status')
            THROW 51621, 'Migration 158 is required first.', 1;
        IF OBJECT_ID(N'Store.OrderStatusArchive158', N'U') IS NULL
            THROW 51622, 'Store.OrderStatusArchive158 is missing; inspect before migration.', 1;
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID(N'Store.OrderStatusArchive158'))
            THROW 51623, 'A foreign key references the archive; inspect before migration.', 1;
        IF EXISTS
        (
            SELECT 1 FROM sys.sql_modules
            WHERE definition LIKE N'%OrderStatusArchive158%'
        )
            THROW 51624, 'A database module references the archive; inspect before migration.', 1;

        -- Lock the archive through the drop so rows cannot be added after the check.
        EXEC sys.sp_executesql N'IF EXISTS (SELECT 1 FROM Store.OrderStatusArchive158 WITH (TABLOCKX, HOLDLOCK)) THROW 51625, ''Archive contains rows; preserve and inspect them before removal.'', 1;';
        EXEC sys.sp_executesql N'DROP TABLE Store.OrderStatusArchive158;';
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'162-remove-empty-order-status-archive');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
