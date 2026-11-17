SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'109-remove-product-newest-fields')
    BEGIN
        IF COL_LENGTH(N'Warehouse.Product', N'IsNewest') IS NULL OR COL_LENGTH(N'Warehouse.Product', N'NewestOrder') IS NULL
            THROW 51190, 'Product newest fields are missing; review database state.', 1;
        DECLARE @name sysname, @sql nvarchar(max);
        DECLARE newest_defaults CURSOR LOCAL FAST_FORWARD FOR
            SELECT dc.name FROM sys.default_constraints dc
            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'Warehouse.Product') AND c.name IN (N'IsNewest', N'NewestOrder');
        OPEN newest_defaults;
        FETCH NEXT FROM newest_defaults INTO @name;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @sql = N'ALTER TABLE Warehouse.Product DROP CONSTRAINT ' + QUOTENAME(@name);
            EXEC sys.sp_executesql @sql;
            FETCH NEXT FROM newest_defaults INTO @name;
        END;
        CLOSE newest_defaults;
        DEALLOCATE newest_defaults;
        ALTER TABLE Warehouse.Product DROP COLUMN IsNewest, NewestOrder;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'109-remove-product-newest-fields');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
