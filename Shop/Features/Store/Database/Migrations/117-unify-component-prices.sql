SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'117-unify-component-prices')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'116-product-price-reference')
            THROW 51250, 'Migration 116 must be applied first.', 1;

        DECLARE @columns TABLE (TableName sysname NOT NULL, ColumnName sysname NOT NULL);
        INSERT @columns VALUES
            (N'ProductSalesPlanOffer', N'PriceAmount'), (N'ProductSalesPlanOffer', N'DiscountPercent'),
            (N'ProductVariantSalesPlanOffer', N'PriceAmount'), (N'ProductVariantSalesPlanOffer', N'DiscountPercent'),
            (N'ProductAccessory', N'PriceAmount'), (N'ProductSalesPlanOfferAccessory', N'PriceAmount'),
            (N'ProductPurchaseOptionValue', N'PriceDelta');
        IF EXISTS (SELECT 1 FROM @columns c WHERE COL_LENGTH(N'Store.' + c.TableName, c.ColumnName) IS NULL)
            THROW 51251, 'A legacy component price column is missing; review the schema.', 1;
        IF EXISTS (SELECT 1 FROM sys.sql_expression_dependencies d JOIN sys.sql_modules m ON m.object_id = d.referencing_id JOIN @columns c ON d.referenced_id = OBJECT_ID(N'Store.' + c.TableName))
            THROW 51252, 'A database module depends on a price source table; review it before migration.', 1;
        IF EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns col ON col.object_id = ic.object_id AND col.column_id = ic.column_id JOIN @columns c ON ic.object_id = OBJECT_ID(N'Store.' + c.TableName) AND col.name = c.ColumnName)
            THROW 51253, 'An index depends on a legacy price column.', 1;
        IF EXISTS (SELECT 1 FROM sys.computed_columns cc JOIN @columns c ON cc.object_id = OBJECT_ID(N'Store.' + c.TableName) AND cc.definition LIKE N'%' + c.ColumnName + N'%')
            THROW 51254, 'A computed column depends on a legacy price column.', 1;
        IF EXISTS (SELECT 1 FROM sys.check_constraints ck JOIN @columns c ON ck.parent_object_id = OBJECT_ID(N'Store.' + c.TableName) AND ck.definition LIKE N'%' + c.ColumnName + N'%' JOIN sys.columns othercol ON othercol.object_id = ck.parent_object_id AND CHARINDEX(N'[' + othercol.name + N']', ck.definition) > 0 WHERE NOT EXISTS (SELECT 1 FROM @columns allowed WHERE OBJECT_ID(N'Store.' + allowed.TableName) = ck.parent_object_id AND allowed.ColumnName = othercol.name))
            THROW 51255, 'A price constraint also references another column.', 1;
        IF EXISTS (SELECT 1 FROM sys.check_constraints ck JOIN sys.columns othercol ON othercol.object_id = ck.parent_object_id AND CHARINDEX(N'[' + othercol.name + N']', ck.definition) > 0 WHERE ck.parent_object_id = OBJECT_ID(N'Store.PriceVersion') AND ck.definition LIKE N'%PriceAmount%' AND othercol.name <> N'PriceAmount')
            THROW 51259, 'The price amount constraint also references another column.', 1;
        DECLARE @amountChecks nvarchar(max) = N'';
        SELECT @amountChecks = @amountChecks + N'ALTER TABLE Store.PriceVersion DROP CONSTRAINT ' + QUOTENAME(name) + N';'
        FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'Store.PriceVersion') AND definition LIKE N'%PriceAmount%';
        EXEC sys.sp_executesql @amountChecks;
        ALTER TABLE Store.PriceVersion WITH CHECK ADD CONSTRAINT CK_PriceVersion_AmountByComponent CHECK (PriceAmount >= 0 OR IdOptionValue IS NOT NULL);

        EXEC sys.sp_executesql N'
            IF EXISTS (SELECT 1 FROM Store.ProductPurchaseOptionValue WHERE PriceDelta IS NULL OR ABS(PriceDelta * 1000000) > 9007199254740991)
                THROW 51256, ''Option prices must be reviewed before migration.'', 1;
            IF EXISTS (SELECT 1 FROM Store.ProductSalesPlanOffer WHERE PriceAmount IS NULL OR PriceAmount < 0 OR PriceAmount > 9007199254740991 OR DiscountPercent IS NULL OR DiscountPercent < 0 OR DiscountPercent >= 100)
               OR EXISTS (SELECT 1 FROM Store.ProductVariantSalesPlanOffer WHERE PriceAmount IS NULL OR PriceAmount < 0 OR PriceAmount > 9007199254740991 OR DiscountPercent IS NULL OR DiscountPercent < 0 OR DiscountPercent >= 100)
               OR EXISTS (SELECT 1 FROM Store.ProductAccessory WHERE PriceAmount IS NULL OR PriceAmount < 0 OR PriceAmount > 9007199254740991)
               OR EXISTS (SELECT 1 FROM Store.ProductSalesPlanOfferAccessory WHERE PriceAmount IS NULL OR PriceAmount < 0 OR PriceAmount > 9007199254740991)
                THROW 51257, ''Component prices must be reviewed before migration.'', 1;
            IF EXISTS (SELECT IdOptionValue FROM Store.PriceVersion WHERE IsActive = 1 AND IdOptionValue IS NOT NULL GROUP BY IdOptionValue HAVING COUNT(*) > 1)
                THROW 51258, ''An option has multiple active prices; review it before migration.'', 1;

            SELECT Id, PriceAmount, DiscountPercent INTO Store.ProductOfferPriceArchive117 FROM Store.ProductSalesPlanOffer;
            SELECT Id, PriceAmount, DiscountPercent INTO Store.VariantOfferPriceArchive117 FROM Store.ProductVariantSalesPlanOffer;
            SELECT Id, PriceAmount INTO Store.ProductAccessoryPriceArchive117 FROM Store.ProductAccessory;
            SELECT Id, PriceAmount INTO Store.OfferAccessoryPriceArchive117 FROM Store.ProductSalesPlanOfferAccessory;
            SELECT Id, PriceDelta INTO Store.OptionPriceArchive117 FROM Store.ProductPurchaseOptionValue;

            INSERT Store.PriceVersion (IdProduct, IdSalesPlan, PriceAmount, DiscountPercent, IsActive, CreatedAtUtc, ChangeReason)
            SELECT o.IdProduct, o.IdSalesPlan, o.PriceAmount, o.DiscountPercent, 1, SYSUTCDATETIME(), N''Migration 117: offer''
            FROM Store.ProductSalesPlanOffer o
            WHERE NOT EXISTS (SELECT 1 FROM Store.PriceVersion v WHERE v.IdProduct = o.IdProduct AND v.IdSalesPlan = o.IdSalesPlan AND v.IdVariant IS NULL AND v.IdOptionValue IS NULL AND v.IdAccessory IS NULL AND v.IdOrderItem IS NULL AND v.IsActive = 1);

            INSERT Store.PriceVersion (IdProduct, IdSalesPlan, IdVariant, PriceAmount, DiscountPercent, IsActive, CreatedAtUtc, ChangeReason)
            SELECT variant.IdProduct, o.IdSalesPlan, o.IdVariant, o.PriceAmount, o.DiscountPercent, 1, SYSUTCDATETIME(), N''Migration 117: variant''
            FROM Store.ProductVariantSalesPlanOffer o JOIN Warehouse.ProductVariant variant ON variant.Id = o.IdVariant
            WHERE NOT EXISTS (SELECT 1 FROM Store.PriceVersion v WHERE v.IdProduct = variant.IdProduct AND v.IdSalesPlan = o.IdSalesPlan AND v.IdVariant = o.IdVariant AND v.IsActive = 1);

            INSERT Store.PriceVersion (IdProduct, IdOptionValue, PriceAmount, DiscountPercent, IsActive, CreatedAtUtc, ChangeReason)
            SELECT opt.IdProduct, value.Id, ROUND(value.PriceDelta * 1000000, 0), 0, 1, SYSUTCDATETIME(), N''Migration 117: option''
            FROM Store.ProductPurchaseOptionValue value JOIN Store.ProductPurchaseOption opt ON opt.Id = value.IdOption
            WHERE NOT EXISTS (SELECT 1 FROM Store.PriceVersion v WHERE v.IdOptionValue = value.Id AND v.IsActive = 1);

            INSERT Store.PriceVersion (IdProduct, IdSalesPlan, IdAccessory, PriceAmount, DiscountPercent, IsActive, CreatedAtUtc, ChangeReason)
            SELECT pa.IdProduct, salesPlan.Id, pa.IdAccessory, COALESCE(oa.PriceAmount, pa.PriceAmount), 0, 1, SYSUTCDATETIME(), N''Migration 117: accessory''
            FROM Store.ProductAccessory pa
            JOIN Warehouse.SalesPlanReference salesPlan ON salesPlan.IdStockOwnerType = 1
            LEFT JOIN Store.ProductSalesPlanOffer offer ON offer.IdProduct = pa.IdProduct AND offer.IdSalesPlan = salesPlan.Id
            LEFT JOIN Store.ProductSalesPlanOfferAccessory oa ON oa.IdOffer = offer.Id AND oa.IdAccessory = pa.IdAccessory
            WHERE (salesPlan.IsRegular = 1 OR offer.Id IS NOT NULL)
              AND NOT EXISTS (SELECT 1 FROM Store.PriceVersion v WHERE v.IdProduct = pa.IdProduct AND v.IdSalesPlan = salesPlan.Id AND v.IdAccessory = pa.IdAccessory AND v.IsActive = 1);
        ';

        DECLARE @sql nvarchar(max) = N'';
        SELECT @sql = @sql + N'ALTER TABLE Store.' + QUOTENAME(c.TableName) + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
        FROM @columns c JOIN sys.columns col ON col.object_id = OBJECT_ID(N'Store.' + c.TableName) AND col.name = c.ColumnName JOIN sys.default_constraints dc ON dc.parent_object_id = col.object_id AND dc.parent_column_id = col.column_id;
        SELECT @sql = @sql + N'ALTER TABLE Store.' + QUOTENAME(OBJECT_NAME(ck.parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(ck.name) + N';'
        FROM sys.check_constraints ck WHERE EXISTS (SELECT 1 FROM @columns c WHERE ck.parent_object_id = OBJECT_ID(N'Store.' + c.TableName) AND ck.definition LIKE N'%' + c.ColumnName + N'%');
        SELECT @sql = @sql + N'ALTER TABLE Store.' + QUOTENAME(TableName) + N' DROP COLUMN ' + QUOTENAME(ColumnName) + N';' FROM @columns;
        EXEC sys.sp_executesql @sql;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'117-unify-component-prices');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
