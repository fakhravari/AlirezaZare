SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'107-remove-product-power-source-use-case')
    BEGIN
        IF COL_LENGTH(N'Warehouse.Product', N'PowerSource') IS NULL OR COL_LENGTH(N'Warehouse.Product', N'UseCase') IS NULL
            THROW 51170, 'Product columns are missing; review database state.', 1;
        IF OBJECT_ID(N'Warehouse.ProductLegacyFieldsArchive107', N'U') IS NOT NULL
            THROW 51171, 'Product legacy fields archive already exists; review database state.', 1;
        CREATE TABLE Warehouse.ProductLegacyFieldsArchive107 (IdProduct bigint NOT NULL PRIMARY KEY, PowerSource nvarchar(200) NULL, UseCase nvarchar(500) NULL);
        INSERT Warehouse.ProductLegacyFieldsArchive107 (IdProduct, PowerSource, UseCase)
            SELECT Id, PowerSource, UseCase FROM Warehouse.Product;

        -- Definitions backed by the removed columns no longer produce a public value.
        UPDATE Store.AttributeDefinition SET IsActive = 0 WHERE SourceKey IN (N'powerSource', N'useCase');

        DECLARE @name sysname, @sql nvarchar(max);
        DECLARE legacy_defaults CURSOR LOCAL FAST_FORWARD FOR
            SELECT dc.name FROM sys.default_constraints dc
            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'Warehouse.Product') AND c.name IN (N'PowerSource', N'UseCase');
        OPEN legacy_defaults;
        FETCH NEXT FROM legacy_defaults INTO @name;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @sql = N'ALTER TABLE Warehouse.Product DROP CONSTRAINT ' + QUOTENAME(@name);
            EXEC sys.sp_executesql @sql;
            FETCH NEXT FROM legacy_defaults INTO @name;
        END;
        CLOSE legacy_defaults;
        DEALLOCATE legacy_defaults;
        -- Other dependencies cause the transaction, including the archive, to roll back.
        ALTER TABLE Warehouse.Product DROP COLUMN PowerSource, UseCase;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'107-remove-product-power-source-use-case');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
