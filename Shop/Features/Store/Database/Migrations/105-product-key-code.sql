SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'105-product-key-code')
    BEGIN
        IF COL_LENGTH(N'Warehouse.Product', N'Code') IS NULL
            THROW 51150, 'Legacy product Code column is missing.', 1;
        IF EXISTS (SELECT 1 FROM Warehouse.Product WHERE NULLIF(LTRIM(RTRIM(ProductKey)), N'') IS NULL)
            THROW 51151, 'Empty product keys require review before migration.', 1;
        IF EXISTS (SELECT 1 FROM Warehouse.Product GROUP BY ProductKey HAVING COUNT(*) > 1)
            THROW 51152, 'Duplicate product keys require review before migration.', 1;
        IF OBJECT_ID(N'Warehouse.ProductCodeArchive105', N'U') IS NOT NULL
            THROW 51153, 'Product code archive already exists; review before migration.', 1;
        CREATE TABLE Warehouse.ProductCodeArchive105 (IdProduct bigint NOT NULL PRIMARY KEY, Code nvarchar(100) NOT NULL, ProductKey nvarchar(128) NOT NULL);
        INSERT Warehouse.ProductCodeArchive105 (IdProduct, Code, ProductKey)
            SELECT Id, Code, ProductKey FROM Warehouse.Product;

        -- Only single-column legacy indexes are removed; other dependencies stop the transaction.
        IF EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID(N'Warehouse.Product') AND c.name=N'Code'
              AND (i.is_primary_key=1 OR (SELECT COUNT(*) FROM sys.index_columns allcols WHERE allcols.object_id=i.object_id AND allcols.index_id=i.index_id) <> 1))
            THROW 51154, 'Composite or primary-key dependency on product Code requires review.', 1;
        DECLARE @name sysname, @constraint bit, @sql nvarchar(max);
        DECLARE code_indexes CURSOR LOCAL FAST_FORWARD FOR
            SELECT DISTINCT i.name, i.is_unique_constraint FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID(N'Warehouse.Product') AND c.name=N'Code' AND i.is_primary_key=0;
        OPEN code_indexes;
        FETCH NEXT FROM code_indexes INTO @name, @constraint;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @sql = CASE WHEN @constraint=1 THEN N'ALTER TABLE Warehouse.Product DROP CONSTRAINT ' + QUOTENAME(@name)
                ELSE N'DROP INDEX ' + QUOTENAME(@name) + N' ON Warehouse.Product' END;
            EXEC sys.sp_executesql @sql;
            FETCH NEXT FROM code_indexes INTO @name, @constraint;
        END;
        CLOSE code_indexes; DEALLOCATE code_indexes;
        SELECT @name = dc.name FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id=dc.parent_object_id AND c.column_id=dc.parent_column_id
            WHERE dc.parent_object_id=OBJECT_ID(N'Warehouse.Product') AND c.name=N'Code';
        IF @name IS NOT NULL
        BEGIN
            SET @sql=N'ALTER TABLE Warehouse.Product DROP CONSTRAINT ' + QUOTENAME(@name);
            EXEC sys.sp_executesql @sql;
        END;
        ALTER TABLE Warehouse.Product DROP COLUMN Code;
        CREATE UNIQUE INDEX UX_Product_ProductKey_105 ON Warehouse.Product(ProductKey);
        ALTER TABLE Warehouse.Product ADD CONSTRAINT CK_Product_ProductKey_NotBlank_105 CHECK (LEN(LTRIM(RTRIM(ProductKey))) > 0);
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'105-product-key-code');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
