SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'112-rename-product-badge-id')
    BEGIN
        DECLARE @columns TABLE (TableName nvarchar(300), OldName sysname, NewName sysname);
        INSERT @columns VALUES
            (N'Warehouse.Product', N'IdProductBadge', N'IdBadge'),
            (N'Account.CartItem', N'IdProductVariant', N'IdVariant'),
            (N'Account.CartItemPurchaseOptionValue', N'IdProductPurchaseOptionValue', N'IdOptionValue'),
            (N'Store.Order', N'IdWarehouseDocument', N'IdDocument'),
            (N'Store.OrderItem', N'IdProductVariant', N'IdVariant'),
            (N'Store.PriceVersion', N'IdProductVariant', N'IdVariant'),
            (N'Warehouse.InventoryItem', N'IdProductVariant', N'IdVariant'),
            (N'Store.ProductVariantSalesPlanOffer', N'IdProductVariant', N'IdVariant'),
            (N'Store.HomeTestimonial', N'IdProductFeedback', N'IdFeedback'),
            (N'Store.ProductPurchaseOptionValue', N'IdProductPurchaseOption', N'IdOption'),
            (N'Store.ProductCollectionItem', N'IdProductCollection', N'IdCollection'),
            (N'Store.ProductSalesPlanOfferAccessory', N'IdProductSalesPlanOffer', N'IdOffer'),
            (N'Store.ProductSalesPlanOfferSpecificationOverride', N'IdProductSalesPlanOffer', N'IdOffer'),
            (N'Warehouse.Warehouse', N'IdWarehouseType', N'IdType');
        IF EXISTS (SELECT 1 FROM @columns WHERE COL_LENGTH(TableName, OldName) IS NULL OR COL_LENGTH(TableName, NewName) IS NOT NULL)
            THROW 51203, 'Unexpected column state; review database before renaming.', 1;
        IF EXISTS (SELECT 1 FROM sys.sql_modules m JOIN @columns c ON m.definition LIKE N'%' + c.OldName + N'%')
            THROW 51204, 'A database module references a renamed column; review it before renaming.', 1;
        IF EXISTS (SELECT 1 FROM sys.check_constraints ck JOIN @columns c ON ck.parent_object_id = OBJECT_ID(c.TableName) AND ck.definition LIKE N'%' + c.OldName + N'%' WHERE ck.name <> N'CK_Store_PriceVersion_SingleTypedReference')
            THROW 51205, 'An unexpected check constraint references a renamed column.', 1;
        IF EXISTS (SELECT 1 FROM sys.computed_columns cc JOIN @columns c ON cc.object_id = OBJECT_ID(c.TableName) AND cc.definition LIKE N'%' + c.OldName + N'%')
            THROW 51206, 'A computed column references a renamed column.', 1;
        -- SQL Server cannot rename a column while a filtered index predicate references it.
        IF (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'Warehouse.InventoryItem') AND name IN (N'UX_Warehouse_InventoryItem_ProductVariant', N'UX_Warehouse_InventoryItem_ProductWithoutVariant') AND is_unique = 1 AND type = 2 AND is_disabled = 0 AND is_hypothetical = 0 AND data_space_id = (SELECT data_space_id FROM sys.filegroups WHERE name = N'PRIMARY') AND fill_factor = 0 AND is_padded = 0 AND ignore_dup_key = 0 AND allow_row_locks = 1 AND allow_page_locks = 1 AND ((name = N'UX_Warehouse_InventoryItem_ProductVariant' AND filter_definition = N'([IdProductVariant] IS NOT NULL)') OR (name = N'UX_Warehouse_InventoryItem_ProductWithoutVariant' AND filter_definition = N'([IdProductVariant] IS NULL)'))) <> 2
            THROW 51208, 'Unexpected inventory filtered index state.', 1;
        IF EXISTS (SELECT 1 FROM sys.indexes i JOIN @columns c ON i.object_id = OBJECT_ID(c.TableName) AND i.filter_definition LIKE N'%' + c.OldName + N'%' WHERE i.object_id <> OBJECT_ID(N'Warehouse.InventoryItem') OR i.name NOT IN (N'UX_Warehouse_InventoryItem_ProductVariant', N'UX_Warehouse_InventoryItem_ProductWithoutVariant'))
            THROW 51209, 'An unexpected filtered index references a renamed column.', 1;
        IF EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id JOIN sys.columns col ON col.object_id = ic.object_id AND col.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'Warehouse.InventoryItem') AND i.name IN (N'UX_Warehouse_InventoryItem_ProductVariant', N'UX_Warehouse_InventoryItem_ProductWithoutVariant') AND NOT (ic.is_included_column = 0 AND ic.is_descending_key = 0 AND ((ic.key_ordinal = 1 AND col.name = N'IdProduct') OR (i.name = N'UX_Warehouse_InventoryItem_ProductVariant' AND ic.key_ordinal = 2 AND col.name = N'IdProductVariant')))) OR (SELECT COUNT(*) FROM sys.index_columns ic JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id WHERE i.object_id = OBJECT_ID(N'Warehouse.InventoryItem') AND i.name IN (N'UX_Warehouse_InventoryItem_ProductVariant', N'UX_Warehouse_InventoryItem_ProductWithoutVariant')) <> 3 OR EXISTS (SELECT 1 FROM sys.partitions p JOIN sys.indexes i ON i.object_id = p.object_id AND i.index_id = p.index_id WHERE i.object_id = OBJECT_ID(N'Warehouse.InventoryItem') AND i.name IN (N'UX_Warehouse_InventoryItem_ProductVariant', N'UX_Warehouse_InventoryItem_ProductWithoutVariant') AND p.data_compression <> 0)
            THROW 51210, 'Unexpected inventory filtered index columns or compression.', 1;
        DECLARE @check nvarchar(max);
        SELECT @check = definition FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'Store.PriceVersion') AND name = N'CK_Store_PriceVersion_SingleTypedReference'
          AND is_disabled = 0 AND is_not_trusted = 0 AND is_not_for_replication = 0;
        IF @check IS NULL OR @check NOT LIKE N'%[[]IdProductVariant]%' THROW 51207, 'Unexpected price version check constraint state.', 1;
        DROP INDEX UX_Warehouse_InventoryItem_ProductVariant ON Warehouse.InventoryItem;
        DROP INDEX UX_Warehouse_InventoryItem_ProductWithoutVariant ON Warehouse.InventoryItem;
        ALTER TABLE Store.PriceVersion DROP CONSTRAINT CK_Store_PriceVersion_SingleTypedReference;
        DECLARE @table nvarchar(300), @old sysname, @new sysname, @qualified nvarchar(600);
        DECLARE columns_to_rename CURSOR LOCAL FAST_FORWARD FOR SELECT TableName, OldName, NewName FROM @columns;
        OPEN columns_to_rename;
        FETCH NEXT FROM columns_to_rename INTO @table, @old, @new;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @qualified = QUOTENAME(PARSENAME(@table, 2)) + N'.' + QUOTENAME(PARSENAME(@table, 1)) + N'.' + QUOTENAME(@old);
            EXEC sys.sp_rename @qualified, @new, N'COLUMN';
            FETCH NEXT FROM columns_to_rename INTO @table, @old, @new;
        END;
        CLOSE columns_to_rename;
        DEALLOCATE columns_to_rename;
        EXEC sys.sp_executesql N'CREATE UNIQUE NONCLUSTERED INDEX UX_Warehouse_InventoryItem_ProductVariant ON Warehouse.InventoryItem (IdProduct, IdVariant) WHERE IdVariant IS NOT NULL ON [PRIMARY]; CREATE UNIQUE NONCLUSTERED INDEX UX_Warehouse_InventoryItem_ProductWithoutVariant ON Warehouse.InventoryItem (IdProduct) WHERE IdVariant IS NULL ON [PRIMARY];';
        SET @check = REPLACE(@check, N'[IdProductVariant]', N'[IdVariant]');
        DECLARE @checkSql nvarchar(max) = N'ALTER TABLE Store.PriceVersion WITH CHECK ADD CONSTRAINT CK_Store_PriceVersion_SingleTypedReference CHECK ' + @check;
        EXEC sys.sp_executesql @checkSql;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'112-rename-product-badge-id');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
