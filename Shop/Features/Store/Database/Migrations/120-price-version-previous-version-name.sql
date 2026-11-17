SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'120-price-version-previous-version-name')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'119-price-version-remove-order-item')
            THROW 51268, 'Migration 119 must be applied first.', 1;
        IF COL_LENGTH(N'Store.PriceVersion', N'PreviousVersionId') IS NULL OR COL_LENGTH(N'Store.PriceVersion', N'IdPreviousVersion') IS NOT NULL
            THROW 51269, 'Unexpected PriceVersion previous-version columns; review the schema.', 1;
        IF EXISTS (SELECT 1 FROM sys.sql_modules WHERE definition LIKE N'%PreviousVersionId%')
            THROW 51270, 'A database module references PreviousVersionId; review it before renaming.', 1;
        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'Store.PriceVersion') AND definition LIKE N'%PreviousVersionId%')
           OR EXISTS (SELECT 1 FROM sys.computed_columns WHERE object_id = OBJECT_ID(N'Store.PriceVersion') AND definition LIKE N'%PreviousVersionId%')
           OR EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'Store.PriceVersion') AND filter_definition LIKE N'%PreviousVersionId%')
            THROW 51271, 'A schema expression references PreviousVersionId; review it before renaming.', 1;
        EXEC sys.sp_rename N'Store.PriceVersion.PreviousVersionId', N'IdPreviousVersion', N'COLUMN';
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'120-price-version-previous-version-name');
    END
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
