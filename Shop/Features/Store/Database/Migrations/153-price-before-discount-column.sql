SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51530, 'Store.SchemaMigration is required before migration 153.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'153-price-before-discount-column')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'152-price-version-final-price')
            THROW 51531, 'Migration 152 is required before migration 153.', 1;
        IF COL_LENGTH(N'Store.PriceVersion', N'PriceAmount') IS NULL
           OR COL_LENGTH(N'Store.PriceVersion', N'PriceBeforeDiscountAmount') IS NOT NULL
            THROW 51532, 'Unexpected PriceVersion amount columns; review the schema.', 1;
        IF EXISTS (
            SELECT 1 FROM sys.sql_expression_dependencies dependency
            JOIN sys.sql_modules module_info ON module_info.object_id = dependency.referencing_id
            JOIN sys.columns column_info ON column_info.object_id = dependency.referenced_id
                AND column_info.column_id = dependency.referenced_minor_id
            WHERE dependency.referenced_id = OBJECT_ID(N'Store.PriceVersion')
              AND column_info.name = N'PriceAmount'
        )
            THROW 51533, 'A SQL module still references Store.PriceVersion.PriceAmount; review its definition before renaming.', 1;
        IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
            WHERE parent_object_id = OBJECT_ID(N'Store.PriceVersion')
              AND name = N'CK_PriceVersion_AmountByComponent'
              AND REPLACE(REPLACE(REPLACE(definition, N'(', N''), N')', N''), N' ', N'') = N'[PriceAmount]>=0'
              AND is_disabled = 0 AND is_not_trusted = 0)
            THROW 51534, 'Expected enabled and trusted PriceVersion amount constraint was not found.', 1;
        ALTER TABLE Store.PriceVersion DROP CONSTRAINT CK_PriceVersion_AmountByComponent;
        EXEC sys.sp_rename N'Store.PriceVersion.PriceAmount', N'PriceBeforeDiscountAmount', N'COLUMN';
        EXEC(N'ALTER TABLE Store.PriceVersion WITH CHECK ADD CONSTRAINT CK_PriceVersion_AmountByComponent CHECK (PriceBeforeDiscountAmount >= 0);');
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'153-price-before-discount-column');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
