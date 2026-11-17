SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'119-price-version-remove-order-item')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'118-price-version-changed-by-user')
            THROW 51262, 'Migration 118 must be applied first.', 1;
        DECLARE @adminId bigint = (SELECT Id FROM Admin.SystemUser WHERE NormalizedUserName = N'ADMIN');
        IF @adminId IS NULL THROW 51261, 'The admin user is required for legacy price versions.', 1;
        UPDATE Store.PriceVersion SET IdChangedBy = @adminId WHERE IdChangedBy IS NULL;
        ALTER TABLE Store.PriceVersion ALTER COLUMN IdChangedBy bigint NOT NULL;
        IF EXISTS (SELECT 1 FROM Store.PriceVersion WHERE IdOrderItem IS NOT NULL)
            THROW 51263, 'Price versions linked directly to order items require review before removing the column.', 1;
        IF EXISTS (SELECT 1 FROM sys.sql_modules WHERE definition LIKE N'%IdOrderItem%' AND definition LIKE N'%PriceVersion%')
            THROW 51264, 'A database module references PriceVersion and IdOrderItem; review it first.', 1;
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'Store.PriceVersion') AND filter_definition LIKE N'%IdOrderItem%')
            THROW 51265, 'A filtered index references IdOrderItem; review it first.', 1;
        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'Store.PriceVersion') AND definition LIKE N'%IdOrderItem%' AND name <> N'CK_Store_PriceVersion_SingleTypedReference')
            THROW 51266, 'An unexpected check constraint references IdOrderItem.', 1;
        IF EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'Store.PriceVersion') AND c.name = N'IdOrderItem' AND (i.type <> 2 OR i.is_primary_key = 1 OR i.is_unique_constraint = 1 OR i.is_disabled = 1 OR i.is_hypothetical = 1 OR i.data_space_id <> (SELECT data_space_id FROM sys.filegroups WHERE name = N'PRIMARY') OR i.fill_factor <> 0 OR i.is_padded <> 0 OR i.ignore_dup_key <> 0 OR i.allow_row_locks <> 1 OR i.allow_page_locks <> 1))
            THROW 51267, 'An unsupported index references IdOrderItem; review it first.', 1;
        DECLARE @drop nvarchar(max) = N'', @create nvarchar(max) = N'';
        SELECT @drop = @drop + N'DROP INDEX ' + QUOTENAME(i.name) + N' ON Store.PriceVersion;',
               @create = @create + N'CREATE ' + CASE WHEN i.is_unique = 1 THEN N'UNIQUE ' ELSE N'' END + N'NONCLUSTERED INDEX ' + QUOTENAME(i.name) + N' ON Store.PriceVersion (' +
                 (SELECT STRING_AGG(CONVERT(nvarchar(max), QUOTENAME(c.name) + CASE WHEN ic.is_descending_key = 1 THEN N' DESC' ELSE N' ASC' END), N',') WITHIN GROUP (ORDER BY ic.key_ordinal)
                  FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                  WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal > 0 AND c.name <> N'IdOrderItem') + N')' +
                 COALESCE(N' INCLUDE (' + (SELECT STRING_AGG(CONVERT(nvarchar(max), QUOTENAME(c.name)), N',') FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1 AND c.name <> N'IdOrderItem') + N')', N'') +
                 COALESCE(N' WHERE ' + i.filter_definition, N'') + N';'
        FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'Store.PriceVersion') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND c.name = N'IdOrderItem');
        EXEC sys.sp_executesql @drop;
        DECLARE @fks nvarchar(max) = N'';
        SELECT @fks = @fks + N'ALTER TABLE Store.PriceVersion DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
        FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        WHERE fk.parent_object_id = OBJECT_ID(N'Store.PriceVersion') AND c.name = N'IdOrderItem';
        EXEC sys.sp_executesql @fks;
        IF OBJECT_ID(N'Store.CK_Store_PriceVersion_SingleTypedReference', N'C') IS NOT NULL
            ALTER TABLE Store.PriceVersion DROP CONSTRAINT CK_Store_PriceVersion_SingleTypedReference;
        ALTER TABLE Store.PriceVersion DROP COLUMN IdOrderItem;
        ALTER TABLE Store.PriceVersion WITH CHECK ADD CONSTRAINT CK_Store_PriceVersion_SingleTypedReference CHECK
            ((CASE WHEN IdVariant IS NULL THEN 0 ELSE 1 END + CASE WHEN IdOptionValue IS NULL THEN 0 ELSE 1 END + CASE WHEN IdAccessory IS NULL THEN 0 ELSE 1 END) <= 1);
        EXEC sys.sp_executesql @create;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'119-price-version-remove-order-item');
    END
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
