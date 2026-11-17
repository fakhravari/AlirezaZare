SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'116-product-price-reference')
    BEGIN
        IF COL_LENGTH(N'Warehouse.Product', N'PriceAmount') IS NULL OR COL_LENGTH(N'Warehouse.Product', N'DiscountPercent') IS NULL
            THROW 51240, 'Product price columns are missing; review the schema before migration.', 1;
        IF NOT EXISTS (SELECT 1 FROM Warehouse.SalesPlanReference WHERE IsRegular = 1 AND IdStockOwnerType = 1)
            THROW 51241, 'A regular sales salesPlan is required before migrating product prices.', 1;
        IF EXISTS (SELECT 1 FROM sys.sql_expression_dependencies d JOIN sys.sql_modules m ON m.object_id = d.referencing_id WHERE d.referenced_id = OBJECT_ID(N'Warehouse.Product'))
            THROW 51242, 'A database module references the product table; review it before removing price columns.', 1;
        IF EXISTS (SELECT 1 FROM sys.computed_columns WHERE object_id = OBJECT_ID(N'Warehouse.Product') AND (definition LIKE N'%PriceAmount%' OR definition LIKE N'%DiscountPercent%'))
            THROW 51243, 'A computed column depends on product prices.', 1;
        IF EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE ic.object_id = OBJECT_ID(N'Warehouse.Product') AND c.name IN (N'PriceAmount', N'DiscountPercent'))
            THROW 51244, 'An index depends on product prices; review it before migration.', 1;
        IF EXISTS (SELECT 1 FROM sys.check_constraints ck JOIN sys.columns c ON c.object_id = ck.parent_object_id
                   WHERE ck.parent_object_id = OBJECT_ID(N'Warehouse.Product') AND (ck.definition LIKE N'%PriceAmount%' OR ck.definition LIKE N'%DiscountPercent%')
                     AND c.name NOT IN (N'PriceAmount', N'DiscountPercent') AND CHARINDEX(N'[' + c.name + N']', ck.definition) > 0)
            THROW 51246, 'A product price constraint also references another column; review it before migration.', 1;

        EXEC sys.sp_executesql N'
            IF EXISTS (SELECT 1 FROM Warehouse.Product WHERE PriceAmount IS NULL OR DiscountPercent IS NULL OR PriceAmount < 0 OR DiscountPercent < 0 OR DiscountPercent >= 100 OR PriceAmount * 100.0 / NULLIF(100.0 - DiscountPercent, 0) > 9007199254740991)
                THROW 51245, ''Product prices must be reviewed before migration.'', 1;

            CREATE TABLE Warehouse.ProductPriceArchive116 (IdProduct bigint NOT NULL PRIMARY KEY, PriceAmount decimal(19,0) NOT NULL, DiscountPercent decimal(5,2) NOT NULL);
            INSERT Warehouse.ProductPriceArchive116 (IdProduct, PriceAmount, DiscountPercent)
                SELECT Id, PriceAmount, DiscountPercent FROM Warehouse.Product;

            INSERT Store.PriceVersion (IdProduct, IdSalesPlan, PriceAmount, DiscountPercent, IsActive, CreatedAtUtc, ChangeReason)
            SELECT product.Id, salesPlan.Id,
                   COALESCE(offer.PriceAmount, ROUND(product.PriceAmount * 100.0 / (100.0 - product.DiscountPercent), 0)),
                   COALESCE(offer.DiscountPercent, product.DiscountPercent), 1, SYSUTCDATETIME(), N''Migration 116: product price''
            FROM Warehouse.Product product
            JOIN Warehouse.SalesPlanReference salesPlan ON salesPlan.IdStockOwnerType = 1
            LEFT JOIN Store.ProductSalesPlanOffer offer ON offer.IdProduct = product.Id AND offer.IdSalesPlan = salesPlan.Id
            WHERE (salesPlan.IsRegular = 1 OR offer.Id IS NOT NULL)
              AND NOT EXISTS (SELECT 1 FROM Store.PriceVersion version WHERE version.IdProduct = product.Id AND version.IdSalesPlan = salesPlan.Id
                  AND version.IdVariant IS NULL AND version.IdOptionValue IS NULL AND version.IdAccessory IS NULL AND version.IdOrderItem IS NULL AND version.IsActive = 1);
        ';

        DECLARE @sql nvarchar(max) = N'';
        SELECT @sql = @sql + N'ALTER TABLE Warehouse.Product DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
        FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'Warehouse.Product') AND c.name IN (N'PriceAmount', N'DiscountPercent');
        SELECT @sql = @sql + N'ALTER TABLE Warehouse.Product DROP CONSTRAINT ' + QUOTENAME(ck.name) + N';'
        FROM sys.check_constraints ck WHERE ck.parent_object_id = OBJECT_ID(N'Warehouse.Product')
          AND (ck.definition LIKE N'%PriceAmount%' OR ck.definition LIKE N'%DiscountPercent%');
        EXEC sys.sp_executesql @sql;
        EXEC sys.sp_executesql N'ALTER TABLE Warehouse.Product DROP COLUMN PriceAmount, DiscountPercent;';
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'116-product-price-reference');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
