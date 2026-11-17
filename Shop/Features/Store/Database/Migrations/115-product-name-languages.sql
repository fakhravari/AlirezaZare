SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'115-product-name-languages')
    BEGIN
        IF COL_LENGTH(N'Warehouse.Product', N'Name') IS NULL OR COL_LENGTH(N'Warehouse.Product', N'EnglishName') IS NULL
           OR COL_LENGTH(N'Warehouse.Product', N'NameFa') IS NOT NULL OR COL_LENGTH(N'Warehouse.Product', N'NameEn') IS NOT NULL
            THROW 51230, 'Unexpected product name column state.', 1;
        IF EXISTS (SELECT 1 FROM sys.sql_expression_dependencies d JOIN sys.sql_modules m ON m.object_id = d.referencing_id WHERE d.referenced_id = OBJECT_ID(N'Warehouse.Product'))
            THROW 51231, 'A database module references the product table; review it before renaming.', 1;
        IF EXISTS (SELECT 1 FROM sys.computed_columns WHERE object_id = OBJECT_ID(N'Warehouse.Product') AND (definition LIKE N'%[[]Name]%' OR definition LIKE N'%[[]EnglishName]%'))
            THROW 51232, 'A computed column references a product name.', 1;
        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'Warehouse.Product') AND name <> N'CK_Warehouse_Product_Name_NotEmpty' AND (definition LIKE N'%[[]Name]%' OR definition LIKE N'%[[]EnglishName]%'))
            THROW 51233, 'An unexpected check constraint references a product name.', 1;
        DECLARE @check nvarchar(max);
        SELECT @check = definition FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'Warehouse.Product') AND name = N'CK_Warehouse_Product_Name_NotEmpty'
          AND is_disabled = 0 AND is_not_trusted = 0 AND is_not_for_replication = 0;
        IF @check IS NULL OR @check NOT LIKE N'%[[]Name]%' THROW 51234, 'Unexpected product name check constraint state.', 1;
        ALTER TABLE Warehouse.Product DROP CONSTRAINT CK_Warehouse_Product_Name_NotEmpty;
        EXEC sys.sp_rename N'Warehouse.Product.Name', N'NameFa', N'COLUMN';
        EXEC sys.sp_rename N'Warehouse.Product.EnglishName', N'NameEn', N'COLUMN';
        SET @check = REPLACE(@check, N'[Name]', N'[NameFa]');
        DECLARE @sql nvarchar(max) = N'ALTER TABLE Warehouse.Product WITH CHECK ADD CONSTRAINT CK_Warehouse_Product_Name_NotEmpty CHECK ' + @check;
        EXEC sys.sp_executesql @sql;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'115-product-name-languages');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
