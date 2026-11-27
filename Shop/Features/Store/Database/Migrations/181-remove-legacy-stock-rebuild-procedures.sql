SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51810, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                   WHERE [MigrationKey] = N'181-remove-legacy-stock-rebuild-procedures')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                       WHERE [MigrationKey] = N'180-system-user-warehouse-id-and-unique-pair')
            THROW 51811, 'Migration 180 must be applied first.', 1;

        IF OBJECT_ID(N'[Warehouse].[RebuildDocumentStock]', N'P') IS NOT NULL
            DROP PROCEDURE [Warehouse].[RebuildDocumentStock];

        IF OBJECT_ID(N'[Warehouse].[RebuildVoucherStock]', N'P') IS NOT NULL
            DROP PROCEDURE [Warehouse].[RebuildVoucherStock];

        INSERT [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'181-remove-legacy-stock-rebuild-procedures');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
